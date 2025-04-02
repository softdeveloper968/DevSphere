using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Text;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace MyDMVpro.Common
{
    public static class SmtpHelper
    {
        public static string SmtpFrom { get { return ConfigurationHelper.Configuration["Smtp:From"]; } }
        public static string SmtpServer { get { return ConfigurationHelper.Configuration["Smtp:Server"]; } }
        public static string SmtpUser { get { return ConfigurationHelper.Configuration["Smtp:User"]; } }
        public static string SmtpPassword { get { return ConfigurationHelper.Configuration["Smtp:Password"]; } }
        public static int SmtpPort { get { return ConfigurationHelper.Configuration.GetValue<int>("Smtp:Port"); } }
        public static bool EnableSsl { get { return ConfigurationHelper.Configuration.GetValue<bool>("Smtp:EnableSsl"); } }
        public static string BccAddress { get { return ConfigurationHelper.Configuration["Smtp:BccAddress"]; } }
        public static string FromDisplayName { get { return ConfigurationHelper.Configuration["Smtp:FromDisplayName"]; } }
        private const string c_DefaultInviteSubject = "MyDMV.Pro Inivitation";

        public static bool SendVendorInvite(string siteUrl, string from, string to, Guid inviteId, string vendorName)
        {
            try
            {
                var emailSettings = GetEmailSettingsForVendorInvite(siteUrl, inviteId, vendorName);

                using (SmtpClient client = GetClient())
                {
                    MailMessage message = new MailMessage();
                    message.From = new MailAddress(from ?? SmtpFrom, FromDisplayName);
                    message.To.Add(new MailAddress(to));
                    if (!string.IsNullOrWhiteSpace(emailSettings.CC))
                    {
                        try
                        {
                            message.CC.Add(emailSettings.CC);
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Logger.LogError(ex, "Error adding CC address");
                        }
                    }
                    if (!string.IsNullOrEmpty(BccAddress))
                        message.Bcc.Add(new MailAddress(BccAddress));
                    if (!string.IsNullOrWhiteSpace(emailSettings.BCC))
                    {
                        try
                        {
                            message.Bcc.Add(emailSettings.BCC);
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Logger.LogError(ex, "Error adding BCC address");
                        }
                    }
                    message.Subject = emailSettings.Subject;
                    message.IsBodyHtml = emailSettings.BodyIsHtml;
                    message.Body = emailSettings.Body;

                    client.Send(message);

                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Logger.LogError(ex, "Error sending vendor invite");
                return false;
            }
        }
        public static bool SendGroupInvite(string siteUrl, string from, string to, Guid inviteId, string groupName)
        {
            try
            {
                var emailSettings = GetEmailSettingsForGroupInvite(siteUrl, inviteId, groupName);

                using (SmtpClient client = GetClient())
                {
                    MailMessage message = new MailMessage();
                    message.From = new MailAddress(from ?? SmtpFrom, FromDisplayName);
                    message.To.Add(new MailAddress(to));
                    if (!string.IsNullOrWhiteSpace(emailSettings.CC))
                    {
                        try
                        {
                            message.CC.Add(emailSettings.CC);
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Logger.LogError(ex, "Error adding CC address");
                        }
                    }
                    if (!string.IsNullOrEmpty(BccAddress))
                        message.Bcc.Add(new MailAddress(BccAddress));
                    if (!string.IsNullOrWhiteSpace(emailSettings.BCC))
                    {
                        try
                        {
                            message.Bcc.Add(emailSettings.BCC);
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Logger.LogError(ex, "Error adding BCC address");
                        }
                    }
                    message.Subject = emailSettings.Subject;
                    message.IsBodyHtml = emailSettings.BodyIsHtml;
                    message.Body = emailSettings.Body;

                    client.Send(message);

                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Logger.LogError(ex, "Error sending vendor invite");
                return false;
            }
        }
        public class EmailSettings
        {
            public string From { get; set; }
            public string ReplyTo { get; set; }
            public string Subject { get; set; }
            public string CC { get; set; }
            public string BCC { get; set; }
            public string Body { get; set; }
            public bool BodyIsHtml { get; set; }
        }
        public static EmailSettings GetEmailSettingsForVendorInvite(string siteUrl, Guid inviteId, string vendorName)
        {
            dynamic settings = null;
            using (var context = new MaggardDMVContext())
            {
                var invite = context.VendorInvite.Where(v => v.Id == inviteId).FirstOrDefault();
                if (invite != null)
                {
                    var vsobj = context.VendorSettings.Where(vs => vs.VendorId == invite.VendorId).FirstOrDefault();
                    if (vsobj != null)
                    {
                        try
                        {
                            settings = JsonConvert.DeserializeObject<ExpandoObject>(vsobj.JSettings);
                            settings = settings?.VendorInvite;
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Logger.LogError(ex, "Error deserializing vendor JSettings");
                            settings = null;
                        }
                    }
                }
            }
            string bodyHtml;
            string subject = c_DefaultInviteSubject;
            if (string.IsNullOrWhiteSpace((string)settings?.Body))
            {
                bodyHtml = FormatVendorInviteBody(DefaultVendorInviteHtml, siteUrl, inviteId, vendorName);
            }
            else
            {
                bodyHtml = settings.Body;
            }
            if (!string.IsNullOrWhiteSpace((string)settings?.Subject))
            {
                subject = settings.Subject;
            }
            return new EmailSettings()
            {
                Subject = subject,
                CC = settings?.CC,
                BCC = settings?.BCC,
                Body = bodyHtml,
                BodyIsHtml = true
            };
        }
        public static EmailSettings GetEmailSettingsForGroupInvite(string siteUrl, Guid inviteId, string groupName)
        {
            dynamic settings = null;
            using (var context = new MaggardDMVContext())
            {
                var invite = context.GroupInvite.Where(v => v.Id == inviteId).FirstOrDefault();
                if (invite != null)
                {
                    var vsobj = context.VendorSettings.Where(vs => vs.VendorId == Guid.Empty).FirstOrDefault();
                    if (vsobj != null && vsobj.JSettings != null)
                    {
                        try
                        {
                            settings = JsonConvert.DeserializeObject<ExpandoObject>(vsobj.JSettings);
                            settings = settings?.GroupInvite;
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.Logger.LogError(ex, "Error deserializing group JSettings");
                            settings = null;
                        }
                    }
                }
            }
            string bodyHtml;
            string subject = c_DefaultInviteSubject;
            if (string.IsNullOrWhiteSpace((string)settings?.Body))
            {
                bodyHtml = FormatGroupInviteBody(DefaultGroupInviteBodyFormat, siteUrl, inviteId, groupName);
            }
            else 
            {
                bodyHtml = settings.Body;
            }
            if (!string.IsNullOrWhiteSpace((string)settings?.Subject))
            {
                subject = settings.Subject;
            }
            return new EmailSettings()
            {
                Subject = subject,
                CC = settings?.CC,
                BCC = settings?.BCC,
                Body = bodyHtml,
                BodyIsHtml = true
            };
        }
        private const string DefaultVendorInviteHtml = @"<div>
<p/>
<p>You have been invited to join the '{0}' vendor group at MyDmv.Pro</p>
<p>Please see the instructions <a href=""{3}"">here</a> on how to work with MyDMV.Pro. For more information, please email Drew Maggard at amaggard@maggard.net.</p>
<p>To accept this invitation, click this <a href=""{1}"">link</a> and follow the login instructions provided <a href=""{2}"">here</a>.</p>
<p>We appreciate you and look forward to your feedback.</p><br/>
        <p>We look forward to working with you further!</p>
        <p>The MyDMV.Pro Team</p>
</div>";
        public static string FormatVendorInviteBody(string html, string siteUrl, Guid inviteId, string VendorName)
        {
            const string docsFolder = "/files/docs/"; //myDMV.pro Login Procedure.pdf
            string quickRefGuidePdfUrl = $"{siteUrl}{docsFolder}MyDMV.Pro Quick Reference Guide.pdf";
            string userInstructionsPdfUrl = $"{siteUrl}{docsFolder}myDMV.Pro User Instructions.pdf";
            string loginInstructionsPdfUrl = $"{siteUrl}{docsFolder}myDMV.Pro Login Procedure.pdf";
            string registerUrl = $"{siteUrl}/Vendors/Register/{inviteId}";
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(html,
                HttpUtility.HtmlEncode(VendorName),
                registerUrl,
                loginInstructionsPdfUrl,
                quickRefGuidePdfUrl
            );
            return sb.ToString();
        }
        private const string DefaultGroupInviteBodyFormat = @"<div style=""font-family:Calibri;font-size:12pt;max-width:622"">
<p>Hello,</p>
<p/>
<p>You have been invited to join the “{0}” group at MyDMV.Pro.</p>
<p style=""margin-bottom:0px"">To accept the invitation and create your login credentials, please click the link below.<p>
<p style=""text-align:center""><a style=""background-color:darkblue;color:white;"" href=""{1}"">Click Here to Accept Your Invitation to MyDMV.Pro!</a></p>

<p style=""margin-bottom:0px"">For your convenience, we have also included instructions to complete your account set up and for general use of the system. Please find links to the PDFs below:</p>
<p style=""text-align:center""><a style=""background-color:silver;color:black;"" href=""{2}""><b>Click Here for Instructions to Complete Your Sign-Up Process</b></a></p>
<p style=""text-align:center""><a style=""background-color:silver;color:black;"" href=""{3}""><b>Click Here to Access the MyDMV.Pro User Guide</b></a></p>

<p>For more information, please email <a href=""mailto:info@maggard.net"">info@maggard.net</a>.</p>
<p>We look forward to working with you further!<br/>
The Maggard Team</p></div>";

        public static string FormatGroupInviteBody(string html, string siteUrl, Guid inviteId, string GroupName)
        {
            const string docsFolder = "/files/docs/"; //myDMV.pro Login Procedure.pdf
            string userInstructionsPdfUrl = $"{siteUrl}{docsFolder}myDMV.pro User Instructions.pdf";
            string quickRefGuidePdfUrl = $"{siteUrl}{docsFolder}MyDMV.Pro Quick Reference Guide.pdf";
            string loginInstructionsPdfUrl = $"{siteUrl}{docsFolder}myDMV.Pro Login Procedure.pdf";
            string registerUrl = $"{siteUrl}/Groups/Register/{inviteId}";
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(html, 
                HttpUtility.HtmlEncode(GroupName),
                registerUrl,
                loginInstructionsPdfUrl,
                quickRefGuidePdfUrl
            );
            return sb.ToString();
        }

        public static SmtpClient GetClient()
        {
            SmtpClient client = new SmtpClient();
            client.EnableSsl = EnableSsl;
            client.Host = SmtpServer;
            client.Port = SmtpPort;
            if (!string.IsNullOrEmpty(SmtpUser))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(SmtpUser, SmtpPassword);
            }

            return client;
        }
        public static bool SendLHChatNotification(string siteUrl, string from, string to)
        {
            try
            {
                using (SmtpClient client = GetClient())
                {
                    MailMessage message = new MailMessage();
                    message.From = new MailAddress(from ?? SmtpFrom, FromDisplayName);
                    message.To.Add(new MailAddress(to));
                    if (!string.IsNullOrEmpty(BccAddress))
                        message.Bcc.Add(new MailAddress(BccAddress));
                    message.Subject = c_DefaultInviteSubject;
                    message.IsBodyHtml = true;
                    message.Body = FormatChatBody();

                    client.Send(message);

                    return true;
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Logger.LogError(ex, "Error sending LH Chat notification");
                return false;
            }
        }
        public static string FormatChatBody()
        {
            StringBuilder sb = new StringBuilder();
            return sb.ToString();
        }
    }
}
