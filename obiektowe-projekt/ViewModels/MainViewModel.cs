using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using obiektowe_projekt.Models;
using obiektowe_projekt.Services;

namespace obiektowe_projekt.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRepository<List<Note>> _repository;
    private readonly IZipExportService _zipExportService;
    private readonly IAutoSaveService _autoSaveService;

    private bool _isDirty;
    private Stroke? _currentStroke;

    [ObservableProperty]
    private ObservableCollection<Note> notes = new();

    [ObservableProperty]
    private Note? selectedNote;

    [ObservableProperty]
    private string statusMessage = "Gotowe.";

    [ObservableProperty]
    private Color selectedColor = Colors.Black;

    [ObservableProperty]
    private float selectedThickness = 2f;

    public IReadOnlyList<Color> AvailableColors { get; } = new[] { Colors.Black, Colors.Blue };
    public IReadOnlyList<float> AvailableThicknesses { get; } = new[] { 2f, 6f };

    public MainViewModel(
        IRepository<List<Note>> repository,
        IZipExportService zipExportService,
        IAutoSaveService autoSaveService)
    {
        _repository = repository;
        _zipExportService = zipExportService;
        _autoSaveService = autoSaveService;

        Notes.CollectionChanged += (_, _) => MarkDirty();
        _autoSaveService.Start(TimeSpan.FromSeconds(30), AutoSaveIfDirtyAsync);
    }

    partial void OnSelectedNoteChanged(Note? oldValue, Note? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsSelectedForExport = oldValue.IsSelectedForExport;
        }
    }

    public void UpdateTitle(string title)
    {
        if (SelectedNote is null)
        {
            return;
        }

        SelectedNote.Title = title;
        TouchNote();
    }

    public void UpdateBody(string body)
    {
        if (SelectedNote is null)
        {
            return;
        }

        SelectedNote.Body = body;
        TouchNote();
    }

    public void StartStroke(Point point)
    {
        if (SelectedNote is null)
        {
            return;
        }

        _currentStroke = new Stroke
        {
            Thickness = SelectedThickness,
            ArgbColor = SelectedColor.ToArgbHex(),
            Points = new List<DrawingPoint> { new((float)point.X, (float)point.Y) }
        };
    }

    public void AddStrokePoint(Point point)
    {
        _currentStroke?.Points.Add(new DrawingPoint((float)point.X, (float)point.Y));
    }

    public void EndStroke()
    {
        if (SelectedNote is null || _currentStroke is null)
        {
            return;
        }

        if (_currentStroke.Points.Count > 1)
        {
            SelectedNote.Drawing.Strokes.Add(_currentStroke);
            TouchNote();
        }

        _currentStroke = null;
    }

    public Stroke? GetCurrentStroke() => _currentStroke;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var result = await _repository.LoadAsync();
        if (!result.IsSuccess)
        {
            StatusMessage = result.Error;
            return;
        }

        Notes = new ObservableCollection<Note>(result.Value ?? new List<Note>());
        Notes.CollectionChanged += (_, _) => MarkDirty();

        SelectedNote = Notes.OrderByDescending(n => n.UpdatedAt).FirstOrDefault();
        _isDirty = false;
        StatusMessage = "Wczytano dane.";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var result = await _repository.SaveAsync(Notes.ToList());
        if (result.IsSuccess)
        {
            _isDirty = false;
            StatusMessage = "Zapisano notatki.";
            return;
        }

        StatusMessage = result.Error;
    }

    [RelayCommand]
    private void AddNote()
    {
        var note = new Note();
        Notes.Add(note);
        SelectedNote = note;
        MarkDirty();
    }

    [RelayCommand]
    private void DeleteSelectedNote()
    {
        if (SelectedNote is null)
        {
            return;
        }

        Notes.Remove(SelectedNote);
        SelectedNote = Notes.FirstOrDefault();
        MarkDirty();
    }

    [RelayCommand]
    private void DuplicateSelectedNote()
    {
        if (SelectedNote is null)
        {
            return;
        }

        var copy = SelectedNote.DeepClone();
        Notes.Add(copy);
        SelectedNote = copy;
        MarkDirty();
    }

    [RelayCommand]
    private void UndoStroke()
    {
        if (SelectedNote is null || SelectedNote.Drawing.Strokes.Count == 0)
        {
            return;
        }

        SelectedNote.Drawing.Strokes.RemoveAt(SelectedNote.Drawing.Strokes.Count - 1);
        TouchNote();
    }

    [RelayCommand]
    private void ClearDrawing()
    {
        if (SelectedNote is null)
        {
            return;
        }

        SelectedNote.Drawing.Strokes.Clear();
        TouchNote();
    }

    [RelayCommand]
    private async Task ExportSelectedAsync()
    {
        var selected = Notes.Where(n => n.IsSelectedForExport).ToList();
        var exportResult = await _zipExportService.ExportAsync(selected);
        StatusMessage = exportResult.IsSuccess
            ? $"Wyeksportowano: {exportResult.Value}"
            : exportResult.Error;
    }

    private async Task AutoSaveIfDirtyAsync()
    {
        if (!_isDirty)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () => await SaveAsync());
    }

    private void TouchNote()
    {
        if (SelectedNote is null)
        {
            return;
        }

        SelectedNote.UpdatedAt = DateTime.Now;
        MarkDirty();
    }

    private void MarkDirty() => _isDirty = true;
}

internal static class ColorExtensions
{
    public static uint ToArgbHex(this Color color)
    {
        var a = (uint)(color.Alpha * 255) << 24;
        var r = (uint)(color.Red * 255) << 16;
        var g = (uint)(color.Green * 255) << 8;
        var b = (uint)(color.Blue * 255);
        return a | r | g | b;
    }
}
