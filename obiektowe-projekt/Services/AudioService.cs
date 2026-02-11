using Plugin.Maui.Audio;
using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public sealed class AudioService : IAudioService
{
    private readonly IAudioManager _audioManager;
    private IAudioRecorder? _audioRecorder;
    private IAudioPlayer? _audioPlayer;
    private Stream? _audioPlaybackStream;
    private string? _currentRecordingPath;
    private DateTime _recordingStartedAtUtc;

    public AudioService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    public bool IsRecording => _audioRecorder?.IsRecording ?? false;

    public bool IsPlaying => _audioPlayer?.IsPlaying ?? false;

    public async Task<Result<string>> StartRecordingAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        if (IsRecording)
        {
            return Result<string>.Failure("Nagrywanie już trwa.");
        }

        if (IsPlaying)
        {
            return Result<string>.Failure("Zatrzymaj odtwarzanie przed rozpoczęciem nagrywania.");
        }

        var permissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();
        if (permissionStatus != PermissionStatus.Granted)
        {
            return Result<string>.Failure("Brak zgody na użycie mikrofonu.");
        }

        try
        {
            var audioDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "Audio");
            Directory.CreateDirectory(audioDirectory);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{noteId}_{timestamp}.wav";
            var filePath = Path.Combine(audioDirectory, fileName);

            _audioRecorder ??= _audioManager.CreateRecorder();
            await _audioRecorder.StartAsync(filePath);

            _currentRecordingPath = filePath;
            _recordingStartedAtUtc = DateTime.UtcNow;

            return Result<string>.Success("Nagrywanie... ");
        }
        catch (Exception ex)
        {
            _currentRecordingPath = null;
            return Result<string>.Failure($"Nie udało się rozpocząć nagrywania: {ex.Message}");
        }
    }

    public async Task<Result<AudioAttachment>> StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRecording || _audioRecorder is null || string.IsNullOrWhiteSpace(_currentRecordingPath))
        {
            return Result<AudioAttachment>.Failure("Nagrywanie nie jest aktywne.");
        }

        try
        {
            await _audioRecorder.StopAsync();

            if (!File.Exists(_currentRecordingPath))
            {
                return Result<AudioAttachment>.Failure("Nagranie nie zostało zapisane na dysku.");
            }

            var duration = DateTime.UtcNow - _recordingStartedAtUtc;
            var attachment = new AudioAttachment
            {
                FileName = Path.GetFileName(_currentRecordingPath),
                StoredPath = _currentRecordingPath,
                DurationSeconds = Math.Max(0, duration.TotalSeconds)
            };

            _currentRecordingPath = null;
            return Result<AudioAttachment>.Success(attachment);
        }
        catch (Exception ex)
        {
            _currentRecordingPath = null;
            return Result<AudioAttachment>.Failure($"Nie udało się zakończyć nagrywania: {ex.Message}");
        }
    }

    public async Task<Result<string>> PlayAsync(AudioAttachment attachment, CancellationToken cancellationToken = default)
    {
        if (IsRecording)
        {
            return Result<string>.Failure("Nie można odtwarzać podczas nagrywania.");
        }

        if (string.IsNullOrWhiteSpace(attachment.StoredPath) || !File.Exists(attachment.StoredPath))
        {
            return Result<string>.Failure("Plik audio nie istnieje.");
        }

        try
        {
            await StopPlaybackAsync(cancellationToken);

            _audioPlaybackStream = File.OpenRead(attachment.StoredPath);
            _audioPlayer = _audioManager.CreatePlayer(_audioPlaybackStream);
            _audioPlayer.Play();

            return Result<string>.Success("Odtwarzanie...");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Nie udało się odtworzyć audio: {ex.Message}");
        }
    }

    public Task StopPlaybackAsync(CancellationToken cancellationToken = default)
    {
        _audioPlayer?.Stop();
        _audioPlayer?.Dispose();
        _audioPlayer = null;
        _audioPlaybackStream?.Dispose();
        _audioPlaybackStream = null;

        return Task.CompletedTask;
    }
}
