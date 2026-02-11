namespace obiektowe_projekt.Models;

public class DrawingData
{
    public List<Stroke> Strokes { get; set; } = new();
}

public class Stroke
{
    public List<DrawingPoint> Points { get; set; } = new();
    public float Thickness { get; set; } = 2f;
    public uint ArgbColor { get; set; } = 0xFF000000;
}

public readonly record struct DrawingPoint(float X, float Y);

public class AudioAttachment
{
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
}
