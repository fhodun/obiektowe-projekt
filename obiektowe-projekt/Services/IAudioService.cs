using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public interface IAudioService
{
    bool IsRecording { get; }
    bool IsPlaying { get; }

    Task<Result<string>> StartRecordingAsync(Guid noteId, CancellationToken cancellationToken = default);
    Task<Result<AudioAttachment>> StopRecordingAsync(CancellationToken cancellationToken = default);
    Task<Result<string>> PlayAsync(AudioAttachment attachment, CancellationToken cancellationToken = default);
    Task StopPlaybackAsync(CancellationToken cancellationToken = default);
}
