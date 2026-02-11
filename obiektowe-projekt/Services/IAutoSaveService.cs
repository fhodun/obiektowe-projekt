namespace obiektowe_projekt.Services;

public interface IAutoSaveService
{
    void Start(TimeSpan interval, Func<Task> callback);
    void Stop();
}
