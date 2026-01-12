using MagicMatch.Services;
using MagicMatch.Models;

var config = new TinderApiConfig
{
    AuthToken = Environment.GetEnvironmentVariable("TINDER_AUTH_TOKEN"),
    AppSessionId = Environment.GetEnvironmentVariable("TINDER_APP_SESSION_ID"),
    AppSessionTimeElapsed = int.TryParse(Environment.GetEnvironmentVariable("TINDER_APP_SESSION_TIME_ELAPSED"), out var appSessionTime) ? appSessionTime : null,
    UserSessionId = Environment.GetEnvironmentVariable("TINDER_USER_SESSION_ID"),
    UserSessionTimeElapsed = int.TryParse(Environment.GetEnvironmentVariable("TINDER_USER_SESSION_TIME_ELAPSED"), out var userSessionTime) ? userSessionTime : null,
    PersistentDeviceId = Environment.GetEnvironmentVariable("TINDER_PERSISTENT_DEVICE_ID")
};

var message = Environment.GetEnvironmentVariable("TINDER_MESSAGE") ?? "Hi!";

var minAge = int.TryParse(Environment.GetEnvironmentVariable("TINDER_MIN_AGE"), out var minAgeValue) ? minAgeValue : 20;
var maxAge = int.TryParse(Environment.GetEnvironmentVariable("TINDER_MAX_AGE"), out var maxAgeValue) ? maxAgeValue : 50;
var maxDistanceKm = double.TryParse(Environment.GetEnvironmentVariable("TINDER_MAX_DISTANCE_KM"), out var maxDistanceValue) ? maxDistanceValue : 15.0;
var minPhotos = int.TryParse(Environment.GetEnvironmentVariable("TINDER_MIN_PHOTOS"), out var minPhotosValue) ? minPhotosValue : 6;
var maxErrors = int.TryParse(Environment.GetEnvironmentVariable("TINDER_MAX_ERRORS"), out var maxErrorsValue) ? maxErrorsValue : 3;
var bioExcludeKeywords = (Environment.GetEnvironmentVariable("TINDER_BIO_EXCLUDE_KEYWORDS") ?? "")
    .Split(';', StringSplitOptions.RemoveEmptyEntries)
    .Select(k => k.Trim().ToLowerInvariant())
    .Where(k => !string.IsNullOrWhiteSpace(k))
    .ToArray();

var delayRecsMinMs = int.TryParse(Environment.GetEnvironmentVariable("TINDER_DELAY_RECS_MIN_MS"), out var delayRecsMin) ? delayRecsMin : 10000;
var delayRecsMaxMs = int.TryParse(Environment.GetEnvironmentVariable("TINDER_DELAY_RECS_MAX_MS"), out var delayRecsMax) ? delayRecsMax : 30000;
var delayMessagesMinMs = int.TryParse(Environment.GetEnvironmentVariable("TINDER_DELAY_MESSAGES_MIN_MS"), out var delayMessagesMin) ? delayMessagesMin : 3000;
var delayMessagesMaxMs = int.TryParse(Environment.GetEnvironmentVariable("TINDER_DELAY_MESSAGES_MAX_MS"), out var delayMessagesMax) ? delayMessagesMax : 7000;
var delayMatchesMinMs = int.TryParse(Environment.GetEnvironmentVariable("TINDER_DELAY_MATCHES_MIN_MS"), out var delayMatchesMin) ? delayMatchesMin : 8000;
var delayMatchesMaxMs = int.TryParse(Environment.GetEnvironmentVariable("TINDER_DELAY_MATCHES_MAX_MS"), out var delayMatchesMax) ? delayMatchesMax : 12000;
var delayBetweenExecutionsMinMinutes = int.TryParse(Environment.GetEnvironmentVariable("TINDER_DELAY_BETWEEN_EXECUTIONS_MIN_MINUTES"), out var delayExecMin) ? delayExecMin : 30;
var delayBetweenExecutionsMaxMinutes = int.TryParse(Environment.GetEnvironmentVariable("TINDER_DELAY_BETWEEN_EXECUTIONS_MAX_MINUTES"), out var delayExecMax) ? delayExecMax : 120;

var service = new TinderApiService(config);

