using System;
using System.ComponentModel;
using System.Numerics;
using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using Neo.SmartContract.Framework.Native;

namespace NotaiSmartContract
{
    [DisplayName("NotaiSmartContract")]
    [ManifestExtra("Author", "Jeffrey Lewis")]
    [ManifestExtra("Email", "jeffreynlewis@outlook.com")]
    [ManifestExtra("Description", "The NOTAI Smart Contract to facilitate payment management.")]
    [ContractPermission("*")]
    public partial class NotaiSmartContract : SmartContract
    {
        static readonly ulong DefaultServiceFee = 50000000; // 0.5 GAS

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

        public static UInt160 Owner() => MetadataStorageGetOwner();

        public static void _deploy(object data, bool update)
        {
            if (update) return;

            // Set the account used to deploy this contract as owner
            var tx = (Transaction) Runtime.ScriptContainer;
            var owner = tx.Sender;

            // Check if contract is already deployed
            if (MetadataStorageGetOwner() != UInt160.Zero) {
                throw new Exception("_deploy: Contract has already been deployed.");
            }

            MetadataStorageSetOwner(owner);
            MetadataStorageSetServiceFee(DefaultServiceFee);

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
            if (!IsOwner()) throw new Exception("Destroy: No authorization.");
            ContractManagement.Destroy();
        }

        public static void UpdateServiceFee(BigInteger serviceFeeGas)
        {
            if (!IsOwner()) throw new Exception("UpdateServiceFee: No authorization.");
            MetadataStorageSetServiceFee(serviceFeeGas);
        }

        public static BigInteger GetServiceFee()
        {
            return MetadataStorageGetServiceFee();
        }

        #endregion

        #region Payments

        public static Payment GetPayment(UInt160 paymentId)
        {
            return PaymentStorageGet(paymentId);
        }

        public static List<Payment> GetPaymentsByCreator(UInt160 creatorAddress)
        {
            List<Payment> payments= new List<Payment>();

            var paymentIds = GetPaymentIdsByCreator(creatorAddress);
            foreach (UInt160 paymentId in paymentIds) {
                payments.Add(PaymentStorageGet(paymentId));
            }

            return payments;
        }
        
        public static List<UInt160> GetPaymentIdsByCreator(UInt160 creatorAddress)
        {
            return PaymentStorageGetCreatorPayment(creatorAddress);
        }

        public static void CreatePayment(
            UInt160 paymentId,
            UInt160 creatorAddress,
            UInt160 recipientAddress,
            string paymentIdString,
            string creatorAddressString,
            string recipientAddressString,
            string title,
            string asset,
            BigInteger amount,
            ulong expiry,
            string status,
            string conditionApi,
            string conditionField,
            string conditionFieldType,
            string conditionOperator,
            string conditionValue)
        {
            // Verify that invoker is the creator
            if (!Runtime.CheckWitness(creatorAddress)) 
            {
                throw new Exception("CreatePayment: Invoker must be payment creator.");
            }

            // Check if payment already exist
            if (PaymentStorageGet(paymentId).PaymentId != null) {
                throw new Exception("CreatePayment: Payment already exist.");
            }

            // Transfer from sender to escrow (contract)
            Runtime.Log("CreatePayment: Transfering assets.");

            // TODO: Check sender balance

            // Service Fee payment
            bool isFeeTransferred = GAS.Transfer(creatorAddress, MetadataStorageGetOwner(), DefaultServiceFee);
            if (!isFeeTransferred) {
                Runtime.Log("CreatePayment: Failed to transfer fee from payment creator to owner.");
            }
            
            // NEP-17 Asset transfer to escrow
            UInt160 assetHash =  GetAssetHash(asset);
            bool isTransferred = (bool)Contract.Call(assetHash, "transfer", CallFlags.All, new object[] { creatorAddress, Runtime.ExecutingScriptHash, amount, 0 });
            if (!isTransferred) {
                // throw new Exception("Failed to transfer from payment creator to escrow.");
                Runtime.Log("CreatePayment: Failed to transfer from payment creator to escrow.");
            }
            
            // Add payment
            Runtime.Log("CreatePayment: Saving payment.");
            Payment payment = new Payment()
            {
                PaymentId = paymentId,
                CreatorAddress = creatorAddress,
                RecipientAddress = recipientAddress,
                PaymentIdString = paymentIdString,
                CreatorAddressString = creatorAddressString,
                RecipientAddressString = recipientAddressString,
                Title = title,
                Asset = asset,
                Amount = amount,
                Expiry = expiry,
                Status = status,
                ConditionApi = conditionApi,
                ConditionField = conditionField,
                ConditionFieldType = conditionFieldType,
                ConditionOperator = conditionOperator,
                ConditionValue = conditionValue
            };
            PaymentStoragePut(paymentId, payment);
            PaymentStorageAddCreatorPayment(payment.CreatorAddress, paymentId);

            // Fire event
            OnPaymentCreated(paymentId);
        }

