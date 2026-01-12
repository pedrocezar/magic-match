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

var service = new TinderApiService(config);

var recsTask = Task.Run(async () =>
{
    try
    {
        Console.WriteLine("Fetching Tinder recommendations...");
        var response = await service.GetRecsCoreAsync(locale: "pt", duos: 0);

        if (response.Meta?.Status == 200 && response.Data?.Results != null)
        {
            Console.WriteLine($"\nFound {response.Data.Results.Count} results:\n");
            
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
                    
                    bool meetsCriteria = age.HasValue && 
                                        age >= 20 && age <= 50 && 
                                        distanceKm <= 15 && 
                                        photoCount >= 6;

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
                    
                    await Task.Delay(1000);
                }
            }
            
            Console.WriteLine($"\n=== Recommendations Summary ===");
            Console.WriteLine($"Total likes sent: {likesCount}");
            Console.WriteLine($"Total passes sent: {passesCount}");
        }
        else
        {
            Console.WriteLine($"Status: {response.Meta?.Status}");
            Console.WriteLine("No results found.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching recommendations: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Details: {ex.InnerException.Message}");
        }
    }
});

var matchesTask = Task.Run(async () =>
{
    try
    {
        Console.WriteLine("Fetching matches...");
        var matchesResponse = await service.GetMatchesAsync(locale: "pt", count: 60, message: 0, isTinderU: false, includeConversations: true);

        if (matchesResponse.Meta?.Status == 200 && matchesResponse.Data?.Matches != null)
        {
            Console.WriteLine($"\nFound {matchesResponse.Data.Matches.Count} matches:\n");
            
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
                        Console.WriteLine($"Sending message: {message}");

                        var messageResponse = await service.SendMessageAsync(
                            match.Id,
                            match.LikedContent.ByCloser.UserId,
                            match.Person.Id,
                            message);

                        if (!string.IsNullOrEmpty(messageResponse.Id))
                        {
                            messagesSent++;
                            Console.WriteLine($"Message sent! Message ID: {messageResponse.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"Error sending message");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message to {match.Person.Name}: {ex.Message}");
                    }
                    
                    Console.WriteLine(new string('-', 50));
                    
                    await Task.Delay(1000);
                }
            }
            
            Console.WriteLine($"\n=== Matches Summary ===");
            Console.WriteLine($"Total messages sent: {messagesSent}");
        }
        else
        {
            Console.WriteLine($"Matches Status: {matchesResponse.Meta?.Status}");
            Console.WriteLine("No matches found.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing matches: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Details: {ex.InnerException.Message}");
        }
    }
});

await Task.WhenAll(recsTask, matchesTask);

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
