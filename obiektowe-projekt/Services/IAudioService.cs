using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public interface IAudioService
{
    bool IsRecording { get; }
    bool IsPlaying { get; }

    Task<Result<bool>> StartRecordingAsync(Guid noteId, CancellationToken cancellationToken = default);
    Task<Result<AudioAttachment>> StopRecordingAsync(CancellationToken cancellationToken = default);
    Task<Result<bool>> PlayAsync(AudioAttachment attachment, CancellationToken cancellationToken = default);
    Task StopPlaybackAsync();
    Task<Result<AudioAttachment?>> ValidateAttachmentAsync(AudioAttachment? attachment, CancellationToken cancellationToken = default);
}
