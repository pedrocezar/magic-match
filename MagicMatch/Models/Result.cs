using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class Result
{
    public string? Type { get; set; }
    
    public User? User { get; set; }
    
    public Facebook? Facebook { get; set; }
    
    public Spotify? Spotify { get; set; }
    
    [JsonPropertyName("distance_mi")]
    public int DistanceMi { get; set; }
    
    [JsonPropertyName("content_hash")]
    public string? ContentHash { get; set; }
    
    [JsonPropertyName("s_number")]
    public long SNumber { get; set; }
    
    public Teaser? Teaser { get; set; }
    
    public List<object>? Teasers { get; set; }
}

public class Facebook
{
    [JsonPropertyName("common_connections")]
    public List<object>? CommonConnections { get; set; }
    
    [JsonPropertyName("connection_count")]
    public int ConnectionCount { get; set; }
    
    [JsonPropertyName("common_interests")]
    public List<object>? CommonInterests { get; set; }
}

public class Spotify
{
    [JsonPropertyName("spotify_connected")]
    public bool SpotifyConnected { get; set; }
    
    [JsonPropertyName("spotify_top_artists")]
    public List<object>? SpotifyTopArtists { get; set; }
}

public class Teaser
{
    public string? String { get; set; }
}
