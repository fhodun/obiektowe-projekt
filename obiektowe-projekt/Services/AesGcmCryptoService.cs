using System.Security.Cryptography;
using obiektowe_projekt.Models;

namespace obiektowe_projekt.Services;

public class AesGcmCryptoService : ICryptoService
{
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int Iterations = 100_000;

    public Result<byte[]> Encrypt(byte[] plainBytes, string password)
    {
        try
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var key = DeriveKey(password, salt);

            var ciphertext = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Encrypt(nonce, plainBytes, ciphertext, tag);

            var packed = new byte[SaltSize + NonceSize + ciphertext.Length + TagSize];
            Buffer.BlockCopy(salt, 0, packed, 0, SaltSize);
            Buffer.BlockCopy(nonce, 0, packed, SaltSize, NonceSize);
            Buffer.BlockCopy(ciphertext, 0, packed, SaltSize + NonceSize, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, packed, SaltSize + NonceSize + ciphertext.Length, TagSize);

            return Result<byte[]>.Success(packed);
        }
        catch (Exception ex)
        {
            return Result<byte[]>.Failure($"Błąd szyfrowania: {ex.Message}");
        }
    }

    public Result<byte[]> Decrypt(byte[] encryptedBytes, string password)
    {
        try
        {
            if (encryptedBytes.Length < SaltSize + NonceSize + TagSize)
            {
                return Result<byte[]>.Failure("Nieprawidłowy format zaszyfrowanego pliku.");
            }

            var salt = encryptedBytes[..SaltSize];
            var nonce = encryptedBytes[SaltSize..(SaltSize + NonceSize)];
            var tagStart = encryptedBytes.Length - TagSize;
            var ciphertext = encryptedBytes[(SaltSize + NonceSize)..tagStart];
            var tag = encryptedBytes[tagStart..];

            var key = DeriveKey(password, salt);
            var plaintext = new byte[ciphertext.Length];

            using var aesGcm = new AesGcm(key, TagSize);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

            return Result<byte[]>.Success(plaintext);
        }
        catch (Exception ex)
        {
            return Result<byte[]>.Failure($"Błąd deszyfrowania: {ex.Message}");
        }
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }
}
