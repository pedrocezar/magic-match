using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class SendMessageResponse
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    
    public string? From { get; set; }
    
    public string? To { get; set; }
    
    [JsonPropertyName("match_id")]
    public string? MatchId { get; set; }
    
    [JsonPropertyName("sent_date")]
    public DateTime? SentDate { get; set; }
    
    public string? Message { get; set; }
    
    public MessageMedia? Media { get; set; }
    
    [JsonPropertyName("created_date")]
    public DateTime? CreatedDate { get; set; }
}

public class MessageMedia
{
    public int? Width { get; set; }
    
    public int? Height { get; set; }
}
