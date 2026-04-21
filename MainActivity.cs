using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget; // <-- Add this for Toolbar
using AndroidX.AppCompat.App;
using System;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace FinalProject331406710
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : Base // <-- Still inherits from Base
    {
        // We don't need 'CurrentPageMenuItemId' anymore!
        // You can delete that line from all files.

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            // FIX: Automatically pulls your app name from strings.xml!
            SupportActionBar.Title = GetString(Resource.String.app_name);

            Button buttonGoToSignIn = FindViewById<Button>(Resource.Id.buttonGoToSignIn);
            Button buttonGoToSignUp = FindViewById<Button>(Resource.Id.buttonGoToSignUp);

            buttonGoToSignIn.Click += (s, e) =>
            {
                StartActivity(typeof(SignIn));
            };

            buttonGoToSignUp.Click += (s, e) =>
            {
                StartActivity(typeof(SignUp));
            };
        }
    }
}