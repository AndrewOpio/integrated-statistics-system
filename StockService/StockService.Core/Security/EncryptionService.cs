using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration config)
    {
        var keyBase64 = config["Encryption:Key"] 
            ?? throw new InvalidOperationException("Encryption key is not configured.");
        
        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption key must be 32 bytes (256-bit).");
    }

    public string Encrypt(decimal amount)
    {
        var plaintext = Encoding.UTF8.GetBytes(amount.ToString());

        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];  // 12 bytes
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];       // 16 bytes
        var ciphertext = new byte[plaintext.Length];

        RandomNumberGenerator.Fill(nonce); // unique nonce every time

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Combine nonce + tag + ciphertext into one base64 string
        var result = nonce.Concat(tag).Concat(ciphertext).ToArray();
        return Convert.ToBase64String(result);
    }

    public decimal Decrypt(string encryptedValue)
    {
        var data = Convert.FromBase64String(encryptedValue);

        int nonceSize = AesGcm.NonceByteSizes.MaxSize;   // 12
        int tagSize = AesGcm.TagByteSizes.MaxSize;         // 16

        var nonce      = data[..nonceSize];
        var tag        = data[nonceSize..(nonceSize + tagSize)];
        var ciphertext = data[(nonceSize + tagSize)..];
        var plaintext  = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, tagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return decimal.Parse(Encoding.UTF8.GetString(plaintext));
    }
}