using KeyServiceAPI;
using KeyServiceAPI.Models;
using StreamGatewayCoreUtilities.CommonExceptions;
using System.Net;
using System.Security.Cryptography;

namespace EncryptionService
{
    public class FileEncryptor : IFileEncryptor
    {
        private readonly IKeyServiceClient _keyServiceClient;
        public FileEncryptor(IKeyServiceClient keyServiceClient)
        {
            _keyServiceClient = keyServiceClient;
        }
        public async Task<Stream> EncryptAES(Guid fileId, Stream file) //TODO: create exception logic in KeyServiceClient!!
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

            if (file == null || file.Length == 0)
                throw new ArgumentException("File cannot be null or empty", nameof(file));

            using (var aes = Aes.Create())
            {
                aes.Key = aesEncryptionKey.KeyData.Key;
                aes.IV = aesEncryptionKey.KeyData.IV;

                var encryptedStream = new MemoryStream();
                using (var cryptoStream = new CryptoStream(encryptedStream, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
                {
                    await file.CopyToAsync(cryptoStream);
                }

                encryptedStream.Position = 0;

                return encryptedStream;
            }
        }
        
    }
}
