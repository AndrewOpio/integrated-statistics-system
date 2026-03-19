public interface IEncryptionService
{
    string Encrypt(decimal amount);
    decimal Decrypt(string encryptedValue);
}