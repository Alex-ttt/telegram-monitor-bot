using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace TelegramMonitorBot.Controllers;

[ApiController] 
[Route("api/[controller]")]
public class BotController : ControllerBase
{
    [HttpPost]
    public void Post(Update update) 
    {
        Console.WriteLine(update.Message?.Text ?? "Update message has no text");
    }
    
    [HttpGet]
    public string Get() 
    {
        return "Telegram bot was started";
    }
}
