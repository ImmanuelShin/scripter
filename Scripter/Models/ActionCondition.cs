namespace Scripter.Models;

public class ActionCondition
{
    public ConditionKind Kind { get; set; } = ConditionKind.Always;
    public string? ImagePath { get; set; }
    public int RegionX { get; set; }
    public int RegionY { get; set; }
    public int RegionW { get; set; }
    public int RegionH { get; set; }
    public int Threshold { get; set; } = 70;

    public bool HasRegion => RegionW > 0 && RegionH > 0;

    public ActionCondition Clone() => new()
    {
        Kind = Kind,
        ImagePath = ImagePath,
        RegionX = RegionX,
        RegionY = RegionY,
        RegionW = RegionW,
        RegionH = RegionH,
        Threshold = Threshold
    };

    public string GetSummary() => Kind switch
    {
        ConditionKind.Always => "always",
        ConditionKind.ImageFound => ImageSummary("image found"),
        ConditionKind.ImageNotFound => ImageSummary("image not found"),
        _ => Kind.ToString()
    };

    private string ImageSummary(string prefix)
    {
        var region = HasRegion ? $" in ({RegionX},{RegionY} {RegionW}x{RegionH})" : "";
        return $"{prefix}{region} ≥{Math.Clamp(Threshold, 1, 100)}%";
    }
}
