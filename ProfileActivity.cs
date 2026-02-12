using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using System;
using System.Text.RegularExpressions;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Android.Provider;
using AndroidX.Core.Content;
using Android.Graphics;
using Java.IO;
using Android.Content.PM;
using Android;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http; // Required for City Search

namespace FinalProject331406710
{
    [Activity(Label = "My Profile")]
    public class ProfileActivity : Base, IOnMapReadyCallback
    {
        // UI Controls
        ImageView imgProfile;
        TextView textNameHeader, textFullName, textEmail, textAge, textCity;
        ImageButton btnEditName, btnEditEmail, btnEditAge, btnEditCity;
        Button btnFriends, btnChangePassword;
        GoogleMap _googleMap;

        // Data
        Users currentUser;
        string _currentId;

        // Camera/Gallery Variables
        private File _photoFile;
        private Android.Net.Uri _photoUri;
        const int CAMERA_REQUEST_CODE = 101;
        const int GALLERY_REQUEST_CODE = 102;
        const int PERMISSION_REQUEST_CAMERA = 200;

        // Web Client for City Search (with 5 second timeout)
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ProfileLayout);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "My Profile";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            // 1. Initialize Views
            imgProfile = FindViewById<ImageView>(Resource.Id.imgProfile);
            textNameHeader = FindViewById<TextView>(Resource.Id.textProfileNameHeader);
            textFullName = FindViewById<TextView>(Resource.Id.textFullName);
            textEmail = FindViewById<TextView>(Resource.Id.textEmail);
            textAge = FindViewById<TextView>(Resource.Id.textAge);
            textCity = FindViewById<TextView>(Resource.Id.textCity);

            btnEditName = FindViewById<ImageButton>(Resource.Id.btnEditName);
            btnEditEmail = FindViewById<ImageButton>(Resource.Id.btnEditEmail);
            btnEditAge = FindViewById<ImageButton>(Resource.Id.btnEditAge);
            btnEditCity = FindViewById<ImageButton>(Resource.Id.btnEditCity);

            btnFriends = FindViewById<Button>(Resource.Id.btnFriends);
            btnChangePassword = FindViewById<Button>(Resource.Id.btnChangePassword);

            // 2. Get User ID & Init Helper
            var prefs = GetSharedPreferences("user_prefs", FileCreationMode.Private);
            _currentId = prefs.GetString("CURRENTLY_LOGGED_IN_ID", "");
            Helper.Initialize(this);

            // 3. Load User Data (Async - won't freeze UI)
            LoadUserDataAsync();

