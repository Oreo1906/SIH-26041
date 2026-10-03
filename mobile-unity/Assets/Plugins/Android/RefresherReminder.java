package in.surakshaxr.storage;

import android.app.AlarmManager;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;

/** One generic device reminder; no worker identity appears on the lock screen. */
public final class RefresherReminder extends BroadcastReceiver {
    private static final String CHANNEL = "offline-refreshers";
    private static final String ACTION = "in.surakshaxr.REFRESHER_DUE";
    private static SharedPreferences prefs(Context context) { return context.getSharedPreferences("surakshaxr_reminder", Context.MODE_PRIVATE); }
    private static PendingIntent alarm(Context context) {
        return PendingIntent.getBroadcast(context, 26041, new Intent(context, RefresherReminder.class).setAction(ACTION), PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
    }
    public static void schedule(Context context, long dueMillis, String title, String message) {
        SharedPreferences saved = prefs(context);
        AlarmManager manager = (AlarmManager)context.getSystemService(Context.ALARM_SERVICE);
        manager.cancel(alarm(context));
        if (dueMillis <= 0) { saved.edit().clear().apply(); return; }
        boolean delivered = saved.getLong("deliveredDue", -1) == dueMillis;
        saved.edit().putLong("due", dueMillis).putString("title", title).putString("message", message).apply();
        if (!delivered) manager.setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP, Math.max(dueMillis, System.currentTimeMillis() + 10000), alarm(context));
    }
    @Override public void onReceive(Context context, Intent intent) {
        SharedPreferences saved = prefs(context); long due = saved.getLong("due", 0);
        if (due <= 0 || saved.getLong("deliveredDue", -1) == due) return;
        if (!ACTION.equals(intent.getAction()) || due > System.currentTimeMillis()) {
            schedule(context, due, saved.getString("title", "SurakshaXR"), saved.getString("message", "Offline training refresher due")); return;
        }
        NotificationManager manager = (NotificationManager)context.getSystemService(Context.NOTIFICATION_SERVICE);
        if (!manager.areNotificationsEnabled()) return;
        String title = saved.getString("title", "SurakshaXR");
        manager.createNotificationChannel(new NotificationChannel(CHANNEL, title, NotificationManager.IMPORTANCE_DEFAULT));
        Intent launch = context.getPackageManager().getLaunchIntentForPackage(context.getPackageName());
        if (launch == null) return;
        PendingIntent tap = PendingIntent.getActivity(context, 26041, launch, PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
        Notification notification = new Notification.Builder(context, CHANNEL).setSmallIcon(android.R.drawable.ic_popup_reminder)
            .setContentTitle(title).setContentText(saved.getString("message", "Offline training refresher due"))
            .setContentIntent(tap).setAutoCancel(true).setVisibility(Notification.VISIBILITY_PRIVATE).build();
        try { manager.notify(26041, notification); saved.edit().putLong("deliveredDue", due).apply(); }
        catch (SecurityException denied) { /* Permission revoked: training remains available. */ }
    }
}
