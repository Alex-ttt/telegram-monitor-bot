using Microsoft.AspNetCore.Mvc;

namespace TelegramMonitorBot.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HomeController : ControllerBase
{
    [HttpGet("{name}")]
    public string SayHello(string name)
    {
        return $"Hello, {name}!";
    }
}