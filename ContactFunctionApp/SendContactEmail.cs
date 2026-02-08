using System.Net;
using System.Text.RegularExpressions;
using Azure;
using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ContactFunctionApp
{
    public class SendContactEmail
    {
        private readonly ILogger _logger;

        // Constructor injection for the logger
        public SendContactEmail(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SendContactEmail>();
        }

        [Function("SendContactEmail")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Processing contact form submission.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string name = data?.name;
            string email = data?.email;
            string subject = data?.subject;
            string message = data?.message;

            // 1. Validation
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
            {
                return await CreateResponse(req, HttpStatusCode.BadRequest, "Missing fields.");
            }

            // 2. Security Check
            if (Regex.IsMatch(message, "<.*?>"))
            {
                return await CreateResponse(req, HttpStatusCode.BadRequest, "HTML tags are not allowed.");
            }

            try
            {
                // 3. Environment Variables
                string connectionString = Environment.GetEnvironmentVariable("ACS_CONNECTION_STRING");
                string recipient = Environment.GetEnvironmentVariable("RECIPIENT_EMAIL");
                string sender = Environment.GetEnvironmentVariable("SENDER_EMAIL");

                // 4. Send via Azure Communication Services
                var emailClient = new EmailClient(connectionString);
                var emailContent = new EmailContent(subject)
                {
                    PlainText = $"Name: {name}\nEmail: {email}\n\nMessage:\n{message}"
                };

                var emailMessage = new EmailMessage(sender, recipient, emailContent);
                
                await emailClient.SendAsync(WaitUntil.Completed, emailMessage);

                return await CreateResponse(req, HttpStatusCode.OK, "Message sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Email failed: {ex.Message}");
                return await CreateResponse(req, HttpStatusCode.InternalServerError, "Error sending email.");
            }
        }

        // Helper method to create responses in Isolated Model
        private async Task<HttpResponseData> CreateResponse(HttpRequestData req, HttpStatusCode code, string message)
        {
            var response = req.CreateResponse(code);
            await response.WriteStringAsync(message);
            return response;
        }
    }
}