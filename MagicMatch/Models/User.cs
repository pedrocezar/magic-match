using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class User
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    
    public List<Badge>? Badges { get; set; }
    
    public string? Bio { get; set; }
    
    [JsonPropertyName("birth_date")]
    public DateTime? BirthDate { get; set; }
    
    public string? Name { get; set; }
    
    public List<Photo>? Photos { get; set; }
    
    public int Gender { get; set; }
    
    public List<object>? Jobs { get; set; }
    
    public List<object>? Schools { get; set; }
    
    [JsonPropertyName("show_gender_on_profile")]
    public bool ShowGenderOnProfile { get; set; }
    
    [JsonPropertyName("recently_active")]
    public bool RecentlyActive { get; set; }
    
    [JsonPropertyName("selected_descriptors")]
    public List<SelectedDescriptor>? SelectedDescriptors { get; set; }
}
