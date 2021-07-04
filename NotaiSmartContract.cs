using System;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using Neo;
using Neo.SmartContract.Framework.Native;

namespace NotaiSmartContract
{
    [DisplayName("NotaiBetaSmartContract")]
    [ManifestExtra("Author", "Jeffrey Lewis")]
    [ManifestExtra("Email", "jeffreynlewis@outlook.com")]
    [ManifestExtra("Description", "The NOTAI Smart Contract to facilitate payment management.")]
    [ContractPermission("*")]
    public partial class NotaiSmartContract : SmartContract
    {
        #region Notifications

        [DisplayName("PaymentCreated")]
        public static event Action<UInt160> OnPaymentCreated;    // <paymentId>

        [DisplayName("PaymentReleased")]
        public static event Action<UInt160> OnPaymentReleased;    // <paymentId>

        [DisplayName("PaymentCancelled")]
        public static event Action<UInt160> OnPaymentCancelled;    // <paymentId>

        #endregion

        // When this contract address is included in the transaction signature,
        // this method will be triggered as a VerificationTrigger to verify that the signature is correct.
        // For example, this method needs to be called when withdrawing token from the contract.
        public static bool Verify() => IsOwner();

        private static bool IsOwner() => Runtime.CheckWitness(MetadataStorageGetOwner());

        public static string Owner() => MetadataStorageGetOwner().ToString();

        public static bool Main()
        {
            return true;
        }

        public static void _deploy(object data, bool update)
        {
            if (update) return;

            // Set the account used to deploy this contract as owner
            var tx = (Transaction) Runtime.ScriptContainer;
            var owner = tx.Sender;

            // Check if contract is already deployed
            if (MetadataStorageGetOwner() != UInt160.Zero) {
                throw new Exception("Contract has already been deployed.");
            }

            MetadataStorageSetOwner(owner);

            Runtime.Log("_deploy: Completed");
        }

        #region Owner

        public static void Update(ByteString nefFile, string manifest, object data)
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Update(nefFile, manifest, data);
        }

