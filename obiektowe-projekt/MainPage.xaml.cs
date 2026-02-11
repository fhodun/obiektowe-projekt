using obiektowe_projekt.ViewModels;
using obiektowe_projekt.Views;

namespace obiektowe_projekt;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly NotesDrawable _drawable;
    private bool _isInitialLoadDone;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        _drawable = new NotesDrawable();
        DrawingView.Drawable = _drawable;

        _viewModel.DrawingChanged += RefreshDrawing;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedNote))
            {
                RefreshDrawing();
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isInitialLoadDone)
        {
            return;
        }

        _isInitialLoadDone = true;
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void OnDrawingStart(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        _viewModel.StartStroke(point);
    }

    private void OnDrawingDrag(object? sender, TouchEventArgs e)
    {
        var point = e.Touches.FirstOrDefault();
        _viewModel.AddStrokePoint(point);
    }

    private void OnDrawingEnd(object? sender, TouchEventArgs e)
    {
        _viewModel.EndStroke();
    }

    private void RefreshDrawing()
    {
        _drawable.DrawingData = _viewModel.SelectedNote?.Drawing ?? new();
        _drawable.CurrentStroke = _viewModel.CurrentStroke;
        DrawingView.Invalidate();
    }
}
