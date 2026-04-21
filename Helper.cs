using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Text.Method;
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

        // --- FIX: The static variable holds the connection forever ---
        static SQLiteConnection dbCommand;

        public Helper() { }

        public static string Path(Context context)
        {
            return System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                dbname
            );
        }

        // --- FIX: Only create a new connection if one doesn't exist yet! ---
        public static SQLiteConnection Getdbcommand(Context context)
        {
            if (dbCommand == null)
            {
                string path = Path(context);
                dbCommand = new SQLiteConnection(path);
            }
            return dbCommand;
        }

        public static void Initialize(Context context)
        {
            try
            {
                var dbCommand = Getdbcommand(context);
                dbCommand.CreateTable<Users>();
                dbCommand.CreateTable<Friendship>();
                dbCommand.CreateTable<UserHandStats>();
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

        // --- STATS LOGIC ---
        public static void RecordHandWin(Context context, string userId, string handType, int moneyWon)
        {
            try
            {
                var db = Getdbcommand(context);
                var stat = db.Table<UserHandStats>().Where(s => s.UserId == userId && s.HandType == handType).FirstOrDefault();

                if (stat == null)
                {
                    stat = new UserHandStats { UserId = userId, HandType = handType, TimesWon = 1, TotalMoneyWon = moneyWon };
                    db.Insert(stat);
                }
                else
                {
                    stat.TimesWon++;
                    stat.TotalMoneyWon += moneyWon;
                    db.Update(stat);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Stat Save Error: " + ex.Message);
            }
        }

        public static List<UserHandStats> GetPersonalStats(Context context, string userId)
        {
            try
            {
                var db = Getdbcommand(context);
                return db.Table<UserHandStats>().Where(s => s.UserId == userId).OrderByDescending(s => s.TotalMoneyWon).ToList();
            }
            catch
            {
                return new List<UserHandStats>();
            }
        }

        // --- UI HELPER METHODS ---
        public static void SetupPasswordVisibilityToggle(EditText editText)
        {
            editText.SetCompoundDrawablesRelativeWithIntrinsicBounds(0, 0, Resource.Drawable.ic_eye_hidden, 0);
            editText.TransformationMethod = PasswordTransformationMethod.Instance;
            editText.Touch += (sender, e) =>
            {
                var et = (EditText)sender;
                if (e.Event.Action == MotionEventActions.Up)
                {
                    var drawables = et.GetCompoundDrawablesRelative();
                    if (drawables[2] != null)
                    {
                        if (e.Event.RawX >= (et.Right - drawables[2].Bounds.Width()))
                        {
                            int selectionStart = et.SelectionStart;
                            int selectionEnd = et.SelectionEnd;
                            if (et.TransformationMethod is PasswordTransformationMethod)
                            {
                                et.TransformationMethod = HideReturnsTransformationMethod.Instance;
                                et.SetCompoundDrawablesRelativeWithIntrinsicBounds(0, 0, Resource.Drawable.ic_eye_visible, 0);
                            }
                            else
                            {
                                et.TransformationMethod = PasswordTransformationMethod.Instance;
                                et.SetCompoundDrawablesRelativeWithIntrinsicBounds(0, 0, Resource.Drawable.ic_eye_hidden, 0);
                            }
                            et.SetSelection(selectionStart, selectionEnd);
                            e.Handled = true;
                            return;
                        }
                    }
                }
                e.Handled = false;
            };
        }


        // --- FRIENDS LOGIC ---
        public static bool SendFriendRequest(Context context, string senderId, string receiverId)
        {
            var db = Getdbcommand(context);
            var existing = db.Table<Friendship>()
                .Where(f => (f.SenderId == senderId && f.ReceiverId == receiverId) ||
                            (f.SenderId == receiverId && f.ReceiverId == senderId))
                .FirstOrDefault();
            if (existing != null) return false;

            var newFriendship = new Friendship(senderId, receiverId, 0);
            db.Insert(newFriendship);
            return true;
        }

        public static void AcceptRequest(Context context, string senderId, string receiverId)
        {
            var db = Getdbcommand(context);
            var relation = db.Table<Friendship>()
                .Where(f => f.SenderId == senderId && f.ReceiverId == receiverId)
                .FirstOrDefault();
            if (relation != null)
            {
                relation.Status = 1;
                db.Update(relation);
            }
        }

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

        public static List<Users> GetMyFriends(Context context, string myId)
        {
            var db = Getdbcommand(context);
            var myFriends = new List<Users>();

            var relations = db.Table<Friendship>()
                .Where(f => (f.SenderId == myId || f.ReceiverId == myId) && f.Status == 1)
                .ToList();
            foreach (var rel in relations)
            {
                string friendId = (rel.SenderId == myId) ?
                rel.ReceiverId : rel.SenderId;

                var user = db.Table<Users>().Where(u => u.Id == friendId).FirstOrDefault();
                if (user != null) myFriends.Add(user);
            }
            return myFriends;
        }

        public static List<Users> GetPendingRequests(Context context, string myId)
        {
            var db = Getdbcommand(context);
            var requests = new List<Users>();

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