using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using System.Collections.Generic;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace FinalProject331406710
{
    [Activity(Label = "Search Users")]
    public class UserSearchActivity : Base
    {
        // UI Controls
        EditText filterName, filterEmail, filterMinAge, filterMaxAge; // Changed filterFirstName/LastName to just filterName
        Spinner sortSpinner;
        Button searchButton;
        ListView userListView;

        // Data
        List<Users> userList;
        UserListAdapter listAdapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var prefs = this.GetSharedPreferences("user_prefs", FileCreationMode.Private);
            string loggedInId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", null);

            if (string.IsNullOrEmpty(loggedInId))
            {
                var intent = new Intent(this, typeof(MainActivity));
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
                StartActivity(intent);
                FinishAffinity();
                return;
            }

            SetContentView(Resource.Layout.UserSearchLayout);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Search Users";

            // --- FIX: Find the new single name filter ---
            // Make sure your XML has: android:id="@+id/filterName"
            filterName = FindViewById<EditText>(Resource.Id.filterName);

            filterEmail = FindViewById<EditText>(Resource.Id.filterEmail);
            filterMinAge = FindViewById<EditText>(Resource.Id.filterMinAge);
            filterMaxAge = FindViewById<EditText>(Resource.Id.filterMaxAge);
            sortSpinner = FindViewById<Spinner>(Resource.Id.sortSpinner);
            searchButton = FindViewById<Button>(Resource.Id.searchButton);
            userListView = FindViewById<ListView>(Resource.Id.userListView);

            var sortOptions = new List<string> { "Ascending", "Descending" };
            var spinnerAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, sortOptions);
            sortSpinner.Adapter = spinnerAdapter;

            LoadUsers();

            searchButton.Click += (s, e) => LoadUsers();
            userListView.ItemClick += UserListView_ItemClick;
        }

        void LoadUsers()
        {
            // --- FIX: Get text from the single name filter ---
            string name = filterName.Text;
            string email = filterEmail.Text;

            int.TryParse(filterMinAge.Text, out int minAge);
            int.TryParse(filterMaxAge.Text, out int maxAge);

            bool descending = sortSpinner.SelectedItemPosition == 1;

            // --- FIX: Call Helper with the new variables ---
            userList = Helper.GetFilteredUsers(this, name, email, minAge, maxAge, descending);

            listAdapter = new UserListAdapter(this, userList);
            userListView.Adapter = listAdapter;
        }

        private void UserListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Users selectedUser = userList[e.Position];

            // --- FIX: Use new PascalCase property names (Email, Age, Password, FullName) ---
            string message = $"ID: {selectedUser.Id}\n" +
                             $"Email: {selectedUser.Email}\n" +
                             $"Age: {selectedUser.Age}\n" +
                             $"Password: {selectedUser.Password}";

            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle(selectedUser.FullName); // Use FullName
            builder.SetMessage(message);
            builder.SetPositiveButton("Close", (dialog, which) => { });
            builder.Show();
        }
    }
}