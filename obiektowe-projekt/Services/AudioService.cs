using Plugin.Maui.Audio;
using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public class AudioService : IAudioService
{
    private readonly IAudioManager _audioManager;
    private readonly string _audioDirectory;
    private IAudioRecorder? _recorder;
    private IAudioPlayer? _player;
    private Stream? _playbackStream;
    private DateTimeOffset? _recordingStartedAt;

    public AudioService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
        _audioDirectory = Path.Combine(FileSystem.Current.AppDataDirectory, "Audio");
        Directory.CreateDirectory(_audioDirectory);
    }

    public bool IsRecording => _recorder?.IsRecording == true;
    public bool IsPlaying => _player?.IsPlaying == true;

    public async Task<Result<bool>> StartRecordingAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        if (IsPlaying)
        {
            return Result<bool>.Failure("Zatrzymaj odtwarzanie przed rozpoczęciem nagrywania.");
        }

        var permissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();
        if (permissionStatus != PermissionStatus.Granted)
        {
            return Result<bool>.Failure("Brak zgody na użycie mikrofonu.");
        }

        if (!_audioManager.CanRecordAudio)
        {
            return Result<bool>.Failure("Nagrywanie audio nie jest dostępne na tym urządzeniu.");
        }

        if (IsRecording)
        {
            return Result<bool>.Failure("Nagrywanie już trwa.");
        }

        try
        {
            _recorder = _audioManager.CreateRecorder();
            var fileName = $"{noteId}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.wav";
            var filePath = Path.Combine(_audioDirectory, fileName);

            await _recorder.StartAsync(filePath);
            _recordingStartedAt = DateTimeOffset.UtcNow;

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Nie udało się rozpocząć nagrywania: {ex.Message}");
        }
    }

    public async Task<Result<AudioAttachment>> StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRecording || _recorder is null)
        {
            return Result<AudioAttachment>.Failure("Nagrywanie nie jest aktywne.");
        }

        try
        {
            var filePath = await _recorder.StopAsync();
            var duration = _recordingStartedAt.HasValue
                ? (DateTimeOffset.UtcNow - _recordingStartedAt.Value).TotalSeconds
                : (double?)null;

            var attachment = new AudioAttachment
            {
                FileName = Path.GetFileName(filePath),
                StoredPath = filePath,
                DurationSeconds = duration > 0 ? Math.Round(duration.Value, 1) : null
            };

            _recordingStartedAt = null;
            _recorder = null;
            return Result<AudioAttachment>.Success(attachment);
        }
        catch (Exception ex)
        {
            return Result<AudioAttachment>.Failure($"Nie udało się zakończyć nagrywania: {ex.Message}");
        }
    }

    public async Task<Result<bool>> PlayAsync(AudioAttachment attachment, CancellationToken cancellationToken = default)
    {
        if (IsRecording)
        {
            return Result<bool>.Failure("Nie można odtwarzać podczas nagrywania.");
        }

        if (string.IsNullOrWhiteSpace(attachment.StoredPath) || !File.Exists(attachment.StoredPath))
        {
            return Result<bool>.Failure("Plik audio nie istnieje.");
        }

        try
        {
            await StopPlaybackAsync();

            _playbackStream = File.OpenRead(attachment.StoredPath);
            _player = _audioManager.CreatePlayer(_playbackStream);
            _player.Play();
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Nie udało się odtworzyć audio: {ex.Message}");
        }
    }

    public Task StopPlaybackAsync()
    {
        _player?.Stop();
        _player?.Dispose();
        _player = null;
        _playbackStream?.Dispose();
        _playbackStream = null;
        return Task.CompletedTask;
    }

    public Task<Result<AudioAttachment?>> ValidateAttachmentAsync(AudioAttachment? attachment, CancellationToken cancellationToken = default)
    {
        if (attachment is null)
        {
            return Task.FromResult(Result<AudioAttachment?>.Success(null));
        }

        if (string.IsNullOrWhiteSpace(attachment.StoredPath) || !File.Exists(attachment.StoredPath))
        {
            return Task.FromResult(Result<AudioAttachment?>.Failure("Plik audio nie istnieje. Referencja została usunięta."));
        }

        return Task.FromResult(Result<AudioAttachment?>.Success(attachment));
    }
}
