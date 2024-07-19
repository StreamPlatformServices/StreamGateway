namespace EncryptionService
{
    public interface IFileEncryptor
    {
        Task<Stream> EncryptAES(Guid fileId, Stream file);
    }
}
