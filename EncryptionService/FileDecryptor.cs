using KeyServiceAPI;
using KeyServiceAPI.Models;
using StreamGatewayCoreUtilities.CommonExceptions;
using System.Security.Cryptography;

namespace EncryptionService
{
    public class FileDecryptor : IFileDecryptor
    {
        private readonly IKeyServiceClient _keyServiceClient;
        public FileDecryptor(IKeyServiceClient keyServiceClient)
        {
            _keyServiceClient = keyServiceClient;
        }
        public async Task DecryptAES(Guid fileId, Stream inputFile, Stream outputFile)
        {
            var aesEncryptionKey = await _keyServiceClient.GetEncryptionKeyAsync(fileId);
            if (aesEncryptionKey.Status != ResultStatus.Success)
            {
                switch (aesEncryptionKey.Status)
                {
                    case ResultStatus.NotFound:
                        throw new NotFoundException($"file id not found: {fileId}");
                    case ResultStatus.AccessDenied:
                        throw new UnauthorizedException("Authorization error while getting encryption key.");
                    case ResultStatus.Failed:
                        throw new Exception("Unexpected error while getting encryption key.");
                }
            }

            if (aesEncryptionKey.KeyData.Key == null || aesEncryptionKey.KeyData.Key.Length == 0)
                throw new ArgumentException("Key cannot be null or empty", nameof(aesEncryptionKey.KeyData.Key));

            if (aesEncryptionKey.KeyData.IV == null || aesEncryptionKey.KeyData.IV.Length == 0)
                throw new ArgumentException("IV cannot be null or empty", nameof(aesEncryptionKey.KeyData.IV));

            if (inputFile == null || inputFile.Length == 0)
                throw new ArgumentException("File cannot be null or empty", nameof(inputFile));

            using (var aes = Aes.Create())
            {
                aes.Key = aesEncryptionKey.KeyData.Key;
                aes.IV = aesEncryptionKey.KeyData.IV;

                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var cryptoStream = new CryptoStream(inputFile, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    await cryptoStream.CopyToAsync(outputFile);
                }
            }
        }
    }
}
