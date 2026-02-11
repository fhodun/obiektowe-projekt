using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public interface IDrawingService
{
    Stroke StartStroke(Point point, Color color, float thickness);
    void AddPoint(Stroke? stroke, Point point);
    bool CommitStroke(Note? note, Stroke? stroke);
}
