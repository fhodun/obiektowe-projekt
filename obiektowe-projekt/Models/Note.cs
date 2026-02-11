using System.Text.Json.Serialization;

namespace obiektowe_projekt.Models;

public class Note : IEquatable<Note>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "Nowa notatka";
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DrawingData Drawing { get; set; } = new();
    public AudioAttachment? Audio { get; set; }

    [JsonIgnore]
    public bool IsSelectedForExport { get; set; }

    public static bool operator ==(Note? left, Note? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Id == right.Id;
    }

    public static bool operator !=(Note? left, Note? right) => !(left == right);

    public static bool operator >(Note left, Note right) => left.UpdatedAt > right.UpdatedAt;

    public static bool operator <(Note left, Note right) => left.UpdatedAt < right.UpdatedAt;

    public static Note operator +(Note left, Note right)
    {
        return new Note
        {
            Title = $"{left.Title} + {right.Title}",
            Body = string.Join(Environment.NewLine, new[] { left.Body, right.Body }.Where(x => !string.IsNullOrWhiteSpace(x))),
            CreatedAt = left.CreatedAt < right.CreatedAt ? left.CreatedAt : right.CreatedAt,
            UpdatedAt = DateTime.Now,
            Drawing = new DrawingData
            {
                Strokes = left.Drawing.Strokes.Concat(right.Drawing.Strokes)
                    .Select(CloneStroke)
                    .ToList()
            }
        };
    }

    public bool Equals(Note? other) => this == other;

    public override bool Equals(object? obj) => obj is Note other && Equals(other);

    public override int GetHashCode() => Id.GetHashCode();

    public Note DeepClone()
    {
        return new Note
        {
            Id = Guid.NewGuid(),
            Title = $"{Title} (kopia)",
            Body = Body,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Drawing = new DrawingData
            {
                Strokes = Drawing.Strokes.Select(CloneStroke).ToList()
            },
            Audio = Audio is null ? null : new AudioAttachment { FileName = Audio.FileName, StoredPath = Audio.StoredPath }
        };
    }

    private static Stroke CloneStroke(Stroke stroke)
    {
        return new Stroke
        {
            Thickness = stroke.Thickness,
            ArgbColor = stroke.ArgbColor,
            Points = stroke.Points.Select(p => new DrawingPoint(p.X, p.Y)).ToList()
        };
    }
}
