using Android.App;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;

namespace FinalProject331406710
{
    internal class UserListAdapter : BaseAdapter<Users>
    {
        private Activity context;
        private List<Users> users;

        public UserListAdapter(Activity context, List<Users> users)
        {
            this.context = context;
            this.users = users;
        }

        public override int Count => users.Count;
        public override Users this[int position] => users[position];
        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var user = users[position];
            var view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.UserListItemLayout, parent, false);

            var textUserName = view.FindViewById<TextView>(Resource.Id.textUserName);

            // --- FIX: Use FullName instead of fName/lName ---
            textUserName.Text = user.FullName;

            return view;
        }
    }
}