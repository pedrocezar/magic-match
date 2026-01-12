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

var service = new TinderApiService(config);

try
{
    Console.WriteLine("Buscando recomendações do Tinder...");
    var response = await service.GetRecsCoreAsync(locale: "pt", duos: 0);

    if (response.Meta?.Status == 200 && response.Data?.Results != null)
    {
        Console.WriteLine($"\nEncontrados {response.Data.Results.Count} resultados:\n");
        
        int likesCount = 0;
        int passesCount = 0;

        foreach (var result in response.Data.Results)
        {
            if (result.User != null)
            {
                var age = CalculateAge(result.User.BirthDate);
                var distanceKm = ConvertMiToKm(result.DistanceMi);
                var photoCount = result.User.Photos?.Count ?? 0;
                
                Console.WriteLine($"Nome: {result.User.Name}");
                Console.WriteLine($"Idade: {age?.ToString() ?? "N/A"} anos");
                Console.WriteLine($"Distância: {distanceKm:F2} km ({result.DistanceMi} milhas)");
                Console.WriteLine($"Fotos: {photoCount}");
                
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
                            Console.WriteLine($"✓ Atende aos critérios! Dando like...");

                            var likeResponse = await service.LikeAsync(
                                result.User.Id,
                                result.SNumber,
                                randomPhoto.Id);

                            if (likeResponse.Status == 200)
                            {
                                likesCount++;
                                Console.WriteLine($"   Like enviado!");
                            }
                            else
                            {
                                Console.WriteLine($"   Erro ao dar like: Status {likeResponse.Status}");
                            }

                            Console.WriteLine($"   Like enviado!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"   Erro ao dar like: {ex.Message}");
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(result.User.Id))
                    {
                        try
                        {
                            Console.WriteLine($"✗ Não atende aos critérios. Dando pass...");
                            var passResponse = await service.PassAsync(
                                result.User.Id, 
                                result.SNumber);
                            
                            if (passResponse.Status == 200)
                            {
                                passesCount++;
                                Console.WriteLine($"   Pass enviado!");
                            }
                            else
                            {
                                Console.WriteLine($"   Erro ao dar pass: Status {passResponse.Status}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"   Erro ao dar pass: {ex.Message}");
                        }
                    }
                }
                
                Console.WriteLine(new string('-', 50));
                
                await Task.Delay(1000);
            }
        }
        
        Console.WriteLine($"\n=== Resumo ===");
        Console.WriteLine($"Total de likes enviados: {likesCount}");
        Console.WriteLine($"Total de passes enviados: {passesCount}");
    }
    else
    {
        Console.WriteLine($"Status: {response.Meta?.Status}");
        Console.WriteLine("Nenhum resultado encontrado.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Erro ao buscar recomendações: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Detalhes: {ex.InnerException.Message}");
    }
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
