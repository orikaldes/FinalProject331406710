using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using System;
using System.Linq;
using AndroidX.AppCompat.Widget;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FinalProject331406710
{
    [Activity(Label = "Friends Map")]
    public class MapActivity : Base, IOnMapReadyCallback
    {
        GoogleMap _googleMap;
        EditText _editCity;
        Button _btnUpdate;
        string _myId;

        // Optimized Web Client with a Timeout so it doesn't hang forever
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MapLayout);

            var toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Friends Map";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            _editCity = FindViewById<EditText>(Resource.Id.editCityLocation);
            _btnUpdate = FindViewById<Button>(Resource.Id.btnUpdateLocation);

            var prefs = GetSharedPreferences("user_prefs", FileCreationMode.Private);
            _myId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", "");

            var mapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.mapFragment);
            mapFragment.GetMapAsync(this);

            _btnUpdate.Click += OnUpdateLocationClick;

            // Header for OpenStreetMap
            if (client.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                client.DefaultRequestHeaders.Add("User-Agent", "MyPokerApp/1.0");
            }
        }

        // Standard Map Setup
        public void OnMapReady(GoogleMap googleMap)
        {
            _googleMap = googleMap;
            _googleMap.UiSettings.ZoomControlsEnabled = true;

            // Start the Async Loader
            // We use 'Fire and Forget' safely here because we handle errors inside
            LoadMapDataAsync();
        }

        // --- THE FIX: Clean Async/Await Pattern ---
        // No more 'RunOnUiThread' nested inside 'Task.Run'
        private async void LoadMapDataAsync()
        {
            try
            {
                // 1. Get Data (Runs in Background)
                // 'await' pauses this method here, lets the UI run free, and comes back when done.
                var data = await Task.Run(() =>
                {
                    var db = Helper.Getdbcommand(this);

                    // Get Me
                    var me = db.Table<Users>().Where(u => u.Id == _myId).FirstOrDefault();

                    // Get Friends
                    var friends = Helper.GetMyFriends(this, _myId);

                    return new { Me = me, Friends = friends };
                });

                // 2. Update UI (Runs on Main Thread automatically)
                if (_googleMap != null)
                {
                    _googleMap.Clear();

                    // Plot Me
                    if (data.Me != null && data.Me.Latitude != 0)
                    {
                        var myMarker = new MarkerOptions();
                        myMarker.SetPosition(new LatLng(data.Me.Latitude, data.Me.Longitude));
                        myMarker.SetTitle("Me");
                        myMarker.SetSnippet(data.Me.City);
                        myMarker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure)); // Blue
                        _googleMap.AddMarker(myMarker);

                        // Move Camera to me
                        _googleMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(data.Me.Latitude, data.Me.Longitude), 10));
                        _editCity.Text = data.Me.City;
                    }

                    // Plot Friends
                    foreach (var friend in data.Friends)
                    {
                        if (friend.Latitude != 0)
                        {
                            var markerOption = new MarkerOptions();
                            markerOption.SetPosition(new LatLng(friend.Latitude, friend.Longitude));
                            markerOption.SetTitle(friend.FullName);
                            markerOption.SetSnippet(friend.City);
                            // Default Red
                            _googleMap.AddMarker(markerOption);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log it but don't crash
                Console.WriteLine("Map Load Error: " + ex.Message);
            }
        }

        private async void OnUpdateLocationClick(object sender, EventArgs e)
        {
            string cityInput = _editCity.Text.Trim();
            if (string.IsNullOrEmpty(cityInput))
            {
                Toast.MakeText(this, "Please enter a city name.", ToastLength.Short).Show();
                return;
            }

            _btnUpdate.Enabled = false;
            _btnUpdate.Text = "Searching...";

            try
            {
                // 1. Search (Background)
                var coordinates = await GetCoordinatesFromCity(cityInput);

                if (coordinates != null)
                {
                    double lat = coordinates.Item1;
                    double lng = coordinates.Item2;

                    // 2. Save to DB (Background)
                    await Task.Run(() =>
                    {
                        var db = Helper.Getdbcommand(this);
                        var me = db.Table<Users>().Where(u => u.Id == _myId).FirstOrDefault();
                        if (me != null)
                        {
                            me.City = cityInput;
                            me.Latitude = lat;
                            me.Longitude = lng;
                            db.Update(me);
                        }
                    });

                    Toast.MakeText(this, $"Moved to {cityInput}!", ToastLength.Short).Show();

                    // 3. Refresh Map (Main Thread)
                    // We just call the loader again, simple and clean
                    LoadMapDataAsync();
                }
                else
                {
                    Toast.MakeText(this, "City not found. Check internet?", ToastLength.Long).Show();
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, "Error: " + ex.Message, ToastLength.Long).Show();
            }
            finally
            {
                _btnUpdate.Enabled = true;
                _btnUpdate.Text = "Update";
            }
        }

        private async Task<Tuple<double, double>> GetCoordinatesFromCity(string cityName)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // OpenStreetMap Search
                    string url = $"https://nominatim.openstreetmap.org/search?q={cityName}&format=json&limit=1";

                    // This 'await' releases the thread while waiting for the web
                    string jsonResponse = await client.GetStringAsync(url);

                    if (string.IsNullOrEmpty(jsonResponse) || jsonResponse == "[]") return null;

                    string latPattern = "\"lat\":\"([^\"]+)\"";
                    string lonPattern = "\"lon\":\"([^\"]+)\"";

                    var latMatch = Regex.Match(jsonResponse, latPattern);
                    var lonMatch = Regex.Match(jsonResponse, lonPattern);

                    if (latMatch.Success && lonMatch.Success)
                    {
                        double lat = double.Parse(latMatch.Groups[1].Value);
                        double lon = double.Parse(lonMatch.Groups[1].Value);
                        return new Tuple<double, double>(lat, lon);
                    }
                }
                catch (Exception)
                {
                    // Fail silently (Internet down, etc.)
                }
                return null;
            });
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return true;
        }
    }
}