using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using FinalProject331406710.Engine;
using System.Collections.Generic;

namespace FinalProject331406710
{
    [Activity(Label = "Statistics", Theme = "@style/AppTheme")]
    public class StatisticsActivity : Base
    {
        Button _btnTabMine, _btnTabWorld;
        ScrollView _scrollMine;
        LinearLayout _listMine, _layoutWorld;
        string _myId;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.StatisticsLayout);

            var toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Hall of Fame";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            var prefs = GetSharedPreferences("user_prefs", Android.Content.FileCreationMode.Private);
            _myId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", "");

            _btnTabMine = FindViewById<Button>(Resource.Id.btnTabMine);
            _btnTabWorld = FindViewById<Button>(Resource.Id.btnTabWorld);
            _scrollMine = FindViewById<ScrollView>(Resource.Id.scrollMine);
            _listMine = FindViewById<LinearLayout>(Resource.Id.listMine);
            _layoutWorld = FindViewById<LinearLayout>(Resource.Id.layoutWorld);

            _btnTabMine.Click += (s, e) => SwitchTab(true);
            _btnTabWorld.Click += (s, e) => SwitchTab(false);

            LoadPersonalStats();
        }

        private void SwitchTab(bool isMine)
        {
            if (isMine)
            {
                _scrollMine.Visibility = ViewStates.Visible;
                _layoutWorld.Visibility = ViewStates.Gone;

                _btnTabMine.SetBackgroundColor(Color.ParseColor("#222222"));
                _btnTabMine.SetTextColor(Color.White);

                _btnTabWorld.SetBackgroundColor(Color.ParseColor("#555555"));
                _btnTabWorld.SetTextColor(Color.ParseColor("#AAAAAA"));
            }
            else
            {
                _scrollMine.Visibility = ViewStates.Gone;
                _layoutWorld.Visibility = ViewStates.Visible;

                _btnTabWorld.SetBackgroundColor(Color.ParseColor("#222222"));
                _btnTabWorld.SetTextColor(Color.White);

                _btnTabMine.SetBackgroundColor(Color.ParseColor("#555555"));
                _btnTabMine.SetTextColor(Color.ParseColor("#AAAAAA"));
            }
        }

        private void LoadPersonalStats()
        {
            _listMine.RemoveAllViews();
            var stats = Helper.GetPersonalStats(this, _myId);

            if (stats.Count == 0)
            {
                var empty = new TextView(this)
                {
                    Text = "No wins recorded yet. Go play some Poker!",
                    TextSize = 18
                };
                empty.SetTextColor(Color.White);
                empty.Gravity = GravityFlags.Center;
                empty.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent) { TopMargin = 100 };
                _listMine.AddView(empty);
                return;
            }

            foreach (var stat in stats)
            {
                var row = new LinearLayout(this) { Orientation = Android.Widget.Orientation.Horizontal };
                row.SetPadding(0, 30, 0, 30);

                var title = new TextView(this) { Text = stat.HandType, TextSize = 22 };
                title.SetTextColor(Color.ParseColor("#FFFF00"));
                title.SetTypeface(null, TypefaceStyle.Bold);
                title.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1);
                title.Gravity = GravityFlags.CenterVertical;

                var details = new TextView(this) { Text = $"Won {stat.TimesWon}x\nTotal: {stat.TotalMoneyWon:N0} Coins", TextSize = 16 };
                details.SetTextColor(Color.White);
                details.Gravity = GravityFlags.Right | GravityFlags.CenterVertical;

                row.AddView(title);
                row.AddView(details);
                _listMine.AddView(row);

                var divider = new View(this) { LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 2) };
                divider.SetBackgroundColor(Color.ParseColor("#444444"));
                _listMine.AddView(divider);
            }
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return true;
        }
    }
}