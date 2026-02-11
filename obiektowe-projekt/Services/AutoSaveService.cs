namespace obiektowe_projekt.Services;

public class AutoSaveService : IAutoSaveService, IDisposable
{
    private Timer? _timer;

    public void Start(TimeSpan interval, Func<Task> callback)
    {
        Stop();
        _timer = new Timer(async _ => await callback(), null, interval, interval);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose() => Stop();
}
