using System.Text.Json.Serialization;

namespace MagicMatch.Models;

public class CropInfo
{
    public CropDetail? User { get; set; }
    
    public CropDetail? Algo { get; set; }
    
    [JsonPropertyName("processed_by_bullseye")]
    public bool ProcessedByBullseye { get; set; }
    
    [JsonPropertyName("user_customized")]
    public bool UserCustomized { get; set; }
    
    public List<Face>? Faces { get; set; }
}

public class CropDetail
{
    [JsonPropertyName("width_pct")]
    public double WidthPct { get; set; }
    
    [JsonPropertyName("x_offset_pct")]
    public double XOffsetPct { get; set; }
    
    [JsonPropertyName("height_pct")]
    public double HeightPct { get; set; }
    
    [JsonPropertyName("y_offset_pct")]
    public double YOffsetPct { get; set; }
}

public class Face
{
    public CropDetail? Algo { get; set; }
    
    [JsonPropertyName("bounding_box_percentage")]
    public double BoundingBoxPercentage { get; set; }
}
