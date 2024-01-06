using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramMonitorBot.TelegramBotClient.Navigation.Models;

/// <summary>
/// Wrapper over the parameters of <see cref="TelegramBotClientExtensions.SendTextMessageAsync"/>
/// </summary>
/// <param name="ChatId"></param>
/// <param name="Text"></param>
/// <param name="ReplyMarkup"></param>
/// <param name="CancellationToken"></param>
public record MessageRequest(ChatId ChatId, string Text, IReplyMarkup? ReplyMarkup = default);
