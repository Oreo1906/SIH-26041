using System;
using System.Linq;
using SurakshaXR.Infrastructure;
using UnityEngine;

namespace SurakshaXR.Presentation
{
    public static class OfflineReminders
    {
        public static void Schedule(LocalStore store, string title, string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var next = store.Workers().SelectMany(x => store.Refreshers(x.id)).Where(x => x.status == "pending").OrderBy(x => x.due_at, StringComparer.Ordinal).FirstOrDefault();
            var due = store.GetPreference("reminders") == "enabled" && next != null ? DateTimeOffset.Parse(next.due_at).ToUnixTimeMilliseconds() : 0L;
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var reminder = new AndroidJavaClass("in.surakshaxr.storage.RefresherReminder"))
                reminder.CallStatic("schedule", activity, due, title, message);
#endif
        }
        public static void Enable(LocalStore store, Action<bool> completed)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                const string permission = "android.permission.POST_NOTIFICATIONS";
                if (version.GetStatic<int>("SDK_INT") >= 33 && !UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
                {
                    var callbacks = new UnityEngine.Android.PermissionCallbacks();
                    callbacks.PermissionGranted += _ => { store.SetPreference("reminders", "enabled"); completed(true); };
                    callbacks.PermissionDenied += _ => completed(false);
                    UnityEngine.Android.Permission.RequestUserPermission(permission, callbacks); return;
                }
            }
#endif
            store.SetPreference("reminders", "enabled"); completed(true);
        }
    }
}
