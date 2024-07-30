namespace EncryptionService
{
    public interface IFileEncryptor
    {
        Task EncryptAES(Guid fileId, Stream inputFile, Stream outputFile);
    }
}
