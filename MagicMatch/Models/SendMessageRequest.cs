using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class SendMessageRequest
{
    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
    
    [JsonPropertyName("otherId")]
    public string? OtherId { get; set; }
    
    [JsonPropertyName("matchId")]
    public string? MatchId { get; set; }
    
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
