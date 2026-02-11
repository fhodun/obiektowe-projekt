using obiektowe_projekt.ViewModels;
using obiektowe_projekt.Views;

namespace obiektowe_projekt;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly NotesDrawable _drawable;
    private bool _suppressTextEvents;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _drawable = new NotesDrawable();
        DrawingView.Drawable = _drawable;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedNote))
            {
                SyncSelectedNote();
            }
        };

        _ = _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void SyncSelectedNote()
    {
        _suppressTextEvents = true;
        TitleEntry.Text = _viewModel.SelectedNote?.Title ?? string.Empty;
        BodyEditor.Text = _viewModel.SelectedNote?.Body ?? string.Empty;
        _suppressTextEvents = false;

        DatesLabel.Text = _viewModel.SelectedNote is null
            ? "Brak wybranej notatki"
            : $"Created: {_viewModel.SelectedNote.CreatedAt:G} | Updated: {_viewModel.SelectedNote.UpdatedAt:G}";

        _drawable.DrawingData = _viewModel.SelectedNote?.Drawing ?? new();
        _drawable.CurrentStroke = _viewModel.GetCurrentStroke();
        DrawingView.Invalidate();
    }

    private void OnTitleChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressTextEvents)
        {
            return;
        }

        _viewModel.UpdateTitle(e.NewTextValue ?? string.Empty);
        SyncSelectedNote();
    }

    private void OnBodyChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressTextEvents)
        {
            return;
        }

        _viewModel.UpdateBody(e.NewTextValue ?? string.Empty);
        SyncSelectedNote();
    }

    private void OnBlackColorClicked(object? sender, EventArgs e) => _viewModel.SelectedColor = Colors.Black;
    private void OnBlueColorClicked(object? sender, EventArgs e) => _viewModel.SelectedColor = Colors.Blue;
    private void OnThickness2Clicked(object? sender, EventArgs e) => _viewModel.SelectedThickness = 2f;
    private void OnThickness6Clicked(object? sender, EventArgs e) => _viewModel.SelectedThickness = 6f;

    private void OnDrawingStart(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        _viewModel.StartStroke(point);
        _drawable.CurrentStroke = _viewModel.GetCurrentStroke();
        DrawingView.Invalidate();
    }

    private void OnDrawingDrag(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        _viewModel.AddStrokePoint(point);
        _drawable.CurrentStroke = _viewModel.GetCurrentStroke();
        DrawingView.Invalidate();
    }

    private void OnDrawingEnd(object? sender, TouchEventArgs e)
    {
        _viewModel.EndStroke();
        _drawable.CurrentStroke = null;
        _drawable.DrawingData = _viewModel.SelectedNote?.Drawing ?? new();
        SyncSelectedNote();
    }
}
