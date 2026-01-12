using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class PassResponse
{
    public int Status { get; set; }
    
    [JsonPropertyName("X-Padding")]
    public string? XPadding { get; set; }
}
