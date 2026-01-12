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
}
