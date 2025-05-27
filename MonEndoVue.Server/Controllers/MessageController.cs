using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Mvc;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class MessageController(FirebaseMessaging? messaging) : ControllerBase
{
    private readonly FirebaseMessaging _messaging = messaging ?? FirebaseMessaging.DefaultInstance;

    [HttpPost("SendNotification")]
    public async Task<IActionResult> SendMessageAsync([FromBody] MessageRequest request)
    {
        try
        {
            var message = new Message
            {
                Token = request.DeviceToken,
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Body
                },
                Webpush = new WebpushConfig
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "Urgency", "high" }
                    },
                    FcmOptions = new WebpushFcmOptions
                    {
                        Link = "https://monendo-app.fr" // Optionnel : URL à ouvrir quand l'utilisateur clique sur la notification
                    },
                    Notification = new WebpushNotification
                    {
                        Title = request.Title,
                        Body = request.Body,
                        RequireInteraction = true,
                        Icon = "https://monendoapp.fr/assets/MonEndoTest-Cqe9G23B.png",
                        Image = "https://monendoapp.fr/assets/MonEndoTest-Cqe9G23B.png",
                    }
                },
            };

            var response = await _messaging.SendAsync(message);

            if (!string.IsNullOrEmpty(response))
            {
                return Ok(new { success = true, message = "Message sent successfully!", messageId = response });
            }
            
            return BadRequest(new { success = false, message = "Failed to send message" });
        }
        catch (FirebaseException ex)
        {
            return StatusCode(500, new { 
                success = false, 
                message = "Error sending FCM message", 
                error = ex.Message,
                errorCode = ex.ErrorCode
            });
        }
        catch (Exception ex)
        {
            // Log l'erreur ici
            return StatusCode(500, new { 
                success = false, 
                message = "Internal server error", 
                error = ex.Message 
            });
        }
    }
}