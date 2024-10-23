using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;


namespace EncryptionService.Blockchain
{
    public class BlockchainClient
    {
        private readonly Web3 _web3;
        private readonly string _contractAddress;

        public BlockchainClient(string blockchainUrl, string contractAddress, string privateKey)
        {
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            _web3 = new Web3(account, blockchainUrl);
            _contractAddress = contractAddress;
        }
        public async Task<string> GetPublicKeyFromWalletAsync(string privateKey)
        {
            // Konwertowanie prywatnego klucza z formatu hex na bajty
            var privateKeyBytes = privateKey.HexToByteArray();

            // Ładowanie parametrów krzywej eliptycznej SECP256K1 (to krzywa używana przez MetaMask i Ethereum)
            var ecParams = SecNamedCurves.GetByName("secp256k1");

            // Tworzenie obiektu ECPrivateKeyParameters na podstawie prywatnego klucza
            var privKey = new ECPrivateKeyParameters(new BigInteger(1, privateKeyBytes), new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H));

            // Wyliczanie publicznego klucza (punkt na krzywej) z prywatnego klucza
            var q = ecParams.G.Multiply(privKey.D);

            // Tworzenie obiektu klucza publicznego
            var pubKey = new ECPublicKeyParameters(q, new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H));

            // Konwersja klucza publicznego do postaci bajtowej (format uncompressed, czyli zaczynający się od 04)
            var publicKeyBytes = pubKey.Q.GetEncoded(false); // false oznacza, że zakodujemy cały klucz publiczny, w tym punkt (04)

            // Konwersja klucza publicznego do formatu hex
            var publicKeyHex = BitConverter.ToString(publicKeyBytes).Replace("-", "").ToLower();

            // Zwracanie klucza publicznego w formacie hex
            return publicKeyHex;
        }


        // Metoda konwersji klucza publicznego do formatu XML dla RSA
        public string ConvertPublicKeyToXml(byte[] publicKeyBytes)
        {
            // Najpierw przekształcamy klucz publiczny (który jest w formacie bajtowym) na odpowiednie parametry RSA (Modulus i Exponent)

            // Klucz publiczny RSA powinien mieć długość Modulus (256 bajtów) i Exponent (zazwyczaj 3 lub 65537).
            // Dla uproszczenia tutaj zakładamy typowe wartości dla Exponent i że Modulus jest całym kluczem publicznym.
            string modulus = Convert.ToBase64String(publicKeyBytes.Skip(1).ToArray()); // Pomijamy pierwszy bajt, który określa typ klucza publicznego (04 dla uncompressed)
            string exponent = Convert.ToBase64String(new byte[] { 1, 0, 1 }); // Standardowy Exponent (65537)

            // Format XML dla klucza publicznego
            var xmlFormattedKey = $"<RSAKeyValue><Modulus>{modulus}</Modulus><Exponent>{exponent}</Exponent></RSAKeyValue>";

            return xmlFormattedKey;
        }


        //TODO: NOW! Fix the function
        public async Task<string> SendEncryptedKeyToBlockchainAsync(Guid fileId, byte[] encryptedKey, byte[] iv)
        {
            // Tworzenie instancji funkcji StoreEncryptedKeyFunction
            var storeEncryptedKeyFunction = new StoreEncryptedKeyFunction
            {
                FileId = fileId.ToString(),
                EncryptedKey = encryptedKey, 
                IV = iv
            };

            // Pobieranie aktualnej ceny gazu z sieci Ethereum
            var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();

            // Automatyczne oszacowanie wymaganego gas limit
            var transactionHandler = _web3.Eth.GetContractTransactionHandler<StoreEncryptedKeyFunction>();
            var estimatedGas = await transactionHandler.EstimateGasAsync(_contractAddress, storeEncryptedKeyFunction);

            // Ustawienie transakcji z oszacowanym gas limit i aktualną ceną gazu
            storeEncryptedKeyFunction.Gas = new Nethereum.Hex.HexTypes.HexBigInteger(estimatedGas);
            storeEncryptedKeyFunction.GasPrice = gasPrice;

            // Wysyłanie transakcji i oczekiwanie na potwierdzenie
            var transactionReceipt = await transactionHandler.SendRequestAndWaitForReceiptAsync(_contractAddress, storeEncryptedKeyFunction);

            return transactionReceipt.TransactionHash;
        }


    }

    [Function("storeEncryptionData")]
    public class StoreEncryptedKeyFunction : FunctionMessage
    {
        [Parameter("string", "fileId", 1)]
        public string FileId { get; set; }

        [Parameter("bytes", "encryptedKey", 2)]
        public byte[] EncryptedKey { get; set; }

        [Parameter("bytes", "iv", 2)]
        public byte[] IV { get; set; }
    }

}
