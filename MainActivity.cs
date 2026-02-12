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
            base.OnCreate(savedInstanceState); // Call base

            // 1. Set the content to YOUR layout
            SetContentView(Resource.Layout.activity_main);

            // 2. Find the toolbar
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar); // 3. Set it as the action bar

            SupportActionBar.Title = "Poker";

            // --- Your old code ---
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