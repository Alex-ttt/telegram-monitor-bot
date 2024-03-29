﻿using Microsoft.AspNetCore.Mvc;
using TelegramMonitorBot.TelegramApiClient;

namespace TelegramMonitorBot.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HomeController : ControllerBase
{
    private readonly ITelegramApiClient _client;
    
    [HttpGet("{name}")]
    public string SayHello(string name)
    {
        return $"Hello, {name}!";
    }
}