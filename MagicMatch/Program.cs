using MagicMatch.Models;
using MagicMatch.Services;
using Microsoft.Extensions.Logging;

var config = new TinderApiConfig
{
    AuthToken = Environment.GetEnvironmentVariable("TINDER_AUTH_TOKEN"),
    PersistentDeviceId = Environment.GetEnvironmentVariable("TINDER_PERSISTENT_DEVICE_ID")
};

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
});

var logger = loggerFactory.CreateLogger(nameof(Program));

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
var minNameLength = int.TryParse(Environment.GetEnvironmentVariable("TINDER_MIN_NAME_LENGTH"), out var minNameLengthValue) ? minNameLengthValue : 2;

var service = new TinderApiService(config);

await ExecuteAsync();

async Task ExecuteAsync()
{
    if (isProduction)
    {
        logger.LogInformation("Running in production mode. Executing once.");
        logger.LogInformation(new string('-', 10));
        await OnceExecuteAsync();
    }
    else
    {
        logger.LogInformation("Running in development mode. Executing indefinitely with error handling and delays.");
        logger.LogInformation(new string('-', 10));
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
        logger.LogError(ex, "Error occurred while executing once.");

        Environment.Exit(1);
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

            logger.LogError(ex, "Error (Attempt {Attempt}/{MaxErrors})", consecutiveErrors, maxErrors);

            if (consecutiveErrors >= maxErrors)
            {
                logger.LogError("Max errors reached ({MaxErrors}). Stopping recommendations task.", maxErrors);
                return;
            }
        }

        if (consecutiveErrors < maxErrors)
        {
            var waitMinutes = RandomDelayMinutes(delayBetweenExecutionsMinMinutes, delayBetweenExecutionsMaxMinutes);
            logger.LogInformation(new string('-', 10));
            logger.LogInformation("Waiting {WaitMinutes} minutes before next execution...", waitMinutes);
            logger.LogInformation(new string('-', 10));
            await Task.Delay(waitMinutes * 60 * 1000);
        }
    }
}

