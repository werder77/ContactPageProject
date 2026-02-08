using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ContactFunctionApp
{
    public static class SendContactEmail
    {
        [FunctionName("SendContactEmail")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string name = data?.name;
            string email = data?.email;
            string subject = data?.subject;
            string message = data?.message;

            if (string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(subject) ||
                string.IsNullOrEmpty(message))
            {
                return new BadRequestObjectResult("Missing fields.");
            }

            if (Regex.IsMatch(message, "<.*?>"))
            {
                return new BadRequestObjectResult("HTML tags are not allowed.");
            }

            string connectionString = System.Environment.GetEnvironmentVariable("ACS_CONNECTION_STRING");
            string recipient = System.Environment.GetEnvironmentVariable("RECIPIENT_EMAIL");
            string sender = System.Environment.GetEnvironmentVariable("SENDER_EMAIL");

            var emailClient = new EmailClient(connectionString);

            var emailContent = new EmailContent(subject)
            {
                PlainText = $"Name: {name}\nEmail: {email}\n\nMessage:\n{message}"
            };

            var emailMessage = new EmailMessage(sender, recipient, emailContent);

            await emailClient.SendAsync(Azure.WaitUntil.Completed, emailMessage);

            return new OkObjectResult("Message sent successfully.");
        }
    }
}