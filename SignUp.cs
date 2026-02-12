using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using SQLite;
using System;
using System.Text.RegularExpressions;
using AndroidX.AppCompat.Widget;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace FinalProject331406710
{
    [Activity(Label = "SignUp")]
    public class SignUp : Base
    {
        EditText editTextId, editTextFullName, editTextEmail, editTextPassword, editTextConfirmPassword;
        TextView textViewDob, errorSummary;
        DateTime? selectedDob;
        public int age;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Note: Login check removed so users can create new accounts while logged in.
            SetContentView(Resource.Layout.SignUpLayout);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Sign Up";

            // Find Views
            editTextId = FindViewById<EditText>(Resource.Id.editTextId);
            editTextFullName = FindViewById<EditText>(Resource.Id.editTextFullName);
            editTextEmail = FindViewById<EditText>(Resource.Id.editTextEmail);
            editTextPassword = FindViewById<EditText>(Resource.Id.editTextPassword);
            editTextConfirmPassword = FindViewById<EditText>(Resource.Id.editTextConfirmPassword);
            textViewDob = FindViewById<TextView>(Resource.Id.textViewDob);
            errorSummary = FindViewById<TextView>(Resource.Id.errorSummary);
            var buttonRegister = FindViewById<Button>(Resource.Id.buttonRegister);

            // Enable Eye Icons for BOTH Password fields
            Helper.SetupPasswordVisibilityToggle(editTextPassword);
            Helper.SetupPasswordVisibilityToggle(editTextConfirmPassword);

            // Date Picker Logic
            textViewDob.Click += (s, e) =>
            {
                DatePickerDialog dialog = new DatePickerDialog(this, (sender, args) =>
                {
                    selectedDob = args.Date;
                    textViewDob.Text = selectedDob.Value.ToString("yyyy-MM-dd");
                    DateTime today = DateTime.Today;
                    age = today.Year - selectedDob.Value.Year;
                    if (selectedDob.Value.Date > today.AddYears(-age))
                    {
                        age--;
                    }
                }, 2000, 0, 1);
                dialog.Show();
            };

            buttonRegister.Click += ValidateForm;
            Helper.Initialize(this);
        }

        void ValidateForm(object sender, EventArgs e)
        {
            bool isValid = true;
            errorSummary.Visibility = ViewStates.Gone;

            void ShowError(int resId, string message)
            {
                var textView = FindViewById<TextView>(resId);
                textView.Text = message;
                textView.Visibility = ViewStates.Visible;
                isValid = false;
            }

            void HideError(int resId)
            {
                var textView = FindViewById<TextView>(resId);
                textView.Visibility = ViewStates.Gone;
            }

            // --- Validation Rules ---
            if (!Regex.IsMatch(editTextId.Text, @"^\d{9}$"))
                ShowError(Resource.Id.errorId, "ID must be 9 digits.");
            else
                HideError(Resource.Id.errorId);

            string fullName = editTextFullName.Text.Trim();
            if (fullName.Length < 2 || fullName.Length > 32)
                ShowError(Resource.Id.errorFullName, "Name must be between 2 and 32 characters.");
            else if (!Regex.IsMatch(fullName, @"^[a-zA-Z\s]+$"))
                ShowError(Resource.Id.errorFullName, "Name can only contain letters.");
            else
                HideError(Resource.Id.errorFullName);

            // --- AGE CHECK (18+) ---
            if (selectedDob == null)
                ShowError(Resource.Id.errorDob, "Please select a valid date of birth.");
            else if (age < 18)
                ShowError(Resource.Id.errorDob, "You must be 18+ to register for Poker.");
            else
                HideError(Resource.Id.errorDob);

            // --- EMAIL CHECK (Format + Duplicates) ---
            string email = editTextEmail.Text.Trim();
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                ShowError(Resource.Id.errorEmail, "Invalid email format.");
            else if (Helper.IsEmailExists(this, email))
                ShowError(Resource.Id.errorEmail, "This email is already registered.");
            else
                HideError(Resource.Id.errorEmail);

            string password = editTextPassword.Text;
            string confirmPassword = editTextConfirmPassword.Text;

            if (password.Length < 8 ||
                !Regex.IsMatch(password, @"[A-Z]") ||
                !Regex.IsMatch(password, @"[!@#$%^&*(),.?\':{ }|<>]"))
            {
                ShowError(Resource.Id.errorPassword, "Password must be 8+ chars, 1 uppercase, 1 special char.");
            }
            else
                HideError(Resource.Id.errorPassword);

            if (password != confirmPassword)
                ShowError(Resource.Id.errorConfirmPassword, "Passwords do not match.");
            else
                HideError(Resource.Id.errorConfirmPassword);

            if (!isValid)
            {
                errorSummary.Text = "Please correct the errors above and try again.";
                errorSummary.Visibility = ViewStates.Visible;
                return;
            }

            // Create User
            Users user = new Users(
                editTextId.Text,
                fullName,
                age,
                email,
                password
            );

            var db = Helper.Getdbcommand(this);

            try
            {
                int rowChange = db.Insert(user);
                if (rowChange > 0)
                {
                    // --- Logic: New User Created, Forget Old Session ---
                    var prefs = this.GetSharedPreferences("user_prefs", FileCreationMode.Private);
                    var editor = prefs.Edit();
                    editor.Remove("CURRENTLY_LOGGED_IN_ID"); // Logout old user
                    editor.Remove("REMEMBERED_ID"); // Clear old remember me
                    editor.Apply();

                    Toast.MakeText(this, "Registration successful! Please Sign In.", ToastLength.Long).Show();

                    StartActivity(typeof(SignIn));
                    Finish();
                }
                else
                {
                    Toast.MakeText(this, "User registration failed.", ToastLength.Long).Show();
                }
            }
            catch (SQLiteException ex)
            {
                Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Long).Show();
            }
        }
    }
}