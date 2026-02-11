using obiektowe_projekt.Models;

namespace obiektowe_projekt.Views;

public class NotesDrawable : IDrawable
{
    public DrawingData DrawingData { get; set; } = new();
    public Stroke? CurrentStroke { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.White;
        canvas.FillRectangle(dirtyRect);

        foreach (var stroke in DrawingData.Strokes)
        {
            DrawStroke(canvas, stroke);
        }

        if (CurrentStroke is not null)
        {
            DrawStroke(canvas, CurrentStroke);
        }
    }

    private static void DrawStroke(ICanvas canvas, Stroke stroke)
    {
        if (stroke.Points.Count < 2)
        {
            return;
        }

        canvas.StrokeColor = Color.FromUint(stroke.ArgbColor);
        canvas.StrokeSize = stroke.Thickness;

        for (var i = 1; i < stroke.Points.Count; i++)
        {
            var from = stroke.Points[i - 1];
            var to = stroke.Points[i];
            canvas.DrawLine(from.X, from.Y, to.X, to.Y);
        }
    }
}
