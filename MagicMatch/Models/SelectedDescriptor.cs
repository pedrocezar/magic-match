using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class SelectedDescriptor
{
    public string? Id { get; set; }
    
    public string? Name { get; set; }
    
    public string? Prompt { get; set; }
    
    public string? Type { get; set; }
    
    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }
    
    [JsonPropertyName("icon_urls")]
    public List<IconUrlDetail>? IconUrls { get; set; }
    
    [JsonPropertyName("choice_selections")]
    public List<ChoiceSelection>? ChoiceSelections { get; set; }
    
    [JsonPropertyName("section_id")]
    public string? SectionId { get; set; }
    
    [JsonPropertyName("section_name")]
    public string? SectionName { get; set; }
}

public class IconUrlDetail
{
    public string? Url { get; set; }
    
    public string? Quality { get; set; }
    
    public int Width { get; set; }
    
    public int Height { get; set; }
}

public class ChoiceSelection
{
    public string? Id { get; set; }
    
    public string? Name { get; set; }
    
    [JsonPropertyName("match_group_key")]
    public string? MatchGroupKey { get; set; }
}
