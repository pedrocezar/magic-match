using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class Photo
{
    public string? Id { get; set; }
    
    [JsonPropertyName("crop_info")]
    public CropInfo? CropInfo { get; set; }
    
    public string? Url { get; set; }
    
    [JsonPropertyName("processed_files")]
    public List<ProcessedFile>? ProcessedFiles { get; set; }
    
    [JsonPropertyName("processed_videos")]
    public List<object>? ProcessedVideos { get; set; }
    
    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }
    
    public string? Extension { get; set; }
    
    public List<object>? Assets { get; set; }
    
    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }
}

public class ProcessedFile
{
    public string? Url { get; set; }
    
    public int Height { get; set; }
    
    public int Width { get; set; }
}
