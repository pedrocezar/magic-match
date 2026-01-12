using MagicMatch.Models;
using Refit;
using System.Text.Json;

namespace MagicMatch.Services;

public class TinderApiService
{
    private readonly ITinderApi _api;

    public TinderApiService(TinderApiConfig config)
    {
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
        };

        var httpClient = new HttpClient(new TinderApiMessageHandler(config))
        {
            BaseAddress = new Uri("https://api.gotinder.com")
        };

        _api = RestService.For<ITinderApi>(httpClient, refitSettings);
    }

    public async Task<RecsCoreResponse> GetRecsCoreAsync(string? locale = "pt", int duos = 0, CancellationToken cancellationToken = default)
    {
        return await _api.GetRecsCoreAsync(locale ?? "pt", duos, cancellationToken);
    }
    
    public async Task<LikeResponse> LikeAsync(string userId, long sNumber, string? photoId, string? locale = "pt", CancellationToken cancellationToken = default)
    {
        var request = new LikeRequest
        {
            SNumber = sNumber,
            LikedContentId = photoId,
            LikedContentType = "photo"
        };
        
        return await _api.LikeAsync(userId, request, locale ?? "pt", cancellationToken);
    }
    
    public async Task<PassResponse> PassAsync(string userId, long sNumber, string? locale = "pt", CancellationToken cancellationToken = default)
    {
        return await _api.PassAsync(userId, sNumber, locale ?? "pt", cancellationToken);
    }
    
    public async Task<MatchesResponse> GetMatchesAsync(string? locale = "pt", int count = 60, int message = 0, bool isTinderU = false, bool includeConversations = true, CancellationToken cancellationToken = default)
    {
        return await _api.GetMatchesAsync(locale ?? "pt", count, message, isTinderU, includeConversations, cancellationToken);
    }
    
    public async Task<SendMessageResponse> SendMessageAsync(string matchId, string userId, string otherId, string message, string? locale = "pt", CancellationToken cancellationToken = default)
    {
        var request = new SendMessageRequest
        {
            UserId = userId,
            OtherId = otherId,
            MatchId = matchId,
            SessionId = null,
            Message = message
        };
        
        return await _api.SendMessageAsync(matchId, request, locale ?? "pt", cancellationToken);
    }
}

public class TinderApiMessageHandler : DelegatingHandler
{
    private readonly TinderApiConfig _config;

    public TinderApiMessageHandler(TinderApiConfig config)
    {
        _config = config;
        InnerHandler = new HttpClientHandler();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("accept", "application/json");
        request.Headers.Add("accept-language", "pt,pt-BR,en-US,en");
        request.Headers.Add("app-version", _config.AppVersion);
        request.Headers.Add("dnt", "1");
        request.Headers.Add("origin", "https://tinder.com");
        request.Headers.Add("platform", "web");
        request.Headers.Add("priority", "u=1, i");
        request.Headers.Add("referer", "https://tinder.com/");
        request.Headers.Add("sec-ch-ua", "\"Google Chrome\";v=\"141\", \"Not?A_Brand\";v=\"8\", \"Chromium\";v=\"141\"");
        request.Headers.Add("sec-ch-ua-mobile", "?0");
        request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
        request.Headers.Add("sec-fetch-dest", "empty");
        request.Headers.Add("sec-fetch-mode", "cors");
        request.Headers.Add("sec-fetch-site", "cross-site");
        request.Headers.Add("support-short-video", "1");
        request.Headers.Add("tinder-version", _config.TinderVersion);
        request.Headers.Add("user-agent", _config.UserAgent);
        request.Headers.Add("x-supported-image-formats", "webp,jpeg");

        if (!string.IsNullOrEmpty(_config.AuthToken))
            request.Headers.Add("x-auth-token", _config.AuthToken);
        
        if (!string.IsNullOrEmpty(_config.AppSessionId))
            request.Headers.Add("app-session-id", _config.AppSessionId);
        
        if (_config.AppSessionTimeElapsed.HasValue)
            request.Headers.Add("app-session-time-elapsed", _config.AppSessionTimeElapsed.Value.ToString());
        
        if (!string.IsNullOrEmpty(_config.UserSessionId))
            request.Headers.Add("user-session-id", _config.UserSessionId);
        
        if (_config.UserSessionTimeElapsed.HasValue)
            request.Headers.Add("user-session-time-elapsed", _config.UserSessionTimeElapsed.Value.ToString());
        
        if (!string.IsNullOrEmpty(_config.PersistentDeviceId))
            request.Headers.Add("persistent-device-id", _config.PersistentDeviceId);

        return base.SendAsync(request, cancellationToken);
    }
}

public class TinderApiConfig
{
    public string? AuthToken { get; set; }
    public string? AppSessionId { get; set; }
    public string? UserSessionId { get; set; }
    public string? PersistentDeviceId { get; set; }
    public string AppVersion { get; set; } = "1070000";
    public string TinderVersion { get; set; } = "7.0.0";
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
    public int? AppSessionTimeElapsed { get; set; }
    public int? UserSessionTimeElapsed { get; set; }
}
