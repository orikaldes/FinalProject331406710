using Android.App;
using Android.Content;
using AndroidX.Core.App;
using System.Linq;
using System.Threading.Tasks; // NEW: Required for Background Threads

namespace FinalProject331406710
{
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class FriendNotificationReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            // --- FIX: Run the heavy database logic on a background thread! ---
            Task.Run(() =>
            {
                var prefs = context.GetSharedPreferences("user_prefs", FileCreationMode.Private);
                string myId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", "");

                if (string.IsNullOrEmpty(myId)) return;

                int currentRequestsCount = Helper.GetPendingRequests(context, myId).Count;
                int currentFriendsCount = Helper.GetMyFriends(context, myId).Count;

                int lastRequestsCount = prefs.GetInt("LAST_REQUEST_COUNT", currentRequestsCount);
                int lastFriendsCount = prefs.GetInt("LAST_FRIEND_COUNT", currentFriendsCount);

                if (currentRequestsCount > lastRequestsCount)
                {
                    ShowNotification(context, "New Friend Request!", $"You have {currentRequestsCount} pending requests waiting.", 1001);
                }

                if (currentFriendsCount > lastFriendsCount)
                {
                    ShowNotification(context, "Friend Request Accepted!", "You have a new friend in your list.", 1002);
                }

                prefs.Edit()
                     .PutInt("LAST_REQUEST_COUNT", currentRequestsCount)
                     .PutInt("LAST_FRIEND_COUNT", currentFriendsCount)
                     .Apply();
            });
        }

        private void ShowNotification(Context context, string title, string message, int notificationId)
        {
            string channelId = "poker_friends_channel";
            NotificationManager manager = (NotificationManager)context.GetSystemService(Context.NotificationService);

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                NotificationChannel channel = new NotificationChannel(channelId, "Friend Updates", NotificationImportance.Default);
                manager.CreateNotificationChannel(channel);
            }

            Intent clickIntent = new Intent(context, typeof(FriendsActivity));
            PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, clickIntent, PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Builder(context, channelId)
                .SetSmallIcon(Resource.Mipmap.ic_launcher)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetPriority(NotificationCompat.PriorityDefault)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true);

            manager.Notify(notificationId, builder.Build());
        }
    }
}