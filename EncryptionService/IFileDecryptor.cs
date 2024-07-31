namespace EncryptionService
{
    public interface IFileDecryptor
    {
        Task DecryptAES(Guid fileId, Stream inputFile, Stream outputFile);
    }
}
