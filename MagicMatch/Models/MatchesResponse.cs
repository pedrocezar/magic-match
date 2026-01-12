using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class MatchesResponse
{
    public Meta? Meta { get; set; }
    public MatchesData? Data { get; set; }
}

public class MatchesData
{
    public List<MatchItem>? Matches { get; set; }
}

public class MatchItem
{
    public Seen? Seen { get; set; }
    
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    
    public bool Closed { get; set; }
    
    [JsonPropertyName("common_friend_count")]
    public int CommonFriendCount { get; set; }
    
    [JsonPropertyName("common_like_count")]
    public int CommonLikeCount { get; set; }
    
    [JsonPropertyName("created_date")]
    public DateTime? CreatedDate { get; set; }
    
    public bool Dead { get; set; }
    
    [JsonPropertyName("last_activity_date")]
    public DateTime? LastActivityDate { get; set; }
    
    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }
    
    public List<object>? Messages { get; set; }
    
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
    
    [JsonPropertyName("is_experiences_match")]
    public bool IsExperiencesMatch { get; set; }
    
    [JsonPropertyName("is_fast_match")]
    public bool IsFastMatch { get; set; }
    
    [JsonPropertyName("is_preferences_match")]
    public bool IsPreferencesMatch { get; set; }
    
    [JsonPropertyName("is_matchmaker_match")]
    public bool IsMatchmakerMatch { get; set; }
    
    [JsonPropertyName("is_lets_meet_match")]
    public bool IsLetsMeetMatch { get; set; }
    
    [JsonPropertyName("is_opener")]
    public bool IsOpener { get; set; }
    
    [JsonPropertyName("has_shown_initial_interest")]
    public bool HasShownInitialInterest { get; set; }
    
    public Person? Person { get; set; }
    
    public bool Following { get; set; }
    
    [JsonPropertyName("following_moments")]
    public bool FollowingMoments { get; set; }
    
    public ReadReceipt? ReadReceipt { get; set; }
    
    [JsonPropertyName("liked_content")]
    public MatchLikedContent? LikedContent { get; set; }
    
    [JsonPropertyName("subscription_tier")]
    public string? SubscriptionTier { get; set; }
}

public class Seen
{
    [JsonPropertyName("match_seen")]
    public bool MatchSeen { get; set; }
}

public class Person
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    
    public string? Bio { get; set; }
    
    [JsonPropertyName("birth_date")]
    public DateTime? BirthDate { get; set; }
    
    public int Gender { get; set; }
    
    public string? Name { get; set; }
    
    [JsonPropertyName("ping_time")]
    public DateTime? PingTime { get; set; }
    
    public List<PersonPhoto>? Photos { get; set; }
    
    [JsonPropertyName("hide_distance")]
    public bool HideDistance { get; set; }
    
    [JsonPropertyName("hide_age")]
    public bool HideAge { get; set; }
}

public class PersonPhoto
{
    public string? Id { get; set; }
    
    [JsonPropertyName("crop_info")]
    public CropInfo? CropInfo { get; set; }
    
    public string? Url { get; set; }
    
    [JsonPropertyName("processed_files")]
    public List<ProcessedFile>? ProcessedFiles { get; set; }
    
    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }
    
    public string? Extension { get; set; }
    
    [JsonPropertyName("webp_qf")]
    public List<int>? WebpQf { get; set; }
    
    public int Rank { get; set; }
    
    public double Score { get; set; }
    
    [JsonPropertyName("win_count")]
    public int WinCount { get; set; }
    
    public string? Type { get; set; }
    
    [JsonPropertyName("selfie_verified")]
    public bool? SelfieVerified { get; set; }
    
    public List<object>? Assets { get; set; }
    
    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }
}

public class ReadReceipt
{
    public bool Enabled { get; set; }
}

public class MatchLikedContent
{
    [JsonPropertyName("by_closer")]
    public MatchByCloser? ByCloser { get; set; }
}

public class MatchByCloser
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
    
    public string? Type { get; set; }
    
    [JsonPropertyName("is_swipe_note")]
    public bool? IsSwipeNote { get; set; }
}
