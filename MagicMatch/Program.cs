using MagicMatch.Models;
using MagicMatch.Services;

var config = new TinderApiConfig
{
    AuthToken = Environment.GetEnvironmentVariable("TINDER_AUTH_TOKEN"),
    PersistentDeviceId = Environment.GetEnvironmentVariable("TINDER_PERSISTENT_DEVICE_ID")
};

var message = Environment.GetEnvironmentVariable("TINDER_MESSAGE") ?? "😊";

var isProduction = Environment.GetEnvironmentVariable("ENVIRONMENT") == "production";
var minAge = int.TryParse(Environment.GetEnvironmentVariable("TINDER_MIN_AGE"), out var minAgeValue) ? minAgeValue : 20;
var maxAge = int.TryParse(Environment.GetEnvironmentVariable("TINDER_MAX_AGE"), out var maxAgeValue) ? maxAgeValue : 50;
var maxDistanceKm = double.TryParse(Environment.GetEnvironmentVariable("TINDER_MAX_DISTANCE_KM"), out var maxDistanceValue) ? maxDistanceValue : 15.0;
var minPhotos = int.TryParse(Environment.GetEnvironmentVariable("TINDER_MIN_PHOTOS"), out var minPhotosValue) ? minPhotosValue : 6;
var recsRequestsPerExecution = int.TryParse(Environment.GetEnvironmentVariable("TINDER_RECS_REQUESTS_PER_EXECUTION"), out var recsRequestsValue) ? recsRequestsValue : 3;
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

await ExecuteAsync();

async Task ExecuteAsync()
{
    if (isProduction)
    {
        Console.WriteLine("[INFO] Running in production mode. Executing once.");
        Console.WriteLine(new string('-', 50));
        await OnceExecuteAsync();
    }
    else
    {
        Console.WriteLine("[INFO] Running in development mode. Executing indefinitely with error handling and delays.");
        Console.WriteLine(new string('-', 50));
        await InfiniteExecuteAsync();
    }
}

async Task OnceExecuteAsync()
{
    try
    {
        await RecsAsync();

        await MatchesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERR] Error occurred: {ex.Message}");

        if (ex.InnerException != null)
        {
            Console.WriteLine($"[ERR] Details: {ex.InnerException.Message}");
        }
    }
}

async Task InfiniteExecuteAsync()
{
    var consecutiveErrors = 0;

    while (consecutiveErrors < maxErrors)
    {
        try
        {
            await RecsAsync();

            await MatchesAsync();
        }
        catch (Exception ex)
        {
            consecutiveErrors++;

            Console.WriteLine($"[ERR] Error (Attempt {consecutiveErrors}/{maxErrors}): {ex.Message}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERR] Details: {ex.InnerException.Message}");
            }

            if (consecutiveErrors >= maxErrors)
            {
                Console.WriteLine($"[ERR] Max errors reached ({maxErrors}). Stopping recommendations task.");
                return;
            }
        }

        if (consecutiveErrors < maxErrors)
        {
            var waitMinutes = RandomDelayMinutes(delayBetweenExecutionsMinMinutes, delayBetweenExecutionsMaxMinutes);
            Console.WriteLine(new string('-', 50));
            Console.WriteLine($"\n[INFO] Waiting {waitMinutes} minutes before next execution...");
            await Task.Delay(waitMinutes * 60 * 1000);
        }
    }
}

