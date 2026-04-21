using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using FinalProject331406710.Engine;
using Android.Content;

namespace FinalProject331406710
{
    // The Online Shell! 
    [Activity(Label = "Online Poker", Theme = "@style/AppTheme")]
    public class OnlinePokerGameActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // We reuse the exact same layout file to save space and keep it unified!
            SetContentView(Resource.Layout.PokerGameLayout);

            var toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Live Multiplayer (Coming Soon)";

            // Show a toast that this is under construction
            Toast.MakeText(this, "Searching for live server...", ToastLength.Long).Show();

            // TODO: In the future, write logic here to connect to the SQL Database,
            // pull real users into the table instead of bots, and sync the GameManager over the cloud!
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.game_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.action_leave_table)
            {
                int startingStack = Intent.GetIntExtra("GAME_STACK", 1000);
                UserWallet.Balance += startingStack; // Refund them since they couldn't play
                Finish();
                return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        [System.Obsolete]
        public override void OnBackPressed()
        {
            int startingStack = Intent.GetIntExtra("GAME_STACK", 1000);
            UserWallet.Balance += startingStack; // Refund them since they couldn't play
            Finish();
        }
    }
}