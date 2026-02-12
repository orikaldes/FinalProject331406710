using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Preferences;
using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace FinalProject331406710
{
    [Activity(Label = "SignIn")]
    public class SignIn : Base
    {
        // UI Controls
        private EditText editTextId;
        private EditText editTextPassword;
        private CheckBox checkBoxRememberMe;
        private Button buttonSignIn;
        private TextView textForgotPassword;

        // Data Storage
        private ISharedPreferences prefs;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // 1. Initialize 'prefs'
            prefs = this.GetSharedPreferences("user_prefs", FileCreationMode.Private);

            // 2. Check if already logged in (Security Check)
            string loggedInId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", null);
            if (!string.IsNullOrEmpty(loggedInId))
            {
                StartActivity(typeof(HomePage));
                Finish();
                return;
            }

            // 3. Load Layout
            SetContentView(Resource.Layout.SignInLayout);

            // 4. Setup Toolbar
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Sign In";

            // 5. Find Views
            editTextId = FindViewById<EditText>(Resource.Id.editTextSignInId);
            editTextPassword = FindViewById<EditText>(Resource.Id.editTextSignInPassword);
            checkBoxRememberMe = FindViewById<CheckBox>(Resource.Id.checkBoxRememberMe);
            buttonSignIn = FindViewById<Button>(Resource.Id.buttonSignIn);
            textForgotPassword = FindViewById<TextView>(Resource.Id.textForgotPassword);

            // --- NEW: Enable Eye Icon for Password ---
            Helper.SetupPasswordVisibilityToggle(editTextPassword);

            // 6. Load Remembered User
            LoadRememberedUser();

            // 7. Click Events
            buttonSignIn.Click += ButtonSignIn_Click;

            if (textForgotPassword != null)
            {
                textForgotPassword.Click += (sender, e) =>
                {
                    StartActivity(typeof(ForgotPasswordActivity));
                };
            }
        }

        private void LoadRememberedUser()
        {
            string rememberedId = prefs.GetString("REMEMBERED_ID", null);
            if (!string.IsNullOrEmpty(rememberedId))
            {
                editTextId.Text = rememberedId;
                checkBoxRememberMe.Checked = true;
            }
        }

        private void ButtonSignIn_Click(object sender, System.EventArgs e)
        {
            string id = editTextId.Text;
            string password = editTextPassword.Text;

            // Check database
            Users user = Helper.ValidateUser(this, id, password);

            if (user != null)
            {
                // Login Success
                HandleRememberMe(id);

                var editor = prefs.Edit();
                editor.PutString("CURRENTLY_LOGGED_IN_ID", user.Id);
                editor.Apply();

                Toast.MakeText(this, "Login Successful!", ToastLength.Short).Show();
                Intent intent = new Intent(this, typeof(HomePage));
                StartActivity(intent);
                FinishAffinity();
            }
            else
            {
                Toast.MakeText(this, "Invalid ID or Password.", ToastLength.Long).Show();
            }
        }

        private void HandleRememberMe(string id)
        {
            var editor = prefs.Edit();
            if (checkBoxRememberMe.Checked)
            {
                editor.PutString("REMEMBERED_ID", id);
            }
            else
            {
                editor.Remove("REMEMBERED_ID");
            }
            editor.Apply();
        }
    }
}