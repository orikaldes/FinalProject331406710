using Android.OS; // System android tools
using Android.Views; // Tools for handling screens and views
using Android.Widget; // Tools for UI elements like Buttons and Lists
using AndroidX.Fragment.App; // Tools for managing Fragments (mini-pages)
using System.Collections.Generic; // Tools for handling Lists of data
using Android.Content; // Tools for accessing the App Context

namespace FinalProject331406710
{
    // ---------------------------------------------------------
    // 1. THE BASE FRAGMENT
    // This class contains the logic that is SHARED by all 3 tabs.
    // ---------------------------------------------------------
    public abstract class BaseFriendFragment : Fragment
    {
        protected ListView _listView;
        protected EditText _searchBox;
        protected List<Users> _userList;
        protected FriendsAdapter _adapter;
        protected string _myId;
        protected TextView _emptyText;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.FragmentListLayout, container, false);
            _listView = view.FindViewById<ListView>(Resource.Id.listFriendsView);
            _searchBox = view.FindViewById<EditText>(Resource.Id.editSearchFriends);
            _emptyText = view.FindViewById<TextView>(Resource.Id.textEmptyState);

            var prefs = Activity.GetSharedPreferences("user_prefs", FileCreationMode.Private);
            _myId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", "");

            SetupView();

            return view;
        }

        protected abstract void SetupView();

        // --- NEW: A public method to force a reload without closing the app ---
        public void ForceReload()
        {
            if (_listView != null)
            {
                // 1. Save where the user has scrolled to
                var scrollState = _listView.OnSaveInstanceState();

                // 2. Re-run the data loading logic
                OnResume();

                // 3. Restore the scroll position so it doesn't jump to top
                _listView.OnRestoreInstanceState(scrollState);
            }
        }

        public void RefreshList(List<Users> users)
        {
            _userList = users;
            _adapter = new FriendsAdapter(Activity, _userList, _myId);

            _adapter.OnActionClick += (s, e) =>
            {
                ((FriendsActivity)Activity).HandleFriendAction(e.User, e.Action);
            };

            _listView.Adapter = _adapter;

            if (_userList.Count == 0)
            {
                _emptyText.Visibility = ViewStates.Visible;
                _listView.Visibility = ViewStates.Gone;

                if (this is MyFriendsFragment) _emptyText.Text = "You haven't added any friends yet.";
                else if (this is RequestsFragment) _emptyText.Text = "No pending requests.";
                else if (this is FindFriendsFragment) _emptyText.Text = "No users found.";
            }
            else
            {
                _emptyText.Visibility = ViewStates.Gone;
                _listView.Visibility = ViewStates.Visible;
            }
        }
    }

    // ---------------------------------------------------------
    // 2. TAB: MY FRIENDS
    // ---------------------------------------------------------
    public class MyFriendsFragment : BaseFriendFragment
    {
        protected override void SetupView()
        {
            _searchBox.Visibility = ViewStates.Gone;
        }

        public override void OnResume()
        {
            base.OnResume();
            var friends = Helper.GetMyFriends(Context, _myId);
            RefreshList(friends);
        }
    }

    // ---------------------------------------------------------
    // 3. TAB: REQUESTS
    // ---------------------------------------------------------
    public class RequestsFragment : BaseFriendFragment
    {
        protected override void SetupView()
        {
            _searchBox.Visibility = ViewStates.Gone;
        }

        public override void OnResume()
        {
            base.OnResume();
            var requests = Helper.GetPendingRequests(Context, _myId);
            RefreshList(requests);
        }
    }

    // ---------------------------------------------------------
    // 4. TAB: SEARCH / FIND FRIENDS
    // ---------------------------------------------------------
    public class FindFriendsFragment : BaseFriendFragment
    {
        protected override void SetupView()
        {
            _searchBox.Visibility = ViewStates.Visible;

            _searchBox.TextChanged += (s, e) =>
            {
                string query = _searchBox.Text;
                LoadUsers(query);
            };
        }

        public override void OnResume()
        {
            base.OnResume();
            // Note: We use the current text in the search box to keep the filter active
            string currentQuery = _searchBox.Text ?? "";
            LoadUsers(currentQuery);
        }

        private void LoadUsers(string query)
        {
            var allUsers = Helper.GetFilteredUsers(Context, query, "", 0, 0, false);
            allUsers.RemoveAll(u => u.Id == _myId);
            RefreshList(allUsers);
        }
    }
}