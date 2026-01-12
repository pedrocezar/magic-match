namespace MagicMatch.Models;

public class RecsCoreResponse
{
    public Meta? Meta { get; set; }
    public RecsCoreData? Data { get; set; }
}

public class RecsCoreData
{
    public List<Result>? Results { get; set; }
}
