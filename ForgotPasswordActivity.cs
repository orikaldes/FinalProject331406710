// --- FIX: Suppress the obsolete warning for SmtpClient ---
#pragma warning disable CS0618 

using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.AppCompat.App;
using System;
using System.Net;
using System.Net.Mail;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace FinalProject331406710
{
    [Activity(Label = "Forgot Password")]
    public class ForgotPasswordActivity : Base
    {
        EditText _editId, _editEmail;
        Button _btnSend;

        private const string SenderEmail = "YudBet4IroniA@gmail.com";
        private const string SenderPassword = "qhip imme dcek jgus";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.ForgotPasswordLayout);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Recover Password";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            _editId = FindViewById<EditText>(Resource.Id.editResetId);
            _editEmail = FindViewById<EditText>(Resource.Id.editResetEmail);
            _btnSend = FindViewById<Button>(Resource.Id.btnSendPassword);

            _btnSend.Click += OnSendClick;
        }

        private void OnSendClick(object sender, EventArgs e)
        {
            string id = _editId.Text.Trim();
            string email = _editEmail.Text.Trim();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(email))
            {
                Toast.MakeText(this, "Please fill in all fields.", ToastLength.Short).Show();
                return;
            }

            Users user = Helper.GetUserByIdAndEmail(this, id, email);

            if (user == null)
            {
                Toast.MakeText(this, "Details do not match our records.", ToastLength.Long).Show();
                return;
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // --- FIX: Use new property names (Email, Password, FullName) ---
                    SendEmail(user.Email, user.Password, user.FullName);

                    RunOnUiThread(() =>
                    {
                        Toast.MakeText(this, "Email Sent! Check your inbox.", ToastLength.Long).Show();
                        Finish();
                    });
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        Toast.MakeText(this, "Failed to send email: " + ex.Message, ToastLength.Long).Show();
                    });
                }
            });
        }

        private void SendEmail(string recipientEmail, string passwordToRecover, string fullName)
        {
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress(SenderEmail);
                mail.To.Add(recipientEmail);
                mail.Subject = "Your Poker App Password Recovery";
                // --- FIX: Use fullName variable ---
                mail.Body = $"Hello {fullName},\n\n" +
                            $"You requested a password recovery for the Poker App.\n\n" +
                            $"Your Password is: {passwordToRecover}\n\n" +
                            $"Please keep it safe or change it after logging in.\n\n" +
                            $"Regards,\nPoker App Team";

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new NetworkCredential(SenderEmail, SenderPassword);
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return true;
        }
    }
}