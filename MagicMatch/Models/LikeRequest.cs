using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class LikeRequest
{
    [JsonPropertyName("s_number")]
    public long SNumber { get; set; }
    
    [JsonPropertyName("liked_content_id")]
    public string? LikedContentId { get; set; }
    
    [JsonPropertyName("liked_content_type")]
    public string? LikedContentType { get; set; }
}
