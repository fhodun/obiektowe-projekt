using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public interface IRepository<T>
{
    Task<Result<T>> LoadAsync(CancellationToken cancellationToken = default);
    Task<Result<bool>> SaveAsync(T data, CancellationToken cancellationToken = default);
}
