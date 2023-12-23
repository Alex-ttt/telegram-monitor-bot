﻿namespace TelegramMonitorBot.Domain.Models;

public class User
{
    public long UserId { get; init; }
    public string Name { get; init; }

    public User(long userId, string name)
    {
        UserId = userId;
        Name = name;
    }
}
