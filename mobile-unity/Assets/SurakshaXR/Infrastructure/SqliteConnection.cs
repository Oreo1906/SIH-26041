using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace SurakshaXR.Infrastructure
{
    public interface ISqliteConnection : IDisposable
    {
        void Execute(string sql, params string[] values);
        List<Dictionary<string, string>> Query(string sql, params string[] values);
        void Begin();
        void Commit();
        void Rollback();
    }

    public static class SqliteConnection
    {
        public static ISqliteConnection Open(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var directory = activity.Call<AndroidJavaObject>("getFilesDir"))
                path = System.IO.Path.Combine(directory.Call<string>("getAbsolutePath"), System.IO.Path.GetFileName(path));
            return new AndroidSqliteConnection(path);
#else
            return new NativeSqliteConnection(path);
#endif
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    internal sealed class AndroidSqliteConnection : ISqliteConnection
    {
        private readonly AndroidJavaObject database;
        public AndroidSqliteConnection(string path)
        { database = new AndroidJavaObject("in.surakshaxr.storage.LocalDatabase", path); }
        public void Execute(string sql, params string[] values)
        { database.Call("execute", sql, JsonConvert.SerializeObject(values)); }
        public List<Dictionary<string, string>> Query(string sql, params string[] values)
        { return JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(database.Call<string>("query", sql, JsonConvert.SerializeObject(values))); }
        public void Begin() => database.Call("begin");
        public void Commit() => database.Call("commit");
        public void Rollback() => database.Call("rollback");
        public void Dispose() { database.Call("close"); database.Dispose(); }
    }
#else
    internal sealed class NativeSqliteConnection : ISqliteConnection
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private const string Library = "winsqlite3";
#else
        private const string Library = "sqlite3";
#endif
        private IntPtr database;
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, IntPtr vfs);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_close(IntPtr db);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_prepare_v2(IntPtr db, byte[] sql, int count, out IntPtr statement, out IntPtr tail);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_step(IntPtr statement);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_finalize(IntPtr statement);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_bind_text(IntPtr statement, int index, byte[] value, int size, IntPtr destructor);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_bind_null(IntPtr statement, int index);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_bind_parameter_count(IntPtr statement);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_column_count(IntPtr statement);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr sqlite3_column_name(IntPtr statement, int column);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr sqlite3_column_text(IntPtr statement, int column);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_column_bytes(IntPtr statement, int column);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_column_type(IntPtr statement, int column);

        public NativeSqliteConnection(string path)
        {
            var result = sqlite3_open_v2(Utf8(path), out database, 2 | 4 | 0x10000, IntPtr.Zero);
            if (result != 0) { Dispose(); throw new InvalidOperationException("Unable to open local SQLite database (code " + result + ")."); }
        }
        private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value + "\0");
        private static void Check(int code)
        { if (code != 0) throw new InvalidOperationException("SQLite operation failed (code " + code + ")."); }
        private IntPtr Prepare(string sql, string[] values)
        {
            if (database == IntPtr.Zero) throw new ObjectDisposedException(nameof(NativeSqliteConnection));
            Check(sqlite3_prepare_v2(database, Utf8(sql), -1, out var statement, out _));
            try
            {
                if (sqlite3_bind_parameter_count(statement) != values.Length) throw new ArgumentException("SQL binding count mismatch.");
                for (var i = 0; i < values.Length; i++)
                {
                    if (values[i] == null) Check(sqlite3_bind_null(statement, i + 1));
                    else
                    {
                        var bytes = Utf8(values[i]);
                        Check(sqlite3_bind_text(statement, i + 1, bytes, bytes.Length - 1, new IntPtr(-1)));
                    }
                }
                return statement;
            }
            catch { sqlite3_finalize(statement); throw; }
        }
        public void Execute(string sql, params string[] values)
        {
            var statement = Prepare(sql, values);
            try
            {
                var code = sqlite3_step(statement);
                if (code != 101) throw new InvalidOperationException("SQLite write failed (code " + code + ").");
            }
            finally { sqlite3_finalize(statement); }
        }
        public List<Dictionary<string, string>> Query(string sql, params string[] values)
        {
            var statement = Prepare(sql, values);
            try
            {
                var rows = new List<Dictionary<string, string>>();
                int code;
                while ((code = sqlite3_step(statement)) == 100)
                {
                    var row = new Dictionary<string, string>(StringComparer.Ordinal);
                    for (var i = 0; i < sqlite3_column_count(statement); i++)
                    {
                        var name = Marshal.PtrToStringAnsi(sqlite3_column_name(statement, i));
                        if (sqlite3_column_type(statement, i) == 5) row[name] = null;
                        else
                        {
                            var bytes = new byte[sqlite3_column_bytes(statement, i)];
                            Marshal.Copy(sqlite3_column_text(statement, i), bytes, 0, bytes.Length);
                            row[name] = Encoding.UTF8.GetString(bytes);
                        }
                    }
                    rows.Add(row);
                }
                if (code != 101) throw new InvalidOperationException("SQLite read failed (code " + code + ").");
                return rows;
            }
            finally { sqlite3_finalize(statement); }
        }
        public void Begin() => Execute("BEGIN IMMEDIATE");
        public void Commit() => Execute("COMMIT");
        public void Rollback() => Execute("ROLLBACK");
        public void Dispose()
        {
            if (database != IntPtr.Zero) { sqlite3_close(database); database = IntPtr.Zero; }
        }
    }
#endif
}