        public static void CancelPayment(UInt160 paymentId)
        {
            // Get payment
            var payment = PaymentStorageGet(paymentId);
            if (payment.PaymentId == null) 
            {
                throw new Exception("CancelPayment: Payment not found.");
            }

            // Verification
            if (payment.Status != "open") 
            {
                throw new Exception("CancelPayment: Payment status is not open.");
            }
            if (Runtime.Time < payment.Expiry)
            {
                throw new Exception("CancelPayment: Payment cannot be cancelled as it is not yet expired.");
            }

            // Verify that invoker is the creator
            if (!Runtime.CheckWitness(payment.CreatorAddress)) 
            {
                throw new Exception("CancelPayment: Invoker must be payment creator.");
            }

            BigInteger amountPlusFee = payment.Amount + MetadataStorageGetServiceFee();

            // Transfer from escrow (contract) to sender
            Runtime.Log("CancelPayment: Transfering assets.");
            UInt160 assetHash =  GetAssetHash(payment.Asset);
            bool isTransferred = (bool)Contract.Call(assetHash, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, payment.CreatorAddress, amountPlusFee, 0 });
            if (!isTransferred) {
                // throw new Exception("CancelPayment: Failed to transfer from escrow to payment creator.");
                Runtime.Log("CancelPayment: Failed to transfer from escrow to payment creator.");
            }

            // Update payment status
            PaymentStorageUpdateStatus(paymentId, "cancelled");
            
            // Fire event
            OnPaymentCancelled(paymentId);
        }

        public static void ReleasePayment(UInt160 paymentId)
        {
            // Get payment
            var payment = PaymentStorageGet(paymentId);
            if (payment.PaymentId == null) 
            {
                throw new Exception("ReleasePayment: Payment not found.");
            }

            // Verification
            if (payment.Status != "open") 
            {
                throw new Exception("ReleasePayment: Payment status is not open.");
            }
            if (Runtime.Time >= payment.Expiry)
            {
                throw new Exception("ReleasePayment: Payment cannot be released as it has already expired.");
            }

            PaymentStorageUpdateStatus(payment.PaymentId, "verification");

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
                throw new Exception("OracleCallback: Oracle response failure with code " + (byte)code);
            }

            // Check condition match
            object queryData = StdLib.JsonDeserialize(result);
            if (queryData == null) {
                Runtime.Log("OracleCallback: Query result is null.");
                return;
            }
            object[] queryArray = (object[])queryData;
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
            if (payment.ConditionFieldType == "datetime") 
            {
                if (payment.ConditionOperator == "=") {
                    isConditionMet = StringToUnixTime(queryValue) == StringToUnixTime(payment.ConditionValue);
                }
                if (payment.ConditionOperator == ">") {
                    isConditionMet = StringToUnixTime(queryValue) > StringToUnixTime(payment.ConditionValue);
                }
                if (payment.ConditionOperator == ">=") {
                    isConditionMet = StringToUnixTime(queryValue) >= StringToUnixTime(payment.ConditionValue);
                }
                if (payment.ConditionOperator == "<") {
                    isConditionMet = StringToUnixTime(queryValue) < StringToUnixTime(payment.ConditionValue);
                }
                if (payment.ConditionOperator == "<=") {
                    isConditionMet = StringToUnixTime(queryValue) <= StringToUnixTime(payment.ConditionValue);
                }
            }

            if (isConditionMet) {
                Runtime.Log("OracleCallback: Condition met, releasing payment.");

                // Transfer from escrow (contract) to recipient
                UInt160 assetHash =  GetAssetHash(payment.Asset);
                bool isTransferred = (bool)Contract.Call(assetHash, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, payment.RecipientAddress, payment.Amount, 0 });
                if (!isTransferred) 
                {
                    // throw new Exception("Failed to transfer from escrow to payment recipient.");
                    Runtime.Log("OracleCallback: Failed to transfer from escrow to payment recipient.");
                }

                // Update payment status
                PaymentStorageUpdateStatus(payment.PaymentId, "released");
                
                // Fire event
                OnPaymentReleased(payment.PaymentId);
                Runtime.Log("OracleCallback: Fired notification OnPaymentReleased.");
            }
            else
            {
                PaymentStorageUpdateStatus(payment.PaymentId, "open");
                Runtime.Log("OracleCallback: Condition not met.");
            }
        }

        private static UInt160 GetAssetHash(string assetName) 
        {
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

        private static long StringToUnixTime(string value) 
        {
            throw new Exception("Datetime type is currently not supported.");
            // TODO:
            // try
            // {
            //     DateTime date = DateTime.Parse(value);
            //     long unixTime = ((DateTimeOffset)date).ToUnixTimeSeconds();
            //     return unixTime;
            // }
            // catch
            // {
            //     return 0;
            // }
        }

        #endregion
    
    }
}
