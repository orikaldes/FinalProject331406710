using Android.App; // Tool for creating screens
using Android.Content; // Tool for passing data
using Android.OS; // Tool for system commands
using Android.Views; // Tool for UI views
using AndroidX.AppCompat.App; // Tool for modern Android features
using Android.Widget; // Tool for standard widgets

namespace FinalProject331406710
{
    // The Parent Activity that all other pages inherit from
    // This handles the shared Menu logic
    public class Base : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        // 1. Create the Menu visual from the XML file
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MenuLayout, menu);
            return true;
        }

        // 2. Decide which buttons to hide or show
        // This runs every time the menu is about to open
        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            // Check if user is logged in by looking for an ID in storage
            ISharedPreferences prefs = GetSharedPreferences("user_prefs", FileCreationMode.Private);
            string currentId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", "");
            bool isLoggedIn = !string.IsNullOrEmpty(currentId);

            // Find all the buttons in the menu
            IMenuItem itemStart = menu.FindItem(Resource.Id.nav_start);
            IMenuItem itemSignIn = menu.FindItem(Resource.Id.nav_signin);
            IMenuItem itemSignUp = menu.FindItem(Resource.Id.nav_signup);

            IMenuItem itemHome = menu.FindItem(Resource.Id.nav_home);
            IMenuItem itemSearch = menu.FindItem(Resource.Id.nav_user_search);
            IMenuItem itemMap = menu.FindItem(Resource.Id.nav_map); // --- NEW ---
            IMenuItem itemProfile = menu.FindItem(Resource.Id.nav_profile);
            IMenuItem itemLogout = menu.FindItem(Resource.Id.nav_logout);

            // Toggle logic
            if (isLoggedIn)
            {
                // LOGGED IN: Show Features
                if (itemHome != null) itemHome.SetVisible(true);
                if (itemSearch != null) itemSearch.SetVisible(true);
                if (itemMap != null) itemMap.SetVisible(true); // --- NEW ---
                if (itemProfile != null) itemProfile.SetVisible(true);
                if (itemLogout != null) itemLogout.SetVisible(true);

                // Hide Login/Register
                if (itemStart != null) itemStart.SetVisible(false);
                if (itemSignIn != null) itemSignIn.SetVisible(false);
                if (itemSignUp != null) itemSignUp.SetVisible(false);
            }
            else
            {
                // LOGGED OUT: Show Login/Register
                if (itemStart != null) itemStart.SetVisible(true);
                if (itemSignIn != null) itemSignIn.SetVisible(true);
                if (itemSignUp != null) itemSignUp.SetVisible(true);

                // Hide Features
                if (itemHome != null) itemHome.SetVisible(false);
                if (itemSearch != null) itemSearch.SetVisible(false);
                if (itemMap != null) itemMap.SetVisible(false); // --- NEW ---
                if (itemProfile != null) itemProfile.SetVisible(false);
                if (itemLogout != null) itemLogout.SetVisible(false);
            }

            return base.OnPrepareOptionsMenu(menu);
        }

        // 3. Handle what happens when a button is clicked
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

                case Resource.Id.nav_user_search:
                    if (!(this is UserSearchActivity)) StartActivity(typeof(UserSearchActivity));
                    return true;

                // --- NEW: Go to Map Page ---
                case Resource.Id.nav_map:
                    if (!(this is MapActivity)) StartActivity(typeof(MapActivity));
                    return true;

                case Resource.Id.nav_profile:
                    if (!(this is ProfileActivity)) StartActivity(typeof(ProfileActivity));
                    return true;

                case Resource.Id.nav_logout:
                    // Clear User Data
                    ISharedPreferences prefs = GetSharedPreferences("user_prefs", FileCreationMode.Private);
                    ISharedPreferencesEditor editor = prefs.Edit();
                    editor.Clear();
                    editor.Apply();

                    Toast.MakeText(this, "Logged Out", ToastLength.Short).Show();

                    // Refresh Menu immediately
                    InvalidateOptionsMenu();

                    // Go to Login/Start
                    StartActivity(typeof(MainActivity));
                    FinishAffinity(); // Clear history stack so they can't go back
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }
    }
}