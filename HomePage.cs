using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Net; // NEW: Required for Network Checking
using FinalProject331406710.Engine;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace FinalProject331406710
{
    [Activity(Label = "HomePage")]
    public class HomePage : Base
    {
        TextView _balanceText;
        Button _btnOffline;
        Button _btnOnline;
        Button _btnTutorial;
        Button _btnTrivia;
        Button _btnStats;

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

            SetContentView(Resource.Layout.HomePageLayout);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            _balanceText = FindViewById<TextView>(Resource.Id.homeBalanceText);
            _btnOffline = FindViewById<Button>(Resource.Id.btnOfflinePoker);
            _btnOnline = FindViewById<Button>(Resource.Id.btnOnlinePoker);
            _btnTutorial = FindViewById<Button>(Resource.Id.btnTutorial);
            _btnTrivia = FindViewById<Button>(Resource.Id.btnTrivia);
            _btnStats = FindViewById<Button>(Resource.Id.btnStats);

            // Wire the buttons
            _btnOffline.Click += (s, e) => ShowBuyInDialog(false);
            _btnOnline.Click += (s, e) => ShowBuyInDialog(true);

            _btnTutorial.Click += (s, e) => StartActivity(typeof(TutorialActivity));
            _btnTrivia.Click += (s, e) => StartActivity(typeof(TriviaActivity));
            _btnStats.Click += (s, e) => StartActivity(typeof(StatisticsActivity));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
                {
                    RequestPermissions(new string[] { Manifest.Permission.PostNotifications }, 101);
                }
            }

            StartBackgroundAlarm();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _balanceText.Text = UserWallet.Balance.ToString("N0");

            // --- NEW: Perform the Network Check every time the page loads ---
            if (IsNetworkAvailable())
            {
                _btnOnline.Enabled = true;
                _btnOnline.Text = "Play Online";
                _btnOnline.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#0000b3")); // Blue
            }
            else
            {
                _btnOnline.Enabled = false;
                _btnOnline.Text = "Play Online (No Internet)";
                _btnOnline.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#555555")); // Grey
            }
        }

        // --- NEW: Hardware Network Check Logic ---
        private bool IsNetworkAvailable()
        {
            ConnectivityManager cm = (ConnectivityManager)GetSystemService(Context.ConnectivityService);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                Network activeNetwork = cm.ActiveNetwork;
                if (activeNetwork == null) return false;
                NetworkCapabilities capabilities = cm.GetNetworkCapabilities(activeNetwork);
                return capabilities != null &&
                       (capabilities.HasTransport(Android.Net.TransportType.Wifi) ||
                        capabilities.HasTransport(Android.Net.TransportType.Cellular));
            }
            else
            {
#pragma warning disable CS0618
                NetworkInfo networkInfo = cm.ActiveNetworkInfo;
                return networkInfo != null && networkInfo.IsConnected;
#pragma warning restore CS0618
            }
        }

        private void StartBackgroundAlarm()
        {
            Intent alarmIntent = new Intent(this, typeof(FriendNotificationReceiver));
            PendingIntent pendingIntent = PendingIntent.GetBroadcast(this, 0, alarmIntent, PendingIntentFlags.Immutable);
            AlarmManager alarmManager = (AlarmManager)GetSystemService(Context.AlarmService);

            long interval = 60 * 1000;
            long triggerAt = SystemClock.ElapsedRealtime() + interval;
            alarmManager.SetInexactRepeating(AlarmType.ElapsedRealtimeWakeup, triggerAt, interval, pendingIntent);
        }

        private void ShowBuyInDialog(bool isOnline)
        {
            View dialogView = this.LayoutInflater.Inflate(Resource.Layout.buyin_dialog, null);

            var slider = dialogView.FindViewById<SeekBar>(Resource.Id.buyInSlider);
            var input = dialogView.FindViewById<EditText>(Resource.Id.buyInAmountInput);
            var btnJoin = dialogView.FindViewById<Button>(Resource.Id.btnJoinTable);
            var walletText = dialogView.FindViewById<TextView>(Resource.Id.walletBalanceText);

            // Change button color to match the mode
            if (isOnline) btnJoin.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#0000b3"));

            walletText.Text = $"Wallet: {UserWallet.Balance:N0}";

            int minBuyIn = 500;
            int maxBuyIn = UserWallet.Balance;

            if (maxBuyIn < minBuyIn)
            {
                Toast.MakeText(this, "Not enough coins!", ToastLength.Short).Show();
                return;
            }

            slider.Max = maxBuyIn - minBuyIn;
            slider.Progress = 0;
            input.Text = minBuyIn.ToString();

            slider.ProgressChanged += (s, e) =>
            {
                int val = minBuyIn + e.Progress;
                if (!input.HasFocus)
                    input.Text = val.ToString();
            };

            AndroidX.AppCompat.App.AlertDialog dialog = new AndroidX.AppCompat.App.AlertDialog.Builder(this)
                .SetView(dialogView)
                .Create();

            btnJoin.Click += (s, e) =>
            {
                if (int.TryParse(input.Text, out int amount))
                {
                    if (amount < minBuyIn || amount > maxBuyIn)
                    {
                        Toast.MakeText(this, "Invalid Amount", ToastLength.Short).Show();
                        return;
                    }

                    UserWallet.Balance -= amount;

                    // Route to the correct activity based on mode!
                    Intent intent;
                    if (isOnline)
                        intent = new Intent(this, typeof(OnlinePokerGameActivity));
                    else
                        intent = new Intent(this, typeof(PokerGameActivity));

                    intent.PutExtra("GAME_STACK", amount);
                    StartActivity(intent);

                    dialog.Dismiss();
                }
            };

            dialog.Show();
        }
    }
}