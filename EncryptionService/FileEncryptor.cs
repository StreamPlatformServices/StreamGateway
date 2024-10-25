using EncryptionService.Blockchain;
using KeyServiceAPI;
using KeyServiceAPI.Models;
using Nethereum.Hex.HexConvertors.Extensions;
using Org.BouncyCastle.Asn1.Crmf;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using StreamGatewayCoreUtilities.CommonExceptions;
using System.Security.Cryptography;

namespace EncryptionService
{
    public class FileEncryptor : IFileEncryptor
    {
        private readonly IKeyServiceClient _keyServiceClient;
        private readonly BlockchainClient _blockchainClient;
        private readonly string _privateKey;
        public FileEncryptor(IKeyServiceClient keyServiceClient)
        {
            _keyServiceClient = keyServiceClient;

            string blockchainUrl = "https://sepolia.infura.io/v3/4ecdb1024ecc43ff82b3a8e42fd8c121";
            _privateKey = "9787a84115ba6a122b6e2622d9574834e49c03738df906a06b9f3f823d3c20cc";
            string contractAddress = "0x1A4CEa101398F444Bf1f5A8FAF68FC283917C366";

            _blockchainClient = new BlockchainClient(blockchainUrl, contractAddress, _privateKey);
        }
        public async Task EncryptAES(Guid fileId, Stream inputFile, Stream outputFile)
        {
            (ResultStatus Status, EncryptionKeyModel KeyData) aesEncryptionKey = new ValueTuple<ResultStatus, EncryptionKeyModel>();
            //TOOD:
            var useBlockchain = true;
            if (!useBlockchain)
            {
                aesEncryptionKey = await _keyServiceClient.GetEncryptionKeyAsync(fileId);
            }

            if (useBlockchain)
            {
                using (var aes = Aes.Create()) //TODO: is it CBC??
                {
                    aes.GenerateKey();
                    aes.GenerateIV();

                    aesEncryptionKey = (ResultStatus.Success, new EncryptionKeyModel
                    {
                        Key = aes.Key,
                        IV = aes.IV
                    });
                }

                //var publicKey = await _blockchainClient.GetPublicKeyFromWalletAsync(_privateKey);

               // var encryptedAesKey = EncryptAESWithPublicKey(aesEncryptionKey.KeyData.Key, publicKey);

                var transactionHash = await _blockchainClient.SendEncryptedKeyToBlockchainAsync(fileId, aesEncryptionKey.KeyData.Key, aesEncryptionKey.KeyData.IV);

                if (string.IsNullOrEmpty(transactionHash))
                {
                    throw new Exception("Error sending encryption key to blockchain.");
                }
            }

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

                using (var cryptoStream = new CryptoStream(outputFile, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
                {
                    await inputFile.CopyToAsync(cryptoStream);
                }
            }
        }

        public byte[] EncryptAESWithPublicKey(byte[] aesKey, string publicKeyHex)
        {
            // Zamiana klucza publicznego z formatu hex na bajty
            var publicKeyBytes = Hex.Decode(publicKeyHex);

            // Ładowanie krzywej eliptycznej secp256k1 (używana w ECDSA i MetaMask)
            var ecParams = SecNamedCurves.GetByName("secp256k1");

            // Tworzenie obiektu klucza publicznego z krzywej eliptycznej
            var q = ecParams.Curve.DecodePoint(publicKeyBytes);

            var privateKeyBytes = _privateKey.HexToByteArray();
            var privKey = new ECPrivateKeyParameters(new BigInteger(1, privateKeyBytes), new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H));
            var publicKeyParam = new ECPublicKeyParameters(q, new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H));

            // Inicjalizacja silnika ECIES (Elliptic Curve Integrated Encryption Scheme)
            IesEngine iesEngine = new IesEngine(
                new ECDHBasicAgreement(), // Algorytm Diffie-Hellmana
                new Kdf2BytesGenerator(new Sha256Digest()), // KDF na bazie SHA-256
                new HMac(new Sha256Digest()) // HMAC na bazie SHA-256 dla MAC
            );

            // Tworzenie losowych danych dla wyprowadzania i kodowania
            byte[] derivation = new byte[16];
            byte[] encoding = new byte[16];
            new SecureRandom().NextBytes(derivation);
            new SecureRandom().NextBytes(encoding);

            // Parametry IesParameters: derivation, encoding, rozmiar klucza MAC (256-bitowy)
            IesParameters iesParams = new IesParameters(derivation, encoding, 256);

            // Inicjalizacja silnika ECIES w trybie szyfrowania
            iesEngine.Init(true, privKey, publicKeyParam, iesParams);

            // Szyfrowanie klucza AES
            return iesEngine.ProcessBlock(aesKey, 0, aesKey.Length);
        }



    }
}

//SOLIDITY:

//contract LicenseManager
//{
//    struct License
//{
//    string fileId;
//    bytes encryptedKey;
//}

//mapping(string => License) private licenses;

//event LicenseStored(string fileId, bytes encryptedKey);

//function storeEncryptedKey(string memory fileId, bytes memory encryptedKey) public
//{
//    licenses[fileId] = License(fileId, encryptedKey);

//    emit LicenseStored(fileId, encryptedKey);
//}

//function getEncryptedKey(string memory fileId) public view returns(bytes memory)
//{
//    return licenses[fileId].encryptedKey;
//}
//}
