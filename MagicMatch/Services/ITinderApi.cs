using MagicMatch.Models;
using Refit;

namespace MagicMatch.Services;

public interface ITinderApi
{
    [Get("/v2/recs/core")]
    Task<RecsCoreResponse> GetRecsCoreAsync(
        [Query("locale")] string locale = "pt",
        [Query("duos")] int duos = 0,
        CancellationToken cancellationToken = default);
    
    [Post("/like/{userId}")]
    Task<LikeResponse> LikeAsync(
        [AliasAs("userId")] string userId,
        [Body] LikeRequest request,
        [Query("locale")] string locale = "pt",
        CancellationToken cancellationToken = default);
    
    [Post("/pass/{userId}")]
    Task<PassResponse> PassAsync(
        [AliasAs("userId")] string userId,
        [Query("s_number")] long sNumber,
        [Query("locale")] string locale = "pt",
        CancellationToken cancellationToken = default);
    
    [Get("/v2/matches")]
    Task<MatchesResponse> GetMatchesAsync(
        [Query("locale")] string locale = "pt",
        [Query("count")] int count = 60,
        [Query("message")] int message = 0,
        [Query("is_tinder_u")] bool isTinderU = false,
        [Query("include_conversations")] bool includeConversations = true,
        CancellationToken cancellationToken = default);
    
    [Post("/user/matches/{matchId}")]
    Task<SendMessageResponse> SendMessageAsync(
        [AliasAs("matchId")] string matchId,
        [Body] SendMessageRequest request,
        [Query("locale")] string locale = "pt",
        CancellationToken cancellationToken = default);
}
