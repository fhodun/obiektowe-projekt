using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using obiektowe_projekt.Models;
using obiektowe_projekt.Services;
using obiektowe_projekt.ViewModels;

namespace obiektowe_projekt;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .AddAudio()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ICryptoService, AesGcmCryptoService>();
        builder.Services.AddSingleton<IRepository<List<Note>>, EncryptedJsonRepository<List<Note>>>();
        builder.Services.AddSingleton<IZipExportService, ZipExportService>();
        builder.Services.AddSingleton<IAutoSaveService, AutoSaveService>();
        builder.Services.AddSingleton<IDrawingService, DrawingService>();
        builder.Services.AddSingleton<IAudioService, AudioService>();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