async Task RecsAsync()
{
    try
    {
        logger.LogInformation("[RECS] Starting recommendations processing...");
        logger.LogInformation(new string('-', 10));

        var recs = new List<Result>();
        var recsItemCount = 0;

        foreach (var i in Enumerable.Range(1, recsRequestsPerExecution))
        {
            logger.LogInformation("[RECS] Requesting recommendations (Request {Request}/{TotalRequests})...", i, recsRequestsPerExecution);
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
                logger.LogInformation("[RECS] Received {BatchCount}/{TotalItems} recommendations of {DistinctCount} (Request {Request}/{TotalRequests}).",
                    response.Data.Results.Count, recsItemCount, recs.Count, i, recsRequestsPerExecution);
            }
            else
            {
                logger.LogWarning("[RECS] No results found in response (Request {Request}/{TotalRequests}). Status: {Status}",
                    i, recsRequestsPerExecution, response.Meta?.Status);
            }

            logger.LogInformation(new string('-', 10));
            await RandomDelayAsync(delayRecsMinMs, delayRecsMaxMs);
        }

        if (recs.Any())
        {
            logger.LogInformation("[RECS] Found {Count} results:", recs.Count);
            logger.LogInformation(new string('-', 10));

            var likesCount = 0;
            var passesCount = 0;

            foreach (var rec in recs)
            {
                if (rec.User != null)
                {
                    var age = CalculateAge(rec.User.BirthDate);
                    var distanceKm = ConvertMiToKm(rec.DistanceMi);
                    var photoCount = rec.User.Photos?.Count ?? 0;
                    var nameLength = rec.User.Name?.Length ?? 0;

                    logger.LogInformation("Name: {Name}", rec.User.Name);
                    logger.LogInformation("Age: {Age} years old", age?.ToString() ?? "N/A");
                    logger.LogInformation("Distance: {DistanceKm:F2} km ({DistanceMi} miles)", distanceKm, rec.DistanceMi);
                    logger.LogInformation("Photos: {PhotoCount}", photoCount);

                    var bio = rec.User.Bio ?? string.Empty;
                    var bioLower = bio.ToLowerInvariant();
                    var hasExcludedKeyword = bioExcludeKeywords.Length > 0 && bioExcludeKeywords.Any(bioLower.Contains);

                    if (hasExcludedKeyword)
                    {
                        logger.LogInformation("X Bio contains excluded keyword. Skipping...");
                    }

                    var meetsCriteria = !hasExcludedKeyword &&
                                        nameLength >= minNameLength &&
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
                                    logger.LogInformation("Like sent!");
                                }
                                else
                                {
                                    logger.LogWarning("Error sending like: Status {Status}", likeResponse.Status);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error sending like.");
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
                                    logger.LogInformation("Pass sent!");
                                }
                                else
                                {
                                    logger.LogWarning("Error sending pass: Status {Status}", passResponse.Status);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error sending pass.");
                            }
                        }
                    }

                    logger.LogInformation(new string('-', 10));

                    await RandomDelayAsync(delayRecsMinMs, delayRecsMaxMs);
                }
            }

            logger.LogInformation("[RECS] === Summary ===");
            logger.LogInformation("[RECS] Total likes sent: {Likes}/{Total}", likesCount, recs.Count);
            logger.LogInformation("[RECS] Total passes sent: {Passes}/{Total}", passesCount, recs.Count);
        }
        else
        {
            logger.LogInformation("[RECS] No results found.");
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
        logger.LogInformation(new string('-', 10));
        logger.LogInformation("[MATCHES] Starting matches processing...");
        logger.LogInformation(new string('-', 10));

        var matchesResponse = await service.GetMatchesAsync(locale: "pt", count: 60, message: 0, isTinderU: false, includeConversations: true);

        if (matchesResponse.Meta?.Status != 200)
        {
            throw new InvalidOperationException($"HTTP Status code: {matchesResponse.Meta?.Status ?? 0}");
        }

        if (matchesResponse.Data?.Matches != null)
        {
            logger.LogInformation("[MATCHES] Found {Count} matches:", matchesResponse.Data.Matches.Count);
            logger.LogInformation(new string('-', 10));

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
                            logger.LogWarning("[MATCHES] Could not extract userId from match.Id. Skipping match {MatchId}", match.Id);
                            continue;
                        }

                        logger.LogInformation("Match: {Name}", match.Person.Name);

                        var personalizedMessage = message.Replace("{{NAME}}", match.Person.Name ?? "");

                        var messageParts = personalizedMessage.Split(['.'], StringSplitOptions.RemoveEmptyEntries)
                            .Select(part => part.Trim())
                            .Where(part => !string.IsNullOrWhiteSpace(part))
                            .ToList();

                        if (messageParts.Count == 0)
                        {
                            messageParts.Add(personalizedMessage.Trim());
                        }

                        logger.LogInformation("Sending {Count} message(s)", messageParts.Count);

                        foreach (var messagePart in messageParts)
                        {
                            logger.LogInformation("Sending message: {Message}", messagePart);

                            var messageResponse = await service.SendMessageAsync(
                                match.Id,
                                userId,
                                match.Person.Id,
                                messagePart);

                            if (!string.IsNullOrEmpty(messageResponse.Id))
                            {
                                messagesSent++;
                                logger.LogInformation("Message sent!");
                            }
                            else
                            {
                                logger.LogWarning("Error sending message");
                            }

                            await RandomDelayAsync(delayMessagesMinMs, delayMessagesMaxMs);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error sending message to {Name}", match.Person.Name);
                    }

                    logger.LogInformation(new string('-', 10));

                    await RandomDelayAsync(delayMatchesMinMs, delayMatchesMaxMs);
                }
                else
                {
                    logger.LogWarning("[MATCHES] Missing required data (Person, Id, Person.Id, or MessageCount > 0). Skipping match.");
                }
            }

            logger.LogInformation("[MATCHES] === Summary ===");
            logger.LogInformation("[MATCHES] Total messages sent: {MessagesSent}", messagesSent);
            logger.LogInformation("[MATCHES] Total matches processed: {TotalMatches}", matchesResponse.Data.Matches.Count);
        }
        else
        {
            logger.LogInformation("[MATCHES] Status: {Status}", matchesResponse.Meta?.Status);
            logger.LogInformation("[MATCHES] No matches found.");
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
