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
            var privateKeyBytes = privateKey.HexToByteArray();

            var ecParams = SecNamedCurves.GetByName("secp256k1");

            var privKey = new ECPrivateKeyParameters(new BigInteger(1, privateKeyBytes), new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H));

            var q = ecParams.G.Multiply(privKey.D);

            var pubKey = new ECPublicKeyParameters(q, new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H));

            var publicKeyBytes = pubKey.Q.GetEncoded(false);

            var publicKeyHex = BitConverter.ToString(publicKeyBytes).Replace("-", "").ToLower();

            return publicKeyHex;
        }


        public string ConvertPublicKeyToXml(byte[] publicKeyBytes)
        {
            string modulus = Convert.ToBase64String(publicKeyBytes.Skip(1).ToArray()); 
            string exponent = Convert.ToBase64String(new byte[] { 1, 0, 1 }); 

            var xmlFormattedKey = $"<RSAKeyValue><Modulus>{modulus}</Modulus><Exponent>{exponent}</Exponent></RSAKeyValue>";

            return xmlFormattedKey;
        }

        public async Task<string> SendEncryptedKeyToBlockchainAsync(Guid fileId, byte[] encryptedKey, byte[] iv)
        {
            var storeEncryptedKeyFunction = new StoreEncryptedKeyFunction
            {
                FileId = fileId.ToString(),
                EncryptedKey = encryptedKey, 
                IV = iv
            };

            var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();

            var transactionHandler = _web3.Eth.GetContractTransactionHandler<StoreEncryptedKeyFunction>();
            var estimatedGas = await transactionHandler.EstimateGasAsync(_contractAddress, storeEncryptedKeyFunction);

            storeEncryptedKeyFunction.Gas = new Nethereum.Hex.HexTypes.HexBigInteger(estimatedGas);
            storeEncryptedKeyFunction.GasPrice = gasPrice;

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
