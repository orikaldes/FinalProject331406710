using Android.App; // Tool for creating the screen
using Android.Views; // Tool for handling views
using Android.Widget; // Tool for UI elements
using AndroidX.RecyclerView.Widget; // Tool for lists
using System; // Basic system tools
using System.Collections.Generic; // Tool for lists

namespace FinalProject331406710
{
    // Custom Event Args to pass data back when buttons are clicked
    public class FriendActionEventArgs : EventArgs
    {
        public Users User { get; set; }
        public string Action { get; set; } // "ADD", "REMOVE", "ACCEPT", "DECLINE", "CANCEL"
    }

    public class FriendsAdapter : BaseAdapter<Users>
    {
        private Activity _context;
        private List<Users> _users;
        private string _currentUserId;

        // Event that the Activity listens to
        public event EventHandler<FriendActionEventArgs> OnActionClick;

        public FriendsAdapter(Activity context, List<Users> users, string currentUserId)
        {
            _context = context;
            _users = users;
            _currentUserId = currentUserId;
        }

        public override Users this[int position] => _users[position];
        public override int Count => _users.Count;
        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            // Reuse the view if possible, otherwise create a new one
            var view = convertView ?? _context.LayoutInflater.Inflate(Resource.Layout.FriendListItemLayout, parent, false);
            var user = _users[position];

            // Find the visual elements in the row
            var imgProfile = view.FindViewById<ImageView>(Resource.Id.imgFriendRow);
            var textName = view.FindViewById<TextView>(Resource.Id.textFriendName);
            var textStatus = view.FindViewById<TextView>(Resource.Id.textFriendStatus);
            var btnPositive = view.FindViewById<ImageButton>(Resource.Id.btnActionPositive);
            var btnNegative = view.FindViewById<ImageButton>(Resource.Id.btnActionNegative);

            textName.Text = user.FullName;
            if (!string.IsNullOrEmpty(user.ProfileImagePath))
            {
                var bitmap = Android.Graphics.BitmapFactory.DecodeFile(user.ProfileImagePath);
                imgProfile.SetImageBitmap(bitmap);
            }
            else
            {
                imgProfile.SetImageResource(Resource.Drawable.ic_default_avatar);
            }

            // 1. Check Status to determine Buttons
            // Returns: -1 (Strangers), 0 (Pending), 1 (Friends)
            int status = Helper.GetFriendStatus(_context, _currentUserId, user.Id);

            // Reset buttons visibility (Hide everything by default)
            btnPositive.Visibility = ViewStates.Gone;
            btnNegative.Visibility = ViewStates.Gone;
            textStatus.Text = "";

            // --- LOGIC: Which buttons to show? ---

            if (status == 1)
            {
                // CASE 1: ALREADY FRIENDS
                textStatus.Text = "Friend";

                // Show Red Delete Button
                btnNegative.Visibility = ViewStates.Visible;
                btnNegative.SetImageResource(Android.Resource.Drawable.IcMenuDelete);
                btnNegative.Click += (s, e) => TriggerAction(user, "REMOVE");
            }
            else if (status == 0)
            {
                // CASE 2: PENDING (Wait, who sent it?)

                // Get list of requests sent TO ME to check direction
                var incomingRequests = Helper.GetPendingRequests(_context, _currentUserId);
                bool didTheySendIt = incomingRequests.Exists(u => u.Id == user.Id);

                if (didTheySendIt)
                {
                    // SUB-CASE 2A: They sent me a request -> Show Accept/Decline
                    textStatus.Text = "Request Received";

                    // Show Green Check
                    btnPositive.Visibility = ViewStates.Visible;
                    btnPositive.SetImageResource(Android.Resource.Drawable.IcInputAdd); // Or a checkmark if you have one

                    // Show Red X
                    btnNegative.Visibility = ViewStates.Visible;
                    btnNegative.SetImageResource(Android.Resource.Drawable.IcMenuCloseClearCancel);

                    btnPositive.Click += (s, e) => TriggerAction(user, "ACCEPT");
                    btnNegative.Click += (s, e) => TriggerAction(user, "DECLINE");
                }
                else
                {
                    // SUB-CASE 2B: I sent them a request -> Show "Undo" button
                    textStatus.Text = "Request Sent";

                    // Show "Undo" / "Revert" button
                    btnNegative.Visibility = ViewStates.Visible;
                    btnNegative.SetImageResource(Android.Resource.Drawable.IcMenuRevert); // Curving arrow icon

                    // Trigger "REMOVE" (logic handles it as Cancel) or "CANCEL" if you updated Activity
                    btnNegative.Click += (s, e) => TriggerAction(user, "CANCEL");
                }
            }
            else
            {
                // CASE 3: STRANGERS (Search Result)
                textStatus.Text = "User";

                // Show Green Add Button
                btnPositive.Visibility = ViewStates.Visible;
                btnPositive.SetImageResource(Android.Resource.Drawable.IcInputAdd);
                btnPositive.Click += (s, e) => TriggerAction(user, "ADD");
            }

            return view;
        }

        private void TriggerAction(Users user, string action)
        {
            // Send the click event back to the main Activity
            OnActionClick?.Invoke(this, new FriendActionEventArgs { User = user, Action = action });
        }
    }
}