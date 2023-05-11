using System.Text;
using Wavee.Spotify.Infrastructure.Crypto;

namespace Wavee.Spotify.Tests;

public sealed class ShannonTests
{
    private static byte[] testKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    private static byte[] originalData = Encoding.UTF8.GetBytes("This is a test message.");


    [Fact]
    public void TestEncryptionDecryption()
    {
        // Test data
        byte[] testKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        byte[] originalData = Encoding.UTF8.GetBytes("This is a test message.");
        byte[] encryptedData = new byte[originalData.Length];
        byte[] decryptedData = new byte[originalData.Length];

        // Initialize the Shannon cipher with the test key
        var encryptionShannon = new Shannon(testKey);

        // Encrypt the original data
        originalData.CopyTo(encryptedData, 0);
        encryptionShannon.Nonce(0);
        encryptionShannon.Encrypt(encryptedData);

        // Ensure the encrypted data is different from the original data
        Assert.NotEqual(BitConverter.ToString(originalData), BitConverter.ToString(encryptedData));

        // Decrypt the encrypted data
        encryptedData.CopyTo(decryptedData, 0);

        var decryptionShannon = new Shannon(testKey);
        decryptionShannon.Nonce(0);
        decryptionShannon.Decrypt(decryptedData);

        // Ensure the decrypted data is the same as the original data
        Assert.Equal(BitConverter.ToString(originalData), BitConverter.ToString(decryptedData));
    }
}