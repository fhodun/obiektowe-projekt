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

            var notesEntry = archive.CreateEntry("notes.json", CompressionLevel.Optimal);
            await using (var stream = notesEntry.Open())
            await using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
                await writer.WriteAsync(json.AsMemory(), cancellationToken);
            }

            var exportedAudioNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var note in notes)
            {
                if (note.Audio is null || string.IsNullOrWhiteSpace(note.Audio.StoredPath))
                {
                    continue;
                }

                if (!File.Exists(note.Audio.StoredPath))
                {
                    continue;
                }

                var fileName = string.IsNullOrWhiteSpace(note.Audio.FileName)
                    ? Path.GetFileName(note.Audio.StoredPath)
                    : note.Audio.FileName;

                if (string.IsNullOrWhiteSpace(fileName) || !exportedAudioNames.Add(fileName))
                {
                    continue;
                }

                var audioEntry = archive.CreateEntry(Path.Combine("Audio", fileName), CompressionLevel.Optimal);
                await using var audioEntryStream = audioEntry.Open();
                await using var audioFileStream = File.OpenRead(note.Audio.StoredPath);
                await audioFileStream.CopyToAsync(audioEntryStream, cancellationToken);
            }

            return Result<string>.Success(exportPath);
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Błąd eksportu ZIP: {ex.Message}");
        }
    }
}