var recsTask = Task.Run(async () =>
{
    int consecutiveErrors = 0;

    while (consecutiveErrors < maxErrors)
    {
        try
        {
            Console.WriteLine("[RECS] Starting recommendations processing...");
            var response = await service.GetRecsCoreAsync(locale: "pt", duos: 0);

            if (response.Meta?.Status != 200)
            {
                throw new Exception($"HTTP Status code: {response.Meta?.Status ?? 0}");
            }

            if (response.Data?.Results != null)
            {
                Console.WriteLine($"\n[RECS] Found {response.Data.Results.Count} results:\n");
                
                int likesCount = 0;
                int passesCount = 0;

                foreach (var result in response.Data.Results)
                {
                    if (result.User != null)
                    {
                        var age = CalculateAge(result.User.BirthDate);
                        var distanceKm = ConvertMiToKm(result.DistanceMi);
                        var photoCount = result.User.Photos?.Count ?? 0;
                        
                        Console.WriteLine($"Name: {result.User.Name}");
                        Console.WriteLine($"Age: {age?.ToString() ?? "N/A"} years old");
                        Console.WriteLine($"Distance: {distanceKm:F2} km ({result.DistanceMi} miles)");
                        Console.WriteLine($"Photos: {photoCount}");
                        
                        var bio = result.User.Bio ?? "";
                        var bioLower = bio.ToLowerInvariant();
                        var hasExcludedKeyword = bioExcludeKeywords.Length > 0 && bioExcludeKeywords.Any(keyword => bioLower.Contains(keyword));
                        
                        if (hasExcludedKeyword)
                        {
                            Console.WriteLine($"X Bio contains excluded keyword. Skipping...");
                        }
                        
                        bool meetsCriteria = !hasExcludedKeyword &&
                                            age.HasValue && 
                                            age >= minAge && age <= maxAge && 
                                            distanceKm <= maxDistanceKm && 
                                            photoCount >= minPhotos;

                        if (meetsCriteria)
                        {
                            var randomPhoto = result.User.Photos?
                                .Where(p => !string.IsNullOrEmpty(p.Id))
                                .OrderBy(x => Guid.NewGuid())
                                .FirstOrDefault();
                            
                            if (randomPhoto != null && !string.IsNullOrEmpty(result.User.Id))
                            {
                                try
                                {
                                    Console.WriteLine($"V Meets criteria! Sending like...");

                                    var likeResponse = await service.LikeAsync(
                                        result.User.Id,
                                        result.SNumber,
                                        randomPhoto.Id);

                                    if (likeResponse.Status == 200)
                                    {
                                        likesCount++;
                                        Console.WriteLine($"Like sent!");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Error sending like: Status {likeResponse.Status}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error sending like: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(result.User.Id))
                            {
                                try
                                {
                                    Console.WriteLine($"X Does not meet criteria. Sending pass...");
                                    var passResponse = await service.PassAsync(
                                        result.User.Id, 
                                        result.SNumber);
                                    
                                    if (passResponse.Status == 200)
                                    {
                                        passesCount++;
                                        Console.WriteLine($"Pass sent!");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Error sending pass: Status {passResponse.Status}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error sending pass: {ex.Message}");
                                }
                            }
                        }
                        
                        Console.WriteLine(new string('-', 50));
                        
                        await RandomDelay(delayRecsMinMs, delayRecsMaxMs);
                    }
                }
                
                Console.WriteLine($"\n[RECS] === Summary ===");
                Console.WriteLine($"[RECS] Total likes sent: {likesCount}");
                Console.WriteLine($"[RECS] Total passes sent: {passesCount}");
                
                consecutiveErrors = 0;
            }
            else
            {
                Console.WriteLine("[RECS] No results found.");
                consecutiveErrors = 0;
            }
        }
        catch (Exception ex)
        {
            consecutiveErrors++;
            Console.WriteLine($"[RECS] Error (Attempt {consecutiveErrors}/{maxErrors}): {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[RECS] Details: {ex.InnerException.Message}");
            }
            
            if (consecutiveErrors >= maxErrors)
            {
                Console.WriteLine($"[RECS] Max errors reached ({maxErrors}). Stopping recommendations task.");
                return;
            }
        }

        if (consecutiveErrors < maxErrors)
        {
            var waitMinutes = RandomDelayMinutes(delayBetweenExecutionsMinMinutes, delayBetweenExecutionsMaxMinutes);
            Console.WriteLine($"[RECS] Waiting {waitMinutes} minutes before next execution...");
            await Task.Delay(waitMinutes * 60 * 1000);
        }
    }
});

var matchesTask = Task.Run(async () =>
{
    int consecutiveErrors = 0;

    while (consecutiveErrors < maxErrors)
    {
        try
        {
            Console.WriteLine("[MATCHES] Starting matches processing...");
            var matchesResponse = await service.GetMatchesAsync(locale: "pt", count: 60, message: 0, isTinderU: false, includeConversations: true);

            if (matchesResponse.Meta?.Status != 200)
            {
                throw new Exception($"HTTP Status code: {matchesResponse.Meta?.Status ?? 0}");
            }

            if (matchesResponse.Data?.Matches != null)
            {
                Console.WriteLine($"\n[MATCHES] Found {matchesResponse.Data.Matches.Count} matches:\n");
                
                int messagesSent = 0;

                foreach (var match in matchesResponse.Data.Matches)
                {
                    if (match.Person != null && 
                        !string.IsNullOrEmpty(match.Id) && 
                        !string.IsNullOrEmpty(match.Person.Id) &&
                        match.LikedContent?.ByCloser != null &&
                        !string.IsNullOrEmpty(match.LikedContent.ByCloser.UserId) &&
                        match.MessageCount == 0)
                    {
                        try
                        {
                            Console.WriteLine($"Match: {match.Person.Name} (ID: {match.Id})");
                            
                            var personalizedMessage = message.Replace("{{NAME}}", match.Person.Name ?? "");
                            
                            var messageParts = personalizedMessage.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(part => part.Trim())
                                .Where(part => !string.IsNullOrWhiteSpace(part))
                                .ToList();

                            if (messageParts.Count == 0)
                            {
                                messageParts.Add(personalizedMessage.Trim());
                            }

                            Console.WriteLine($"Sending {messageParts.Count} message(s)");
                            
                            foreach (var messagePart in messageParts)
                            {
                                Console.WriteLine($"Sending message: {messagePart}");

                                var messageResponse = await service.SendMessageAsync(
                                    match.Id,
                                    match.LikedContent.ByCloser.UserId,
                                    match.Person.Id,
                                    messagePart);

                                if (!string.IsNullOrEmpty(messageResponse.Id))
                                {
                                    messagesSent++;
                                    Console.WriteLine($"Message sent! Message ID: {messageResponse.Id}");
                                }
                                else
                                {
                                    Console.WriteLine($"Error sending message");
                                }
                                
                                await RandomDelay(delayMessagesMinMs, delayMessagesMaxMs);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending message to {match.Person.Name}: {ex.Message}");
                        }
                        
                        Console.WriteLine(new string('-', 50));
                        
                        await RandomDelay(delayMatchesMinMs, delayMatchesMaxMs);
                    }
                }
                
                Console.WriteLine($"\n[MATCHES] === Summary ===");
                Console.WriteLine($"[MATCHES] Total messages sent: {messagesSent}");
                
                consecutiveErrors = 0;
            }
            else
            {
                Console.WriteLine($"[MATCHES] Status: {matchesResponse.Meta?.Status}");
                Console.WriteLine("[MATCHES] No matches found.");
                consecutiveErrors = 0;
            }
        }
        catch (Exception ex)
        {
            consecutiveErrors++;
            Console.WriteLine($"[MATCHES] Error (Attempt {consecutiveErrors}/{maxErrors}): {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[MATCHES] Details: {ex.InnerException.Message}");
            }
            
            if (consecutiveErrors >= maxErrors)
            {
                Console.WriteLine($"[MATCHES] Max errors reached ({maxErrors}). Stopping matches task.");
                return;
            }
        }

        if (consecutiveErrors < maxErrors)
        {
            var waitMinutes = RandomDelayMinutes(delayBetweenExecutionsMinMinutes, delayBetweenExecutionsMaxMinutes);
            Console.WriteLine($"[MATCHES] Waiting {waitMinutes} minutes before next execution...");
            await Task.Delay(waitMinutes * 60 * 1000);
        }
    }
});

await Task.WhenAll(recsTask, matchesTask);

static async Task RandomDelay(int minMilliseconds, int maxMilliseconds)
{
    var random = new Random();
    var delay = random.Next(minMilliseconds, maxMilliseconds + 1);
    await Task.Delay(delay);
}

static int RandomDelayMinutes(int minMinutes, int maxMinutes)
{
    var random = new Random();
    return random.Next(minMinutes, maxMinutes + 1);
}

static int? CalculateAge(DateTime? birthDate)
{
    if (birthDate == null) return null;
    var today = DateTime.Today;
    var age = today.Year - birthDate.Value.Year;
    if (birthDate.Value.Date > today.AddYears(-age)) age--;
    return age;
}

static double ConvertMiToKm(int miles)
{
    return miles * 1.60934;
}
