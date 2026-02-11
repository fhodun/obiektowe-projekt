using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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
    private readonly IDrawingService _drawingService;
    private readonly IAudioService _audioService;

    private bool _isDirty;

    [ObservableProperty]
    private ObservableCollection<Note> notes = new();

    [ObservableProperty]
    private Note? selectedNote;

    [ObservableProperty]
    private string statusMessage = "Gotowe.";

    [ObservableProperty]
    private string audioStatus = "No audio";

    [ObservableProperty]
    private bool isRecording;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private Color selectedColor = Colors.Black;

    [ObservableProperty]
    private float selectedThickness = 2f;

    [ObservableProperty]
    private Stroke? currentStroke;

    public event Action? DrawingChanged;

    public string SelectedNoteDates => SelectedNote is null
        ? "Brak wybranej notatki"
        : $"Created: {SelectedNote.CreatedAt:G} | Updated: {SelectedNote.UpdatedAt:G}";

    public bool CanRecordAudio => SelectedNote is not null && !IsPlaying && !IsRecording;

    public bool CanStopRecording => SelectedNote is not null && IsRecording;

    public bool CanPlayAudio => SelectedNote?.Audio is not null && !IsRecording && !IsPlaying;

    public bool CanStopPlayback => IsPlaying;

    public MainViewModel(
        IRepository<List<Note>> repository,
        IZipExportService zipExportService,
        IAutoSaveService autoSaveService,
        IDrawingService drawingService,
        IAudioService audioService)
    {
        _repository = repository;
        _zipExportService = zipExportService;
        _autoSaveService = autoSaveService;
        _drawingService = drawingService;
        _audioService = audioService;

        Notes.CollectionChanged += OnNotesCollectionChanged;
        _autoSaveService.Start(TimeSpan.FromSeconds(30), AutoSaveIfDirtyAsync);
    }

    partial void OnSelectedNoteChanged(Note? oldValue, Note? newValue)
    {
        OnPropertyChanged(nameof(SelectedNoteDates));
        CurrentStroke = null;
        UpdateAudioState();
        DrawingChanged?.Invoke();
    }

    partial void OnIsRecordingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRecordAudio));
        OnPropertyChanged(nameof(CanStopRecording));
        OnPropertyChanged(nameof(CanPlayAudio));
    }

    partial void OnIsPlayingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanRecordAudio));
        OnPropertyChanged(nameof(CanPlayAudio));
        OnPropertyChanged(nameof(CanStopPlayback));
    }

    [RelayCommand]
    private void SelectBlackColor() => SelectedColor = Colors.Black;

    [RelayCommand]
    private void SelectBlueColor() => SelectedColor = Colors.Blue;

    [RelayCommand]
    private void SelectThinStroke() => SelectedThickness = 2f;

    [RelayCommand]
    private void SelectThickStroke() => SelectedThickness = 6f;

    public void StartStroke(Point point)
    {
        if (SelectedNote is null)
        {
            return;
        }

        CurrentStroke = _drawingService.StartStroke(point, SelectedColor, SelectedThickness);
        DrawingChanged?.Invoke();
    }

    public void AddStrokePoint(Point point)
    {
        _drawingService.AddPoint(CurrentStroke, point);
        DrawingChanged?.Invoke();
    }

    public void EndStroke()
    {
        var didCommit = _drawingService.CommitStroke(SelectedNote, CurrentStroke);
        CurrentStroke = null;

        if (didCommit)
        {
            TouchSelectedNote();
        }

        DrawingChanged?.Invoke();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var result = await _repository.LoadAsync();
        if (!result.IsSuccess)
        {
            StatusMessage = result.Error;
            return;
        }

        var loaded = result.Value ?? new List<Note>();
        RebindNotes(new ObservableCollection<Note>(loaded));

        SelectedNote = Notes.OrderByDescending(n => n.UpdatedAt).FirstOrDefault();
        _isDirty = false;
        StatusMessage = loaded.Count == 0 ? "Brak zapisanych danych. Utwórz pierwszą notatkę." : "Wczytano dane.";
        UpdateAudioState();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        foreach (var note in Notes)
        {
            note.EnsureValidTitle();
        }

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
        var note = new Note
        {
            Title = "Untitled",
            UpdatedAt = DateTime.Now
        };

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
        SelectedNote = Notes.OrderByDescending(n => n.UpdatedAt).FirstOrDefault();
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
        TouchSelectedNote();
        DrawingChanged?.Invoke();
    }

    [RelayCommand]
    private void ClearDrawing()
    {
        if (SelectedNote is null)
        {
            return;
        }

        SelectedNote.Drawing.Strokes.Clear();
        TouchSelectedNote();
        DrawingChanged?.Invoke();
    }

    [RelayCommand]
    private async Task StartRecordingAsync()
    {
        if (SelectedNote is null)
        {
            AudioStatus = "Najpierw wybierz notatkę.";
            return;
        }

        var result = await _audioService.StartRecordingAsync(SelectedNote.Id);
        if (result.IsSuccess)
        {
            IsRecording = true;
            AudioStatus = "Recording...";
            return;
        }

        AudioStatus = result.Error;
    }

    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        if (SelectedNote is null)
        {
            return;
        }

        var result = await _audioService.StopRecordingAsync();
        IsRecording = false;

        if (!result.IsSuccess || result.Value is null)
        {
            AudioStatus = result.Error;
            return;
        }

        SelectedNote.Audio = result.Value;
        TouchSelectedNote();
        UpdateAudioState();
        AudioStatus = $"Recorded: {result.Value.FileName}";
    }

    [RelayCommand]
    private async Task PlayAudioAsync()
    {
        if (SelectedNote?.Audio is null)
        {
            AudioStatus = "No audio";
            return;
        }

        var playResult = await _audioService.PlayAsync(SelectedNote.Audio);
        if (playResult.IsSuccess)
        {
            IsPlaying = true;
            AudioStatus = "Playing...";
            return;
        }

        if (playResult.Error.Contains("nie istnieje", StringComparison.OrdinalIgnoreCase)
            || playResult.Error.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
        {
            SelectedNote.Audio = null;
            TouchSelectedNote();
            UpdateAudioState();
        }

        AudioStatus = playResult.Error;
    }

    [RelayCommand]
    private async Task StopAudioAsync()
    {
        await _audioService.StopPlaybackAsync();
        IsPlaying = false;
        UpdateAudioState();
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

    private void RebindNotes(ObservableCollection<Note> newNotes)
    {
        Notes.CollectionChanged -= OnNotesCollectionChanged;
        foreach (var note in Notes)
        {
            note.PropertyChanged -= OnNotePropertyChanged;
        }

        Notes = newNotes;

        foreach (var note in Notes)
        {
            note.PropertyChanged += OnNotePropertyChanged;
        }

        Notes.CollectionChanged += OnNotesCollectionChanged;
    }

    private void OnNotesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<Note>())
            {
                item.PropertyChanged += OnNotePropertyChanged;
                item.EnsureValidTitle();
            }
        }

        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<Note>())
            {
                item.PropertyChanged -= OnNotePropertyChanged;
            }
        }

        MarkDirty();
    }

    private void OnNotePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Note note)
        {
            return;
        }

        if (e.PropertyName is nameof(Note.Title) && string.IsNullOrWhiteSpace(note.Title))
        {
            note.EnsureValidTitle();
        }

        if (e.PropertyName is nameof(Note.Title) or nameof(Note.Body))
        {
            TouchNote(note);
        }

        if (ReferenceEquals(note, SelectedNote))
        {
            OnPropertyChanged(nameof(SelectedNoteDates));

            if (e.PropertyName is nameof(Note.Audio))
            {
                UpdateAudioState();
            }
        }
    }

    private void TouchSelectedNote()
    {
        if (SelectedNote is null)
        {
            return;
        }

        TouchNote(SelectedNote);
    }

    private void TouchNote(Note note)
    {
        note.UpdatedAt = DateTime.Now;
        MarkDirty();

        if (ReferenceEquals(note, SelectedNote))
        {
            OnPropertyChanged(nameof(SelectedNoteDates));
        }
    }

    private void MarkDirty() => _isDirty = true;

    private void UpdateAudioState()
    {
        OnPropertyChanged(nameof(CanRecordAudio));
        OnPropertyChanged(nameof(CanStopRecording));
        OnPropertyChanged(nameof(CanPlayAudio));
        OnPropertyChanged(nameof(CanStopPlayback));

        if (SelectedNote?.Audio is null)
        {
            if (!IsRecording)
            {
                AudioStatus = "No audio";
            }

            return;
        }

        var durationPart = SelectedNote.Audio.DurationSeconds is > 0
            ? $" ({TimeSpan.FromSeconds(SelectedNote.Audio.DurationSeconds.Value):mm\\:ss})"
            : string.Empty;

        if (!IsRecording && !IsPlaying)
        {
            AudioStatus = $"Audio: {SelectedNote.Audio.FileName}{durationPart}";
        }
    }
}
