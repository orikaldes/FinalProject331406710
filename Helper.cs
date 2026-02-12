using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;      // Required for Touch Events
using Android.Widget;
using Android.Text.Method; // Required for Password Hiding/Showing
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FinalProject331406710
{
    internal class Helper
    {
        public static string dbname = "Users";
        static SQLiteConnection dbCommand;

        public Helper() { }

        public static string Path(Context context)
        {
            return System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                dbname
            );
        }

        public static SQLiteConnection Getdbcommand(Context context)
        {
            string path = Path(context);
            dbCommand = new SQLiteConnection(path);
            return dbCommand;
        }

        public static void Initialize(Context context)
        {
            try
            {
                var dbCommand = Getdbcommand(context);
                dbCommand.CreateTable<Users>();
                dbCommand.CreateTable<Friendship>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        // --- AUTHENTICATION METHODS ---

        public static Users ValidateUser(Context context, string id, string password)
        {
            try
            {
                var db = Getdbcommand(context);
                return db.Table<Users>().Where(u => u.Id == id && u.Password == password).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Users GetUserByIdAndEmail(Context context, string id, string email)
        {
            try
            {
                var db = Getdbcommand(context);
                return db.Table<Users>().Where(u => u.Id == id && u.Email == email).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        // --- NEW: Check if email already exists ---
        public static bool IsEmailExists(Context context, string email)
        {
            try
            {
                var db = Getdbcommand(context);
                var user = db.Table<Users>().Where(u => u.Email == email).FirstOrDefault();
                return user != null;
            }
            catch
            {
                return false;
            }
        }

        public static List<Users> GetFilteredUsers(Context context, string nameFilter, string email, int minAge, int maxAge, bool descending)
        {
            try
            {
                var db = Getdbcommand(context);
                var query = db.Table<Users>();

                if (!string.IsNullOrEmpty(nameFilter))
                {
                    query = query.Where(u => u.FullName.Contains(nameFilter));
                }

                if (!string.IsNullOrEmpty(email))
                {
                    query = query.Where(u => u.Email.StartsWith(email));
                }

                if (minAge > 0)
                {
                    query = query.Where(u => u.Age >= minAge);
                }

                if (maxAge > 0)
                {
                    query = query.Where(u => u.Age <= maxAge);
                }

                if (descending)
                {
                    query = query.OrderByDescending(u => u.FullName);
                }
                else
                {
                    query = query.OrderBy(u => u.FullName);
                }

                return query.ToList();
            }
            catch (Exception)
            {
                return new List<Users>();
            }
        }

        // --- UI HELPER METHODS ---

        /// <summary>
        /// Adds an eye icon to the right of an EditText.
        /// Clicking it toggles the password visibility.
        /// </summary>
        public static void SetupPasswordVisibilityToggle(EditText editText)
        {
            // 1. Set the initial icon (Hidden/Slashed eye)
            // 0,0,ID,0 means: Start:None, Top:None, End:Icon, Bottom:None
            editText.SetCompoundDrawablesRelativeWithIntrinsicBounds(0, 0, Resource.Drawable.ic_eye_hidden, 0);

            // 2. Set the initial transformation (Hidden dots)
            editText.TransformationMethod = PasswordTransformationMethod.Instance;

            // 3. Handle the Touch Event
            editText.Touch += (sender, e) =>
            {
                var et = (EditText)sender;

                // Only react if the user lifted their finger (ActionUp)
                if (e.Event.Action == MotionEventActions.Up)
                {
                    // Index 2 is the "End" (Right side) drawable
                    var drawables = et.GetCompoundDrawablesRelative();

                    if (drawables[2] != null)
                    {
                        // Check if the touch happened on the icon
                        if (e.Event.RawX >= (et.Right - drawables[2].Bounds.Width()))
                        {
                            // Toggle Logic
                            int selectionStart = et.SelectionStart; // Save cursor position
                            int selectionEnd = et.SelectionEnd;

                            if (et.TransformationMethod is PasswordTransformationMethod)
                            {
                                // Currently Hidden -> Show Text
                                et.TransformationMethod = HideReturnsTransformationMethod.Instance;
                                et.SetCompoundDrawablesRelativeWithIntrinsicBounds(0, 0, Resource.Drawable.ic_eye_visible, 0);
                            }
                            else
                            {
                                // Currently Visible -> Hide Text
                                et.TransformationMethod = PasswordTransformationMethod.Instance;
                                et.SetCompoundDrawablesRelativeWithIntrinsicBounds(0, 0, Resource.Drawable.ic_eye_hidden, 0);
                            }

                            // Restore cursor position
                            et.SetSelection(selectionStart, selectionEnd);

                            e.Handled = true;
                            return;
                        }
                    }
                }
                // Allow normal typing behavior for other clicks
                e.Handled = false;
            };
        }


        // 1. Send a Request
        public static bool SendFriendRequest(Context context, string senderId, string receiverId)
        {
            var db = Getdbcommand(context);

            // Check if ANY relationship already exists (Pending OR Accepted)
            var existing = db.Table<Friendship>()
                .Where(f => (f.SenderId == senderId && f.ReceiverId == receiverId) ||
                            (f.SenderId == receiverId && f.ReceiverId == senderId))
                .FirstOrDefault();

            if (existing != null) return false; // Request already exists

            var newFriendship = new Friendship(senderId, receiverId, 0); // 0 = Pending
            db.Insert(newFriendship);
            return true;
        }

        // 2. Accept a Request
        public static void AcceptRequest(Context context, string senderId, string receiverId)
        {
            var db = Getdbcommand(context);
            var relation = db.Table<Friendship>()
                .Where(f => f.SenderId == senderId && f.ReceiverId == receiverId)
                .FirstOrDefault();

            if (relation != null)
            {
                relation.Status = 1; // 1 = Accepted
                db.Update(relation);
            }
        }

        // 3. Remove Friend / Decline Request
        public static void RemoveFriendship(Context context, string myId, string otherId)
        {
            var db = Getdbcommand(context);
            var relation = db.Table<Friendship>()
                .Where(f => (f.SenderId == myId && f.ReceiverId == otherId) ||
                            (f.SenderId == otherId && f.ReceiverId == myId))
                .FirstOrDefault();

            if (relation != null)
            {
                db.Delete(relation);
            }
        }

        // 4. Get List of "My Friends" (People where Status = 1)
        public static List<Users> GetMyFriends(Context context, string myId)
        {
            var db = Getdbcommand(context);
            var myFriends = new List<Users>();

            // Get all accepted friendships involving me
            var relations = db.Table<Friendship>()
                .Where(f => (f.SenderId == myId || f.ReceiverId == myId) && f.Status == 1)
                .ToList();

            foreach (var rel in relations)
            {
                // If I am the sender, the friend is the receiver (and vice versa)
                string friendId = (rel.SenderId == myId) ? rel.ReceiverId : rel.SenderId;

                var user = db.Table<Users>().Where(u => u.Id == friendId).FirstOrDefault();
                if (user != null) myFriends.Add(user);
            }
            return myFriends;
        }

        // 5. Get List of "Pending Requests" (People who sent ME a request, Status = 0)
        public static List<Users> GetPendingRequests(Context context, string myId)
        {
            var db = Getdbcommand(context);
            var requests = new List<Users>();

            // Only find rows where *I* am the receiver and status is Pending
            var relations = db.Table<Friendship>()
                .Where(f => f.ReceiverId == myId && f.Status == 0)
                .ToList();

            foreach (var rel in relations)
            {
                var user = db.Table<Users>().Where(u => u.Id == rel.SenderId).FirstOrDefault();
                if (user != null) requests.Add(user);
            }
            return requests;
        }

        // 6. Check status between two people (Used for the UI buttons)
        // Returns: -1 (Strangers), 0 (Pending), 1 (Friends)
        public static int GetFriendStatus(Context context, string myId, string otherId)
        {
            var db = Getdbcommand(context);
            var rel = db.Table<Friendship>()
                .Where(f => (f.SenderId == myId && f.ReceiverId == otherId) ||
                            (f.SenderId == otherId && f.ReceiverId == myId))
                .FirstOrDefault();

            if (rel == null) return -1;
            return rel.Status;
        }
    }
}