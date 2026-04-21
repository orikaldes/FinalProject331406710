using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using Android.Widget;

namespace FinalProject331406710
{
    public class Base : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MenuLayout, menu);
            return true;
        }

        // Decides what to show based on if you are logged in or not
        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            ISharedPreferences prefs = GetSharedPreferences("user_prefs", FileCreationMode.Private);
            string currentId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", "");
            bool isLoggedIn = !string.IsNullOrEmpty(currentId);

            IMenuItem itemStart = menu.FindItem(Resource.Id.nav_start);
            IMenuItem itemSignIn = menu.FindItem(Resource.Id.nav_signin);
            IMenuItem itemSignUp = menu.FindItem(Resource.Id.nav_signup);

            IMenuItem itemHome = menu.FindItem(Resource.Id.nav_home);
            IMenuItem itemProfile = menu.FindItem(Resource.Id.nav_profile);
            IMenuItem itemFriends = menu.FindItem(Resource.Id.nav_friends);
            IMenuItem itemSearch = menu.FindItem(Resource.Id.nav_user_search);
            IMenuItem itemLogout = menu.FindItem(Resource.Id.nav_logout);

            if (isLoggedIn)
            {
                if (itemHome != null) itemHome.SetVisible(true);
                if (itemProfile != null) itemProfile.SetVisible(true);
                if (itemFriends != null) itemFriends.SetVisible(true);
                if (itemSearch != null) itemSearch.SetVisible(true);
                if (itemLogout != null) itemLogout.SetVisible(true);

                if (itemStart != null) itemStart.SetVisible(false);
                if (itemSignIn != null) itemSignIn.SetVisible(false);
                if (itemSignUp != null) itemSignUp.SetVisible(false);
            }
            else
            {
                if (itemStart != null) itemStart.SetVisible(true);
                if (itemSignIn != null) itemSignIn.SetVisible(true);
                if (itemSignUp != null) itemSignUp.SetVisible(true);

                if (itemHome != null) itemHome.SetVisible(false);
                if (itemProfile != null) itemProfile.SetVisible(false);
                if (itemFriends != null) itemFriends.SetVisible(false);
                if (itemSearch != null) itemSearch.SetVisible(false);
                if (itemLogout != null) itemLogout.SetVisible(false);
            }

            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.nav_start:
                    if (!(this is MainActivity)) StartActivity(typeof(MainActivity));
                    return true;
                case Resource.Id.nav_signin:
                    if (!(this is SignIn)) StartActivity(typeof(SignIn));
                    return true;
                case Resource.Id.nav_signup:
                    if (!(this is SignUp)) StartActivity(typeof(SignUp));
                    return true;
                case Resource.Id.nav_home:
                    if (!(this is HomePage)) StartActivity(typeof(HomePage));
                    return true;
                case Resource.Id.nav_profile:
                    if (!(this is ProfileActivity)) StartActivity(typeof(ProfileActivity));
                    return true;
                case Resource.Id.nav_friends:
                    if (!(this is FriendsActivity)) StartActivity(typeof(FriendsActivity));
                    return true;
                case Resource.Id.nav_user_search:
                    if (!(this is UserSearchActivity)) StartActivity(typeof(UserSearchActivity));
                    return true;
                case Resource.Id.nav_logout:
                    ISharedPreferences prefs = GetSharedPreferences("user_prefs", FileCreationMode.Private);
                    prefs.Edit().Clear().Apply();
                    Toast.MakeText(this, "Logged Out", ToastLength.Short).Show();
                    InvalidateOptionsMenu();
                    StartActivity(typeof(MainActivity));
                    FinishAffinity();
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }
    }
}