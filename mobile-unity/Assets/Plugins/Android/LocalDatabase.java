package in.surakshaxr.storage;

import android.database.Cursor;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteStatement;
import org.json.JSONArray;
import org.json.JSONObject;

/** App-private SQLite bridge. Values are always bound; no worker data is logged. */
public final class LocalDatabase {
    private final SQLiteDatabase database;

    public LocalDatabase(String path) {
        database = SQLiteDatabase.openOrCreateDatabase(path, null);
        database.setForeignKeyConstraintsEnabled(true);
    }

    public void execute(String sql, String parameters) throws Exception {
        JSONArray values = new JSONArray(parameters);
        try (SQLiteStatement statement = database.compileStatement(sql)) {
            for (int i = 0; i < values.length(); i++) {
                if (values.isNull(i)) statement.bindNull(i + 1);
                else statement.bindString(i + 1, values.getString(i));
            }
            statement.execute();
        }
    }

    public String query(String sql, String parameters) throws Exception {
        JSONArray values = new JSONArray(parameters);
        String[] arguments = new String[values.length()];
        for (int i = 0; i < values.length(); i++) {
            if (values.isNull(i)) throw new IllegalArgumentException("Query null values using IS NULL");
            arguments[i] = values.getString(i);
        }
        JSONArray rows = new JSONArray();
        try (Cursor cursor = database.rawQuery(sql, arguments)) {
            while (cursor.moveToNext()) {
                JSONObject row = new JSONObject();
                for (int i = 0; i < cursor.getColumnCount(); i++)
                    row.put(cursor.getColumnName(i), cursor.isNull(i) ? JSONObject.NULL : cursor.getString(i));
                rows.put(row);
            }
        }
        return rows.toString();
    }

    public void begin() { database.beginTransaction(); }
    public void commit() { database.setTransactionSuccessful(); database.endTransaction(); }
    public void rollback() { database.endTransaction(); }
    public void close() { database.close(); }
}
