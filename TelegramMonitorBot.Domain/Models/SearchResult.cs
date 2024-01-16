namespace TelegramMonitorBot.Domain.Models;

public class SearchResult : Dictionary<string, Message>
{
    private readonly Dictionary<string, List<Message>> _phrasesWithMessages;
    
    public SearchResult(string Phrase, List<Message> Messages)
    {
        
    }
    
    public string Phrase { get; init; } 
    public List<Message> Messages { get; init; } 


}
