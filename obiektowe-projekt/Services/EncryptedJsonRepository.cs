using System.Text;
using System.Text.Json;
using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public class EncryptedJsonRepository<T> : IRepository<T>
{
    private readonly ICryptoService _cryptoService;
    private readonly string _filePath;
    private readonly string _password;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public EncryptedJsonRepository(ICryptoService cryptoService)
    {
        _cryptoService = cryptoService;

        var dataDirectory = FileSystem.Current.AppDataDirectory;
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "notes.json.enc");
        _password = "MauiNotesLocalSecret";
    }

    public async Task<Result<T>> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return Result<T>.Failure("Brak zapisanych danych.");
            }

            var encrypted = await File.ReadAllBytesAsync(_filePath, cancellationToken);
            var decryptedResult = _cryptoService.Decrypt(encrypted, _password);
            if (!decryptedResult.IsSuccess || decryptedResult.Value is null)
            {
                return Result<T>.Failure(decryptedResult.Error);
            }

            var json = Encoding.UTF8.GetString(decryptedResult.Value);
            var data = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            return data is null
                ? Result<T>.Failure("Nie udało się odczytać danych JSON.")
                : Result<T>.Success(data);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure($"Błąd odczytu: {ex.Message}");
        }
    }

    public async Task<Result<bool>> SaveAsync(T data, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var plainBytes = Encoding.UTF8.GetBytes(json);
            var encryptedResult = _cryptoService.Encrypt(plainBytes, _password);
            if (!encryptedResult.IsSuccess || encryptedResult.Value is null)
            {
                return Result<bool>.Failure(encryptedResult.Error);
            }

            await File.WriteAllBytesAsync(_filePath, encryptedResult.Value, cancellationToken);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Błąd zapisu: {ex.Message}");
        }
    }
}
