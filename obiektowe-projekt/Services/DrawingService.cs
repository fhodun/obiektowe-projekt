using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public class DrawingService : IDrawingService
{
    public Stroke StartStroke(Point point, Color color, float thickness)
    {
        return new Stroke
        {
            Thickness = thickness,
            ArgbColor = color.ToUint(),
            Points = new List<DrawingPoint> { new((float)point.X, (float)point.Y) }
        };
    }

    public void AddPoint(Stroke? stroke, Point point)
    {
        stroke?.Points.Add(new DrawingPoint((float)point.X, (float)point.Y));
    }

    public bool CommitStroke(Note? note, Stroke? stroke)
    {
        if (note is null || stroke is null || stroke.Points.Count < 2)
        {
            return false;
        }

        note.Drawing.Strokes.Add(stroke);
        return true;
    }
}
