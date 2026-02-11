using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public interface ICryptoService
{
    Result<byte[]> Encrypt(byte[] plainBytes, string password);
    Result<byte[]> Decrypt(byte[] encryptedBytes, string password);
}
