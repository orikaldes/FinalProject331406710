#pragma warning disable CS0618 // Suppress SmtpClient warning

using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FinalProject331406710
{
    public static class EmailHelper
    {
        // CONSTANTS
        private const string SenderEmail = "YudBet4IroniA@gmail.com";
        // REPLACE THIS WITH YOUR REAL APP PASSWORD
        private const string SenderPassword = "YOUR_APP_PASSWORD_HERE";

        /// <summary>
        /// Sends an email in the background. Returns true if successful, false if failed.
        /// </summary>
        public static async Task<bool> SendEmailAsync(string recipientEmail, string subject, string body)
        {
            try
            {
                await Task.Run(() =>
                {
                    MailMessage mail = new MailMessage();
                    SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                    mail.From = new MailAddress(SenderEmail);
                    mail.To.Add(recipientEmail);
                    mail.Subject = subject;
                    mail.Body = body;

                    SmtpServer.Port = 587;
                    SmtpServer.Credentials = new NetworkCredential(SenderEmail, SenderPassword);
                    SmtpServer.EnableSsl = true;

                    SmtpServer.Send(mail);
                });
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Email Error: " + ex.Message);
                return false;
            }
        }
    }
}