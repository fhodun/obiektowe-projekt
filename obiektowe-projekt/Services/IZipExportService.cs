using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public interface IZipExportService
{
    Task<Result<string>> ExportAsync(IReadOnlyCollection<Note> notes, CancellationToken cancellationToken = default);
}
