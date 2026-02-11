using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace obiektowe_projekt.Models;

public partial class Note : ObservableObject, IEquatable<Note>
{
    [ObservableProperty]
    private Guid id = Guid.NewGuid();

    [ObservableProperty]
    private string title = "Nowa notatka";

    [ObservableProperty]
    private string body = string.Empty;

    [ObservableProperty]
    private DateTime createdAt = DateTime.Now;

    [ObservableProperty]
    private DateTime updatedAt = DateTime.Now;

    [ObservableProperty]
    private DrawingData drawing = new();

    [ObservableProperty]
    private AudioAttachment? audio;

    [JsonIgnore]
    [ObservableProperty]
    private bool isSelectedForExport;

    public void EnsureValidTitle()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            Title = "Untitled";
        }
    }

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
            Title = string.IsNullOrWhiteSpace(Title) ? "Untitled (kopia)" : $"{Title} (kopia)",
            Body = Body,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Drawing = new DrawingData
            {
                Strokes = Drawing.Strokes.Select(CloneStroke).ToList()
            },
            Audio = Audio is null ? null : new AudioAttachment
            {
                FileName = Audio.FileName,
                StoredPath = Audio.StoredPath,
                DurationSeconds = Audio.DurationSeconds
            }
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
