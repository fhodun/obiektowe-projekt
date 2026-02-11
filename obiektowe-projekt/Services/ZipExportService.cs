using System.IO.Compression;
using System.Text;
using System.Text.Json;
using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public class ZipExportService : IZipExportService
{
    public async Task<Result<string>> ExportAsync(IReadOnlyCollection<Note> notes, CancellationToken cancellationToken = default)
    {
        try
        {
            if (notes.Count == 0)
            {
                return Result<string>.Failure("Brak zaznaczonych notatek do eksportu.");
            }

            var exportPath = Path.Combine(FileSystem.Current.AppDataDirectory, "NotesExport.zip");
            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }

            await using var zipFs = new FileStream(exportPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            using var archive = new ZipArchive(zipFs, ZipArchiveMode.Create, leaveOpen: false);
            var entry = archive.CreateEntry("notes.json", CompressionLevel.Optimal);

            await using var stream = entry.Open();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
            await writer.WriteAsync(json.AsMemory(), cancellationToken);

            return Result<string>.Success(exportPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Błąd eksportu ZIP: {ex.Message}");
        }
    }
}