        public static void Destroy()
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Destroy();
        }

        #endregion
    
        #region Payments

        public static Payment GetPayment(UInt160 paymentId)
        {
            Runtime.Log("GetPayment: Begin");
            return PaymentStorageGet(paymentId);
        }
        
        public static List<string> GetPaymentIdsByCreator(UInt160 creatorAddress)
        {
            Runtime.Log("GetPaymentIdsByCreator: Begin");
            return PaymentStorageGetCreatorPayment(creatorAddress);
        }

        public static void CreatePayment(UInt160 paymentId, Payment payment)
        {
            Runtime.Log("CreatePayment: Begin");

            // TODO: verify that invoker is the creator (or verify creatorAddress is signed?)
            // if (!Runtime.CheckWitness(payment.CreatorAddress)) 
            // {
            //     throw new Exception("Check your signature.");
            // }

            // Check if payment already exist
            if (PaymentStorageGet(paymentId).PaymentId == null) {
                throw new Exception("Payment already exist.");
            }

            // Transfer from sender to escrow (contract)
            UInt160 assetHash =  GetAssetHash(payment.Asset);
            bool isTransferred = (bool)Contract.Call(assetHash, "transfer", CallFlags.All, new object[] { payment.CreatorAddress, Runtime.ExecutingScriptHash, payment.Amount, 0 });
            if (!isTransferred) {
                throw new Exception("Failed to transfer from payment creator to escrow.");
            }

            // Add payment
            PaymentStoragePut(paymentId, payment);
            PaymentStorageAddCreatorPayment(payment.CreatorAddress, paymentId);

            // Fire event
            OnPaymentCreated(paymentId);
            Runtime.Log("CreatePayment: Fired notification OnPaymentCreated.");
        }

        public static void CancelPayment(UInt160 paymentId)
        {
            Runtime.Log("CancelPayment: Begin");

            // Get payment
            var payment = PaymentStorageGet(paymentId);
            if (payment.PaymentId == null) 
            {
                throw new Exception("Payment not found.");
            }

            // Verification
            if (payment.Status != "open") 
            {
                throw new Exception("Payment status is not open.");
            }
            if (Runtime.Time < payment.Expiry)
            {
                throw new Exception("Payment cannot be cancelled as it is not yet expired.");
            }
            // TODO: verify that invoker is the creator

            // Transfer from escrow (contract) to sender
            UInt160 assetHash =  GetAssetHash(payment.Asset);
            bool isTransferred = (bool)Contract.Call(assetHash, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, payment.CreatorAddress, payment.Amount, 0 });
            if (!isTransferred) {
                throw new Exception("Failed to transfer from escrow to payment creator.");
            }

            // Update payment status
            PaymentStorageUpdateStatus(paymentId, "cancelled");
            
            // Fire event
            OnPaymentCancelled(paymentId);
            Runtime.Log("CancelPayment: Fired notification OnPaymentCancelled.");
        }

        public static void ReleasePayment(UInt160 paymentId)
        {
            Runtime.Log("ReleasePayment: Begin");

            // Get payment
            var payment = PaymentStorageGet(paymentId);
            if (payment.PaymentId == null) 
            {
                throw new Exception("Payment not found.");
            }

            // Verification
            if (payment.Status != "open") 
            {
                throw new Exception("Payment status is not open.");
            }
            if (Runtime.Time >= payment.Expiry)
            {
                throw new Exception("Payment cannot be released as it has already expired.");
            }

            // Verify payment condition
            Oracle.Request(
                payment.ConditionApi, 
                payment.ConditionField, 
                "callback", 
                payment, 
                Oracle.MinimumResponseFee
            );
        }
        
        public static void Callback(string url, Payment payment, OracleResponseCode code, string result)
        {
            Runtime.Log("OracleCallback: Begin");

            // if (ExecutionEngine.CallingScriptHash != Oracle.Hash) 
            // {
            //     throw new Exception("Unauthorized!");
            // }

            if (code != OracleResponseCode.Success) 
            {
                throw new Exception("Oracle response failure with code " + (byte)code);
            }

            // Check condition match
            object queryData = StdLib.JsonDeserialize(result); // [ "hello world" ]
            if (queryData == null) {
                Runtime.Log("OracleCallback: Query result is null.");
                return;
            }
            object[] queryArray = (object[])queryData;
            // object[] queryArray = queryData as object[];
            // if (queryArray == null || queryArray.Length == 0) {
            //     Runtime.Log("OracleCallback: Query result is empty.");
            //     return;
            // }

            string queryValue = (string)queryArray[0];

            Runtime.Log("OracleCallback: Query value: " + queryValue);

            bool isConditionMet = false;

            if (payment.ConditionFieldType == "text") {
                if (payment.ConditionOperator == "=") {
                    isConditionMet = (queryValue == payment.ConditionValue);
                }
            }

            if (payment.ConditionFieldType == "number" && IsNumeric(queryValue) && IsNumeric(payment.ConditionValue)) 
            {
                if (payment.ConditionOperator == "=") {
                    isConditionMet = StringToNumber(queryValue) == StringToNumber(payment.ConditionValue);
                }
                if (payment.ConditionOperator == ">") {
                    isConditionMet = StringToNumber(queryValue) > StringToNumber(payment.ConditionValue);
                }
                if (payment.ConditionOperator == ">=") {
                    isConditionMet = StringToNumber(queryValue) >= StringToNumber(payment.ConditionValue);
                }
                if (payment.ConditionOperator == "<") {
                    isConditionMet = StringToNumber(queryValue) < StringToNumber(payment.ConditionValue);
                }
                if (payment.ConditionOperator == "<=") {
                    isConditionMet = StringToNumber(queryValue) <= StringToNumber(payment.ConditionValue);
                }
            }
            // TODO: Add support for DateTime type

            if (isConditionMet) {
                Runtime.Log("OracleCallback: Condition met, releasing payment.");

                // Transfer from escrow (contract) to recipient
                UInt160 assetHash =  GetAssetHash(payment.Asset);
                bool isTransferred = (bool)Contract.Call(assetHash, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, payment.RecipientAddress, payment.Amount, 0 });
                if (!isTransferred) 
                {
                    throw new Exception("Failed to transfer from escrow to payment recipient.");
                }

                // Update payment status
                PaymentStorageUpdateStatus(payment.PaymentId, "released");
                
                // Fire event
                OnPaymentReleased(payment.PaymentId);
                Runtime.Log("OracleCallback: Fired notification OnPaymentReleased.");
            }
            else
            {
                Runtime.Log("OracleCallback: Condition not met.");
            }
        }

        private static UInt160 GetAssetHash(string assetName) {
            if (assetName == "NEO") {
                return NEO.Hash;
            }
            if (assetName == "GAS") {
                return GAS.Hash;
            }

            Runtime.Log("GetAssetHash: Asset not supported");
            throw new Exception("Asset not supported.");
        }

        private static bool IsNumeric(string value)
        {
            try
            {
                long result = long.Parse(value);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private static long StringToNumber(string value) 
        {
            return long.Parse(value);
        }

        #endregion
    
    }
}
