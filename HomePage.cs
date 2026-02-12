using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;      // <-- NEW
using AndroidX.AppCompat.Widget;   // <-- NEW
using Android.Content;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar; 

namespace FinalProject331406710
{
    [Activity(Label = "HomePage")]
    public class HomePage : Base // <-- Still inherits from Base
    {
        // We no longer need 'CurrentPageMenuItemId'

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var prefs = this.GetSharedPreferences("user_prefs", FileCreationMode.Private);
            string loggedInId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", null);

            if (string.IsNullOrEmpty(loggedInId))
            {
                // User is NOT logged in. Send them to the start page.
                var intent = new Intent(this, typeof(MainActivity));
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
                StartActivity(intent);
                FinishAffinity();
                return; // Stop loading this page
            }

            // 1. Set the content to YOUR layout
            SetContentView(Resource.Layout.HomePageLayout); // <-- NEW

            // 2. Find and set the toolbar
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar); // <-- NEW
            SetSupportActionBar(toolbar); // <-- NEW

            Button goToSearchButton = FindViewById<Button>(Resource.Id.goToSearchButton);
            goToSearchButton.Click += (s, e) =>
            {
                // Start the new activity
                StartActivity(typeof(UserSearchActivity));
            };
        }
    }
}