async Task RecsAsync()
{
    try
    {
        Console.WriteLine("\n[RECS] Starting recommendations processing...");

        var recs = new List<Result>();
        var recsItemCount = 0;

        foreach (var i in Enumerable.Range(1, recsRequestsPerExecution))
        {
            Console.WriteLine($"\n[RECS] Requesting recommendations (Request {i}/{recsRequestsPerExecution})...");
            var response = await service.GetRecsCoreAsync(locale: "pt", duos: 0);

            if (response.Meta?.Status != 200)
            {
                throw new InvalidOperationException($"HTTP Status code Requesting recommendations - (Request {i}/{recsRequestsPerExecution}): {response.Meta?.Status ?? 0}");
            }

            if (response.Data?.Results != null)
            {
                recsItemCount += response.Data.Results.Count;
                recs.AddRange(response.Data.Results.Where(r => !string.IsNullOrEmpty(r.User?.Id) && 
                    !recs.Any(x => !string.IsNullOrEmpty(x.User?.Id) && x.User.Id == r.User.Id)));
                Console.WriteLine($"[RECS] Received {response.Data.Results.Count}/{recsItemCount} recommendations of {recs.Count} (Request {i}/{recsRequestsPerExecution}).");
            }
            else
            {
                Console.WriteLine($"[RECS] No results found in response (Request {i}/{recsRequestsPerExecution}). Status: {response.Meta?.Status}");
            }

            await RandomDelayAsync(delayRecsMinMs, delayRecsMaxMs);
        }

        if (recs.Any())
        {
            Console.WriteLine($"\n[RECS] Found {recs.Count} results:\n");

            var likesCount = 0;
            var passesCount = 0;

            foreach (var rec in recs)
            {
                if (rec.User != null)
                {
                    var age = CalculateAge(rec.User.BirthDate);
                    var distanceKm = ConvertMiToKm(rec.DistanceMi);
                    var photoCount = rec.User.Photos?.Count ?? 0;

                    Console.WriteLine($"Name: {rec.User.Name}");
                    Console.WriteLine($"Age: {age?.ToString() ?? "N/A"} years old");
                    Console.WriteLine($"Distance: {distanceKm:F2} km ({rec.DistanceMi} miles)");
                    Console.WriteLine($"Photos: {photoCount}");

                    var bio = rec.User.Bio ?? string.Empty;
                    var bioLower = bio.ToLowerInvariant();
                    var hasExcludedKeyword = bioExcludeKeywords.Length > 0 && bioExcludeKeywords.Any(bioLower.Contains);

                    if (hasExcludedKeyword)
                    {
                        Console.WriteLine($"X Bio contains excluded keyword. Skipping...");
                    }

                    var meetsCriteria = !hasExcludedKeyword &&
                                        age.HasValue &&
                                        age >= minAge && age <= maxAge &&
                                        distanceKm <= maxDistanceKm &&
                                        photoCount >= minPhotos;

                    if (meetsCriteria)
                    {
                        var randomPhoto = rec.User.Photos?
                            .Where(p => !string.IsNullOrEmpty(p.Id))
                            .OrderBy(x => Guid.NewGuid())
                            .FirstOrDefault();

                        if (randomPhoto != null && !string.IsNullOrEmpty(rec.User.Id))
                        {
                            try
                            {
                                Console.WriteLine($"V Meets criteria! Sending like...");

                                var likeResponse = await service.LikeAsync(
                                    rec.User.Id,
                                    rec.SNumber,
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
                        if (!string.IsNullOrEmpty(rec.User.Id))
                        {
                            try
                            {
                                Console.WriteLine($"X Does not meet criteria. Sending pass...");

                                var passResponse = await service.PassAsync(
                                    rec.User.Id,
                                    rec.SNumber);

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

                    await RandomDelayAsync(delayRecsMinMs, delayRecsMaxMs);
                }
            }

            Console.WriteLine($"\n[RECS] === Summary ===");
            Console.WriteLine($"[RECS] Total likes sent: {likesCount}/{recs.Count}");
            Console.WriteLine($"[RECS] Total passes sent: {passesCount}/{recs.Count}");
        }
        else
        {
            Console.WriteLine("[RECS] No results found.");
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"[RECS] Error: {ex.Message}", ex);
    }
}

async Task MatchesAsync()
{
    try
    {
        Console.WriteLine(new string('-', 50));
        Console.WriteLine("\n[MATCHES] Starting matches processing...");

        var matchesResponse = await service.GetMatchesAsync(locale: "pt", count: 60, message: 0, isTinderU: false, includeConversations: true);

        if (matchesResponse.Meta?.Status != 200)
        {
            throw new InvalidOperationException($"HTTP Status code: {matchesResponse.Meta?.Status ?? 0}");
        }

        if (matchesResponse.Data?.Matches != null)
        {
            Console.WriteLine($"\n[MATCHES] Found {matchesResponse.Data.Matches.Count} matches:");

            var messagesSent = 0;

            foreach (var match in matchesResponse.Data.Matches)
            {
                if (match.Person != null &&
                    !string.IsNullOrEmpty(match.Id) &&
                    !string.IsNullOrEmpty(match.Person.Id) &&
                    match.MessageCount == 0)
                {
                    try
                    {
                        var userId = match.Id.Replace(match.Person.Id, "").Trim();

                        if (string.IsNullOrEmpty(userId))
                        {
                            Console.WriteLine($"[MATCHES] Could not extract userId from match.Id. Skipping match {match.Id}");
                            continue;
                        }

                        Console.WriteLine($"Match: {match.Person.Name} (ID: {match.Id})");

                        var personalizedMessage = message.Replace("{{NAME}}", match.Person.Name ?? "");

                        var messageParts = personalizedMessage.Split(['.'], StringSplitOptions.RemoveEmptyEntries)
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
                                userId,
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

                            await RandomDelayAsync(delayMessagesMinMs, delayMessagesMaxMs);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message to {match.Person.Name}: {ex.Message}");
                    }

                    Console.WriteLine(new string('-', 50));

                    await RandomDelayAsync(delayMatchesMinMs, delayMatchesMaxMs);
                }
                else
                {
                    Console.WriteLine($"[MATCHES] Missing required data (Person, Id, Person.Id, or MessageCount > 0). Skipping match.");
                }
            }

            Console.WriteLine($"\n[MATCHES] === Summary ===");
            Console.WriteLine($"[MATCHES] Total messages sent: {messagesSent}");
        }
        else
        {
            Console.WriteLine($"[MATCHES] Status: {matchesResponse.Meta?.Status}");
            Console.WriteLine("[MATCHES] No matches found.");
        }
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"[MATCHES] Error: {ex.Message}", ex);
    }
}

static async Task RandomDelayAsync(int minMilliseconds, int maxMilliseconds)
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
