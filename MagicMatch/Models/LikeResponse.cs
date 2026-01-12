using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class LikeResponse
{
    public int Status { get; set; }
}

public class Match
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    
    public bool Closed { get; set; }
    
    [JsonPropertyName("common_friend_count")]
    public int CommonFriendCount { get; set; }
    
    [JsonPropertyName("common_like_count")]
    public int CommonLikeCount { get; set; }
    
    [JsonPropertyName("created_date")]
    public DateTime? CreatedDate { get; set; }
    
    public bool Following { get; set; }
    
    [JsonPropertyName("following_moments")]
    public bool FollowingMoments { get; set; }
    
    [JsonPropertyName("last_activity_date")]
    public DateTime? LastActivityDate { get; set; }
    
    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }
    
    public List<object>? Messages { get; set; }
    
    public bool Muted { get; set; }
    
    public List<string>? Participants { get; set; }
    
    public bool Pending { get; set; }
    
    [JsonPropertyName("is_super_like")]
    public bool IsSuperLike { get; set; }
    
    [JsonPropertyName("is_boost_match")]
    public bool IsBoostMatch { get; set; }
    
    [JsonPropertyName("is_super_boost_match")]
    public bool IsSuperBoostMatch { get; set; }
    
    [JsonPropertyName("is_primetime_boost_match")]
    public bool IsPrimetimeBoostMatch { get; set; }
    
    [JsonPropertyName("is_preferences_match")]
    public bool IsPreferencesMatch { get; set; }
    
    [JsonPropertyName("is_fast_match")]
    public bool IsFastMatch { get; set; }
    
    [JsonPropertyName("is_top_pick")]
    public bool IsTopPick { get; set; }
    
    [JsonPropertyName("liked_content")]
    public LikedContent? LikedContent { get; set; }
    
    [JsonPropertyName("subscription_tier")]
    public string? SubscriptionTier { get; set; }
    
    [JsonPropertyName("message_suggestions")]
    public MessageSuggestions? MessageSuggestions { get; set; }
    
    [JsonPropertyName("is_lets_meet_match")]
    public bool IsLetsMeetMatch { get; set; }
}

public class LikedContent
{
    [JsonPropertyName("by_closer")]
    public ByCloser? ByCloser { get; set; }
}

public class ByCloser
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
    
    public string? Type { get; set; }
}

public class MessageSuggestions
{
    public List<MessageSuggestion>? Emojis { get; set; }
    
    public List<MessageSuggestion>? Greetings { get; set; }
}

public class MessageSuggestion
{
    [JsonPropertyName("message_suggestion_id")]
    public string? MessageSuggestionId { get; set; }
    
    public string? Message { get; set; }
}
