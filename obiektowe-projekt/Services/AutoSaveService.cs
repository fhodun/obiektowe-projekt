namespace obiektowe_projekt.Services;

public class AutoSaveService : IAutoSaveService, IDisposable
{
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;

    public void Start(TimeSpan interval, Func<Task> callback)
    {
        Stop();

        _timer = new PeriodicTimer(interval);
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(callback, _timer, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose() => Stop();

    private static async Task RunLoopAsync(Func<Task> callback, PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await callback();
            }
        }
        catch (OperationCanceledException)
        {
            // intentionally ignored
        }
    }
}
