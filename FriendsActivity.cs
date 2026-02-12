using Android.App; // Tool for creating the screen
using Android.Content; // Tool for passing data between screens
using Android.OS; // Tool for system commands
using Android.Widget; // Tool for buttons and text
using AndroidX.AppCompat.App; // Tool for modern app features
using AndroidX.ViewPager2.Widget; // Tool for the swiping tabs
using Google.Android.Material.Tabs; // Tool for the top tab bar
using AndroidX.Fragment.App; // Explicit using for Fragments
using System; // Basic system tools
using Toolbar = AndroidX.AppCompat.Widget.Toolbar; // Tool for the top action bar
using AlertDialog = AndroidX.AppCompat.App.AlertDialog; // Explicit using for Dialogs

namespace FinalProject331406710
{
    [Activity(Label = "Friends")]
    public class FriendsActivity : AppCompatActivity
    {
        ViewPager2 _viewPager;
        TabLayout _tabLayout;
        FriendsPagerAdapter _pagerAdapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.FriendsPageLayout);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Friends";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            _viewPager = FindViewById<ViewPager2>(Resource.Id.friendsViewPager);
            _tabLayout = FindViewById<TabLayout>(Resource.Id.friendsTabLayout);

            _pagerAdapter = new FriendsPagerAdapter(this);
            _viewPager.Adapter = _pagerAdapter;

            _tabLayout.AddTab(_tabLayout.NewTab().SetText("My Friends"));
            _tabLayout.AddTab(_tabLayout.NewTab().SetText("Requests"));
            _tabLayout.AddTab(_tabLayout.NewTab().SetText("Find"));

            _tabLayout.AddOnTabSelectedListener(new MyTabListener(_viewPager));
            _viewPager.RegisterOnPageChangeCallback(new MyPageChangeCallback(_tabLayout));
        }

        // --- CENTRAL LOGIC HANDLER ---
        public void HandleFriendAction(Users user, string action)
        {
            var prefs = GetSharedPreferences("user_prefs", FileCreationMode.Private);
            string myId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", "");

            if (action == "ADD")
            {
                bool sent = Helper.SendFriendRequest(this, myId, user.Id);
                if (sent) Toast.MakeText(this, "Request Sent!", ToastLength.Short).Show();
                else Toast.MakeText(this, "Request already pending.", ToastLength.Short).Show();

                RefreshPage();
            }
            else if (action == "ACCEPT")
            {
                Helper.AcceptRequest(this, user.Id, myId);
                Toast.MakeText(this, "You are now friends!", ToastLength.Short).Show();
                RefreshPage();
            }
            else if (action == "REMOVE" || action == "DECLINE" || action == "CANCEL")
            {
                ShowRemoveConfirmation(user, myId, action);
            }
        }

        private void ShowRemoveConfirmation(Users user, string myId, string action)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);

            int status = Helper.GetFriendStatus(this, myId, user.Id);
            string title = "";
            string message = "";
            string buttonText = "";

            if (action == "DECLINE")
            {
                title = "Decline Request";
                message = $"Decline friend request from {user.FullName}?";
                buttonText = "Decline";
            }
            else if (action == "CANCEL")
            {
                title = "Cancel Request";
                message = $"Undo friend request to {user.FullName}?";
                buttonText = "Undo Request";
            }
            else if (status == 1)
            {
                title = "Remove Friend";
                message = $"Remove {user.FullName} from your friends list?";
                buttonText = "Remove";
            }
            else
            {
                title = "Remove";
                message = $"Remove {user.FullName}?";
                buttonText = "Remove";
            }

            builder.SetTitle(title);
            builder.SetMessage(message);

            builder.SetPositiveButton(buttonText, (s, e) =>
            {
                Helper.RemoveFriendship(this, myId, user.Id);
                Toast.MakeText(this, "Done.", ToastLength.Short).Show();
                RefreshPage();
            });

            builder.SetNegativeButton("Back", (s, e) => { });
            builder.Show();
        }

        // --- NEW REFRESH LOGIC (No Flickering!) ---
        private void RefreshPage()
        {
            // Instead of restarting the Activity, we loop through the active Fragments
            // and tell them to reload their data.
            foreach (var fragment in SupportFragmentManager.Fragments)
            {
                // Check if the fragment is one of ours and is currently alive
                if (fragment is BaseFriendFragment friendFragment && fragment.IsResumed)
                {
                    friendFragment.ForceReload();
                }
            }
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return true;
        }

        // --- INTERNAL HELPER CLASSES ---

        class FriendsPagerAdapter : AndroidX.ViewPager2.Adapter.FragmentStateAdapter
        {
            public FriendsPagerAdapter(FragmentActivity activity) : base(activity) { }
            public override int ItemCount => 3;
            public override AndroidX.Fragment.App.Fragment CreateFragment(int position)
            {
                switch (position)
                {
                    case 0: return new MyFriendsFragment();
                    case 1: return new RequestsFragment();
                    case 2: return new FindFriendsFragment();
                    default: return new MyFriendsFragment();
                }
            }
        }

        class MyTabListener : Java.Lang.Object, TabLayout.IOnTabSelectedListener
        {
            private ViewPager2 _pager;
            public MyTabListener(ViewPager2 pager) { _pager = pager; }
            public void OnTabReselected(TabLayout.Tab tab) { }
            public void OnTabSelected(TabLayout.Tab tab) { _pager.CurrentItem = tab.Position; }
            public void OnTabUnselected(TabLayout.Tab tab) { }
        }

        class MyPageChangeCallback : ViewPager2.OnPageChangeCallback
        {
            private TabLayout _tabs;
            public MyPageChangeCallback(TabLayout tabs) { _tabs = tabs; }
            public override void OnPageSelected(int position)
            {
                base.OnPageSelected(position);
                var tab = _tabs.GetTabAt(position);
                if (tab != null) tab.Select();
            }
        }
    }
}