            // 4. Load Map (Delayed - prevents entry lag)
            var mapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.mapFragmentProfile);

            // Wait 500ms for the page to settle, then load the map
            Task.Delay(500).ContinueWith(t =>
            {
                RunOnUiThread(() => mapFragment.GetMapAsync(this));
            });

            // 5. Setup Listeners
            imgProfile.Click += OnProfilePictureClick;
            btnEditName.Click += (s, e) => ShowEditDialog("Full Name", textFullName.Text, "fullName");
            btnEditEmail.Click += (s, e) => ShowEditDialog("Email", textEmail.Text, "eMail");
            btnEditCity.Click += OnEditCityClick; // New City Editor

            btnFriends.Click += (s, e) => StartActivity(typeof(FriendsActivity));
            btnChangePassword.Click += OnChangePasswordClick;

            btnEditAge.Click += (s, e) => ShowAgePicker();

            // Setup Header for OpenStreetMap
            if (client.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                client.DefaultRequestHeaders.Add("User-Agent", "MyPokerApp/1.0");
            }
        }

        // --- MAP LOGIC (Static Dashboard Mode) ---
        public void OnMapReady(GoogleMap googleMap)
        {
            _googleMap = googleMap;

            // Lock the map interactions (Lite Mode behavior)
            _googleMap.UiSettings.ScrollGesturesEnabled = false;
            _googleMap.UiSettings.ZoomGesturesEnabled = false;
            _googleMap.UiSettings.TiltGesturesEnabled = false;
            _googleMap.UiSettings.RotateGesturesEnabled = false;
            _googleMap.UiSettings.ZoomControlsEnabled = false;
            _googleMap.UiSettings.MapToolbarEnabled = false;

            LoadMapDataAsync();
        }

        private async void LoadMapDataAsync()
        {
            try
            {
                // Background: Fetch Friends
                var data = await Task.Run(() => Helper.GetMyFriends(this, _currentId));

                if (_googleMap != null)
                {
                    _googleMap.Clear();
                    LatLngBounds.Builder builder = new LatLngBounds.Builder();
                    bool hasPoints = false;

                    // Add Me (Blue)
                    if (currentUser != null && currentUser.Latitude != 0)
                    {
                        var pos = new LatLng(currentUser.Latitude, currentUser.Longitude);
                        _googleMap.AddMarker(new MarkerOptions().SetPosition(pos).SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure)));
                        builder.Include(pos);
                        hasPoints = true;
                    }

                    // Add Friends (Red)
                    foreach (var friend in data)
                    {
                        if (friend.Latitude != 0)
                        {
                            var pos = new LatLng(friend.Latitude, friend.Longitude);
                            _googleMap.AddMarker(new MarkerOptions().SetPosition(pos));
                            builder.Include(pos);
                            hasPoints = true;
                        }
                    }

                    // Smart Zoom
                    if (hasPoints)
                    {
                        try
                        {
                            LatLngBounds bounds = builder.Build();
                            // 50px padding is enough for a small card
                            _googleMap.MoveCamera(CameraUpdateFactory.NewLatLngBounds(bounds, 50));
                        }
                        catch
                        {
                            // Fallback if points are too close
                            if (currentUser != null && currentUser.Latitude != 0)
                            {
                                _googleMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(currentUser.Latitude, currentUser.Longitude), 10));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Map Error: " + ex.Message);
            }
        }

        // --- USER DATA LOADER (Async) ---
        private async void LoadUserDataAsync()
        {
            try
            {
                // Background: DB Read & Image Decode
                var result = await Task.Run(() =>
                {
                    var db = Helper.Getdbcommand(this);
                    var user = db.Table<Users>().Where(u => u.Id == _currentId).FirstOrDefault();

                    Bitmap profileBitmap = null;
                    if (user != null && !string.IsNullOrEmpty(user.ProfileImagePath))
                    {
                        File imgFile = new File(user.ProfileImagePath);
                        if (imgFile.Exists())
                        {
                            profileBitmap = BitmapFactory.DecodeFile(imgFile.AbsolutePath);
                        }
                    }

                    return new { User = user, Img = profileBitmap };
                });

                // Foreground: Update UI
                currentUser = result.User;
                if (currentUser != null)
                {
                    textNameHeader.Text = currentUser.FullName;
                    textFullName.Text = currentUser.FullName;
                    textEmail.Text = currentUser.Email;
                    textAge.Text = currentUser.Age.ToString();
                    textCity.Text = string.IsNullOrEmpty(currentUser.City) ? "Not Set" : currentUser.City;

                    if (result.Img != null)
                    {
                        imgProfile.SetImageBitmap(result.Img);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Load Error: " + ex.Message);
            }
        }

        // --- CITY EDITING LOGIC ---
        private void OnEditCityClick(object sender, EventArgs e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Update Location");

            EditText input = new EditText(this);
            input.Hint = "Enter City (e.g. London)";
            input.Text = currentUser.City;
            builder.SetView(input);

            builder.SetPositiveButton("Update", (s, args) => { }); // Overridden below
            builder.SetNegativeButton("Cancel", (s, args) => { });

            var dialog = builder.Create();
            dialog.Show();

            dialog.GetButton((int)DialogButtonType.Positive).Click += async (s, args) =>
            {
                string newCity = input.Text.Trim();
                if (string.IsNullOrEmpty(newCity)) return;

                Toast.MakeText(this, "Searching...", ToastLength.Short).Show();

                // 1. Search Async
                var coords = await GetCoordinatesFromCity(newCity);

                if (coords != null)
                {
                    // 2. Update Data
                    currentUser.City = newCity;
                    currentUser.Latitude = coords.Item1;
                    currentUser.Longitude = coords.Item2;

                    Helper.Getdbcommand(this).Update(currentUser);

                    // 3. Refresh UI
                    LoadUserDataAsync();
                    LoadMapDataAsync();

                    Toast.MakeText(this, "Location Updated!", ToastLength.Short).Show();
                    dialog.Dismiss();
                }
                else
                {
                    Toast.MakeText(this, "City not found. Try again.", ToastLength.Long).Show();
                }
            };
        }

        private async Task<Tuple<double, double>> GetCoordinatesFromCity(string cityName)
        {
            return await Task.Run(async () =>
            {
                try
                {
                    string url = $"https://nominatim.openstreetmap.org/search?q={cityName}&format=json&limit=1";
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
                catch (Exception) { }
                return null;
            });
        }

        // --- HELPERS (Age, Dialogs, Password) ---
        private void ShowAgePicker()
        {
            DatePickerDialog dialog = new DatePickerDialog(this, (sender, args) =>
            {
                DateTime selectedDate = args.Date;
                DateTime today = DateTime.Today;
                int newAge = today.Year - selectedDate.Year;
                if (selectedDate.Date > today.AddYears(-newAge)) newAge--;

                if (newAge < 18)
                {
                    Toast.MakeText(this, "Cannot update: You must be 18+.", ToastLength.Long).Show();
                    return;
                }

                currentUser.Age = newAge;
                Helper.Getdbcommand(this).Update(currentUser);
                LoadUserDataAsync();
            }, 2000, 0, 1);
            dialog.Show();
        }

        private void ShowEditDialog(string fieldName, string currentValue, string dbColumn)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle($"Edit {fieldName}");
            EditText input = new EditText(this);
            input.Text = currentValue;
            builder.SetView(input);
            builder.SetPositiveButton("Save", (s, args) =>
            {
                string newValue = input.Text.Trim();
                if (!string.IsNullOrEmpty(newValue))
                {
                    var db = Helper.Getdbcommand(this);
                    if (dbColumn == "fullName") currentUser.FullName = newValue;
                    if (dbColumn == "eMail") currentUser.Email = newValue;
                    db.Update(currentUser);
                    LoadUserDataAsync();
                }
            });
            builder.SetNegativeButton("Cancel", (s, args) => { });
            builder.Show();
        }

        private void OnChangePasswordClick(object sender, EventArgs e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Change Password");
            var view = LayoutInflater.Inflate(Resource.Layout.dialog_change_password, null);
            builder.SetView(view);

            var editCurrent = view.FindViewById<EditText>(Resource.Id.editCurrentPass);
            var editNew = view.FindViewById<EditText>(Resource.Id.editNewPass);
            var editConfirm = view.FindViewById<EditText>(Resource.Id.editConfirmNewPass);

            Helper.SetupPasswordVisibilityToggle(editCurrent);
            Helper.SetupPasswordVisibilityToggle(editNew);
            Helper.SetupPasswordVisibilityToggle(editConfirm);

            builder.SetPositiveButton("Change", (s, args) => { });
            builder.SetNegativeButton("Cancel", (s, args) => { });
            var dialog = builder.Create();
            dialog.Show();

            dialog.GetButton((int)DialogButtonType.Positive).Click += (s, args) =>
            {
                if (editCurrent.Text != currentUser.Password) { Toast.MakeText(this, "Wrong current password.", ToastLength.Long).Show(); return; }
                if (editNew.Text.Length < 8 || !Regex.IsMatch(editNew.Text, @"[A-Z]") || !Regex.IsMatch(editNew.Text, @"[!@#$%^&*(),.?\':{ }|<>]")) { Toast.MakeText(this, "Weak password.", ToastLength.Long).Show(); return; }
                if (editNew.Text != editConfirm.Text) { Toast.MakeText(this, "Mismatch.", ToastLength.Long).Show(); return; }

                currentUser.Password = editNew.Text;
                Helper.Getdbcommand(this).Update(currentUser);
                Toast.MakeText(this, "Updated!", ToastLength.Short).Show();
                dialog.Dismiss();
            };
        }

        // --- CAMERA & GALLERY LOGIC ---
        private void OnProfilePictureClick(object sender, EventArgs e)
        {
            string[] options = { "Take a Picture", "Upload from Gallery" };
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetTitle("Change Profile Picture");
            builder.SetItems(options, (s, args) =>
            {
                if (args.Which == 0) CheckCameraPermissionAndTakePhoto();
                else OpenGallery();
            });
            builder.Show();
        }

        private void CheckCameraPermissionAndTakePhoto()
        {
            if (CheckSelfPermission(Manifest.Permission.Camera) != Permission.Granted)
                RequestPermissions(new string[] { Manifest.Permission.Camera }, PERMISSION_REQUEST_CAMERA);
            else TakePhoto();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == PERMISSION_REQUEST_CAMERA && grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                TakePhoto();
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void TakePhoto()
        {
            try
            {
                Intent intent = new Intent(MediaStore.ActionImageCapture);
                _photoFile = new File(GetExternalFilesDir(Android.OS.Environment.DirectoryPictures), $"profile_{currentUser.Id}.jpg");
                _photoUri = FileProvider.GetUriForFile(this, "com.companyname.finalproject331406710.fileprovider", _photoFile);
                intent.PutExtra(MediaStore.ExtraOutput, _photoUri);
                StartActivityForResult(intent, CAMERA_REQUEST_CODE);
            }
            catch (Exception ex) { Toast.MakeText(this, "Camera Error: " + ex.Message, ToastLength.Long).Show(); }
        }

        private void OpenGallery()
        {
            Intent intent = new Intent();
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(Intent.CreateChooser(intent, "Select Picture"), GALLERY_REQUEST_CODE);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Result.Ok)
            {
                string finalPath = "";
                if (requestCode == CAMERA_REQUEST_CODE) finalPath = _photoFile.AbsolutePath;
                else if (requestCode == GALLERY_REQUEST_CODE) finalPath = CopyGalleryImageToApp(data.Data);

                if (!string.IsNullOrEmpty(finalPath))
                {
                    currentUser.ProfileImagePath = finalPath;
                    Helper.Getdbcommand(this).Update(currentUser);
                    LoadUserDataAsync();
                }
            }
        }

        private string CopyGalleryImageToApp(Android.Net.Uri uri)
        {
            try
            {
                System.IO.Stream inputStream = ContentResolver.OpenInputStream(uri);
                File outputFile = new File(GetExternalFilesDir(Android.OS.Environment.DirectoryPictures), $"profile_gallery_{currentUser.Id}.jpg");
                System.IO.Stream outputStream = new System.IO.FileStream(outputFile.AbsolutePath, System.IO.FileMode.Create);
                inputStream.CopyTo(outputStream);
                inputStream.Close(); outputStream.Close();
                return outputFile.AbsolutePath;
            }
            catch { return null; }
        }

        public override bool OnSupportNavigateUp() { Finish(); return true; }
    }
}