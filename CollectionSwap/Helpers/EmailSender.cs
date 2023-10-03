using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;

namespace CollectionSwap.Helpers
{
    public class EmailSender
    {
        public static async Task SendEmailAsync(string toAddress, string subject, string body)
        {
            string smtpUsername = ConfigurationManager.AppSettings["smtpUsername"];
            string smtpPassword = ConfigurationManager.AppSettings["smtpPassword"];

            using (var smtpClient = new SmtpClient("smtp.gmail.com", 587))
            {
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = true;
                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(smtpUsername, "Swapper");
                mailMessage.To.Add(toAddress);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;

                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                    Console.WriteLine("Email sent successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to send email. Error: " + ex.Message);
                }
            }
        }
    }
}