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
using System.Net.Http;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;

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

        // --- NEW FORMAT: Activity Result Launchers ---
        private ActivityResultLauncher _cameraLauncher;
        private ActivityResultLauncher _galleryLauncher;
        private File _photoFile;
        private Android.Net.Uri _photoUri;
        const int PERMISSION_REQUEST_CAMERA = 200;

        // Web Client for City Search
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        // Helper Class required for C# Implementation of the New ActivityResult callback
        private class ActivityResultCallback : Java.Lang.Object, IActivityResultCallback
        {
            public Action<Java.Lang.Object> OnResultAction { get; set; }
            public void OnActivityResult(Java.Lang.Object result)
            {
                OnResultAction?.Invoke(result);
            }
        }

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

            // --- 3. REGISTER THE NEW ACTIVITY RESULT LAUNCHERS (BAGRUT REQUIREMENT) ---

            // A. Camera Launcher
            _cameraLauncher = RegisterForActivityResult(
                new ActivityResultContracts.StartActivityForResult(),
                new ActivityResultCallback
                {
                    OnResultAction = resultObj =>
                    {
                        var result = resultObj as ActivityResult;
                        if (result.ResultCode == (int)Result.Ok)
                        {
                            string finalPath = _photoFile.AbsolutePath;
                            if (!string.IsNullOrEmpty(finalPath))
                            {
                                currentUser.ProfileImagePath = finalPath;
                                Helper.Getdbcommand(this).Update(currentUser);
                                LoadUserDataAsync();
                            }
                        }
                    }
                }
            );

            // B. Gallery Launcher
            _galleryLauncher = RegisterForActivityResult(
                new ActivityResultContracts.StartActivityForResult(),
                new ActivityResultCallback
                {
                    OnResultAction = resultObj =>
                    {
                        var result = resultObj as ActivityResult;
                        if (result.ResultCode == (int)Result.Ok && result.Data != null)
                        {
                            string finalPath = CopyGalleryImageToApp(result.Data.Data);
                            if (!string.IsNullOrEmpty(finalPath))
                            {
                                currentUser.ProfileImagePath = finalPath;
                                Helper.Getdbcommand(this).Update(currentUser);
                                LoadUserDataAsync();
                            }
                        }
                    }
                }
            );
            // --------------------------------------------------------------------------

            // 4. Load User Data
            LoadUserDataAsync();

            // 5. Load Map
            var mapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.mapFragmentProfile);
            Task.Delay(800).ContinueWith(t =>
            {
                RunOnUiThread(() => mapFragment?.GetMapAsync(this));
            });

            // 6. Setup Listeners
            imgProfile.Click += OnProfilePictureClick;
            btnEditName.Click += (s, e) => ShowEditDialog("Full Name", textFullName.Text, "fullName");
            btnEditEmail.Click += (s, e) => ShowEditDialog("Email", textEmail.Text, "eMail");
            btnEditCity.Click += OnEditCityClick;

            btnFriends.Click += (s, e) => StartActivity(typeof(FriendsActivity));
            btnChangePassword.Click += OnChangePasswordClick;

            btnEditAge.Click += (s, e) => ShowAgePicker();

            if (client.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                client.DefaultRequestHeaders.Add("User-Agent", "MyPokerApp/1.0");
            }
        }

        // --- MAP LOGIC (Static Dashboard Mode) ---
        public void OnMapReady(GoogleMap googleMap)
        {
            _googleMap = googleMap;
            if (_googleMap == null) return;

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
            if (_googleMap == null) return;

            try
            {
                var data = await Task.Run(() => Helper.GetMyFriends(this, _currentId));

                if (_googleMap == null) return;

                _googleMap.Clear();
                LatLngBounds.Builder builder = new LatLngBounds.Builder();
                bool hasPoints = false;
                int friendCount = 0;
                bool hasUserLocation = currentUser != null && currentUser.Latitude != 0;

                // 1. Add Me (Blue)
                if (hasUserLocation)
                {
                    var pos = new LatLng(currentUser.Latitude, currentUser.Longitude);
                    _googleMap.AddMarker(new MarkerOptions().SetPosition(pos).SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure)));
                    builder.Include(pos);
                    hasPoints = true;
                }

                // 2. Add Friends (Red)
                foreach (var friend in data)
                {
                    if (friend.Latitude != 0)
                    {
                        var pos = new LatLng(friend.Latitude, friend.Longitude);
                        _googleMap.AddMarker(new MarkerOptions().SetPosition(pos));
                        builder.Include(pos);
                        hasPoints = true;
                        friendCount++;
                    }
                }

                // 3. Smart Camera Logic
                if (hasPoints)
                {
                    if (friendCount == 0 && hasUserLocation)
                    {
                        _googleMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(currentUser.Latitude, currentUser.Longitude), 7.5f));
                    }
                    else
                    {
                        try
                        {
                            LatLngBounds bounds = builder.Build();
                            _googleMap.MoveCamera(CameraUpdateFactory.NewLatLngBounds(bounds, 50));
                        }
                        catch
                        {
                            _googleMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(currentUser.Latitude, currentUser.Longitude), 7.5f));
                        }
                    }
                }
                else
                {
                    _googleMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(31.0461, 34.8516), 7.5f));
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

            builder.SetPositiveButton("Update", (s, args) => { });
            builder.SetNegativeButton("Cancel", (s, args) => { });
            var dialog = builder.Create();
            dialog.Show();

            dialog.GetButton((int)DialogButtonType.Positive).Click += async (s, args) =>
            {
                string newCity = input.Text.Trim();
                if (string.IsNullOrEmpty(newCity)) return;

                Toast.MakeText(this, "Searching...", ToastLength.Short).Show();

                var coords = await GetCoordinatesFromCity(newCity);
                if (coords != null)
                {
                    currentUser.City = newCity;
                    currentUser.Latitude = coords.Item1;
                    currentUser.Longitude = coords.Item2;

                    Helper.Getdbcommand(this).Update(currentUser);

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
                    string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(cityName)}&format=json&limit=1";
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

                // Launch using the NEW Format
                _cameraLauncher.Launch(intent);
            }
            catch (Exception ex) { Toast.MakeText(this, "Camera Error: " + ex.Message, ToastLength.Long).Show(); }
        }

        private void OpenGallery()
        {
            Intent intent = new Intent();
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);

            // Launch using the NEW Format
            _galleryLauncher.Launch(Intent.CreateChooser(intent, "Select Picture"));
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

        // --- NEW: LITE MEMORY MANAGEMENT ---
        protected override void OnPause()
        {
            base.OnPause();
            _googleMap?.StopAnimation();
        }

        protected override void OnDestroy()
        {
            if (_googleMap != null)
            {
                _googleMap.Clear();
                _googleMap.Dispose();
                _googleMap = null;
            }
            base.OnDestroy();
            System.GC.Collect();
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return true;
        }
    }
}