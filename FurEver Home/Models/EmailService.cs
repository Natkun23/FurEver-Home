using System;
using System.Net;
using System.Net.Mail;
using System.Configuration;

namespace FurEver_Home.Services
{
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService()
        {
            // Read from Web.config AppSettings
            _smtpHost = ConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"] ?? "587");
            _smtpUsername = ConfigurationManager.AppSettings["SmtpUsername"];
            _smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
            _fromEmail = ConfigurationManager.AppSettings["FromEmail"];
            _fromName = ConfigurationManager.AppSettings["FromName"] ?? "FurEver Home";
        }

        public bool SendEmail(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.EnableSsl = true;
                    smtpClient.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_fromEmail, _fromName);
                        message.To.Add(toEmail);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = isHtml;
                        message.Priority = MailPriority.High;

                        smtpClient.Send(message);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log the error (you can use log4net, NLog, etc.)
                System.Diagnostics.Debug.WriteLine($"Email Error: {ex.Message}");
                return false;
            }
        }

        public bool SendOtpEmail(string toEmail, string userName, string otpCode)
        {
            string subject = "Your FurEver Home Login OTP Code";
            string body = GetOtpEmailTemplate(userName, otpCode);
            return SendEmail(toEmail, subject, body);
        }

        public bool SendPasswordResetEmail(string toEmail, string userName, string resetUrl)
        {
            string subject = "Reset Your FurEver Home Password";
            string body = GetPasswordResetEmailTemplate(userName, resetUrl);
            return SendEmail(toEmail, subject, body);
        }

        private string GetOtpEmailTemplate(string userName, string otpCode)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f5f7fa; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #3FA9F5 0%, #2E8BC0 100%); padding: 40px 20px; text-align: center; color: white; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 40px 30px; }}
        .otp-box {{ background: #f8fafc; border: 3px dashed #3FA9F5; border-radius: 12px; padding: 30px; text-align: center; margin: 30px 0; }}
        .otp-code {{ font-size: 42px; font-weight: 700; color: #2E8BC0; letter-spacing: 8px; margin: 10px 0; }}
        .warning {{ background: #fef3c7; border-left: 4px solid #F6C90E; padding: 15px; border-radius: 8px; margin: 20px 0; color: #92400e; }}
        .footer {{ background: #f8fafc; padding: 20px; text-align: center; color: #64748b; font-size: 14px; }}
        .btn {{ display: inline-block; padding: 14px 30px; background: linear-gradient(135deg, #3FA9F5 0%, #2E8BC0 100%); color: white; text-decoration: none; border-radius: 8px; font-weight: 600; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🐾 FurEver Home</h1>
            <p>Secure Login Verification</p>
        </div>
        <div class='content'>
            <h2 style='color: #2E8BC0; margin-top: 0;'>Hello {userName}!</h2>
            <p style='color: #64748b; font-size: 16px; line-height: 1.6;'>
                We received a login request for your FurEver Home account. Please use the OTP code below to complete your login:
            </p>
            
            <div class='otp-box'>
                <p style='color: #64748b; margin: 0; font-size: 14px;'>Your One-Time Password</p>
                <div class='otp-code'>{otpCode}</div>
                <p style='color: #64748b; margin: 0; font-size: 13px;'>Valid for 5 minutes</p>
            </div>

            <div class='warning'>
                <strong>⚠️ Security Notice:</strong><br>
                Never share this code with anyone. Our team will never ask for your OTP code.
            </div>

            <p style='color: #64748b; font-size: 14px; line-height: 1.6;'>
                If you didn't request this code, please ignore this email and ensure your account is secure.
            </p>
        </div>
        <div class='footer'>
            <p style='margin: 0;'>© 2024 FurEver Home - Pet Adoption Platform</p>
            <p style='margin: 5px 0 0 0;'>Helping pets find their forever homes 🏠</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetPasswordResetEmailTemplate(string userName, string resetUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f5f7fa; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #FFD93D 0%, #F6C90E 100%); padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; color: #2E8BC0; }}
        .content {{ padding: 40px 30px; }}
        .btn-reset {{ display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #3FA9F5 0%, #2E8BC0 100%); color: white; text-decoration: none; border-radius: 10px; font-weight: 700; font-size: 16px; margin: 30px 0; box-shadow: 0 4px 12px rgba(63, 169, 245, 0.3); }}
        .btn-reset:hover {{ box-shadow: 0 6px 20px rgba(63, 169, 245, 0.4); }}
        .warning {{ background: #fef2f2; border-left: 4px solid #ef4444; padding: 15px; border-radius: 8px; margin: 20px 0; color: #991b1b; }}
        .footer {{ background: #f8fafc; padding: 20px; text-align: center; color: #64748b; font-size: 14px; }}
        .expiry-note {{ background: #eff6ff; padding: 12px; border-radius: 8px; color: #1e40af; font-size: 14px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Password Reset Request</h1>
            <p style='color: #64748b; margin: 5px 0 0 0;'>FurEver Home</p>
        </div>
        <div class='content'>
            <h2 style='color: #2E8BC0; margin-top: 0;'>Hello {userName}!</h2>
            <p style='color: #64748b; font-size: 16px; line-height: 1.6;'>
                We received a request to reset your password for your FurEver Home account. 
                Click the button below to create a new password:
            </p>
            
            <div style='text-align: center;'>
                <a href='{resetUrl}' class='btn-reset'>Reset My Password</a>
            </div>

            <div class='expiry-note'>
                ⏰ <strong>Important:</strong> This password reset link will expire in 1 hour for security reasons.
            </div>

            <p style='color: #64748b; font-size: 14px; line-height: 1.6;'>
                If the button doesn't work, you can copy and paste this link into your browser:
            </p>
            <p style='background: #f8fafc; padding: 12px; border-radius: 8px; word-break: break-all; font-size: 13px; color: #3FA9F5;'>
                {resetUrl}
            </p>

            <div class='warning'>
                <strong>⚠️ Didn't request this?</strong><br>
                If you didn't request a password reset, please ignore this email. Your password will remain unchanged.
            </div>
        </div>
        <div class='footer'>
            <p style='margin: 0;'>© 2024 FurEver Home - Pet Adoption Platform</p>
            <p style='margin: 5px 0 0 0;'>Helping pets find their forever homes 🏠</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}