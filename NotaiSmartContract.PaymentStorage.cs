using System;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using Neo;
using Neo.SmartContract.Framework.Native;

namespace NotaiSmartContract
{
    public partial class NotaiSmartContract
    {
        public static readonly string PAYMENT_MAPNAME = "PAYMENT";
        public static readonly string PAYMENTCREATOR_MAPNAME = "PAYMENTCREATOR";

        public static void PaymentStoragePut(UInt160 paymentId, Payment payment) 
        {
            var storageMap = new StorageMap(Storage.CurrentContext, PAYMENT_MAPNAME);
            storageMap.Put(paymentId, StdLib.Serialize(payment));
        }

        public static Payment PaymentStorageGet(UInt160 paymentId) 
        {
            var storageMap = new StorageMap(Storage.CurrentContext, PAYMENT_MAPNAME);
            ByteString value = storageMap.Get(paymentId);
            if (value is null) {
                return new Payment();
            }

            Payment payment = (Payment)StdLib.Deserialize(value);
            return payment;
        }

        public static void PaymentStorageUpdateStatus(UInt160 paymentId, string newStatus) 
        {
            var payment = PaymentStorageGet(paymentId);
            if (payment.PaymentId != null) {
                payment.Status = newStatus;
                PaymentStoragePut(paymentId, payment);
            }
        }
        
        public static void PaymentStorageAddCreatorPayment(UInt160 creator, UInt160 paymentId) 
        {
            List<string> currentPayments = PaymentStorageGetCreatorPayment(creator);
            currentPayments.Add(paymentId);
            
            var storageMap = new StorageMap(Storage.CurrentContext, PAYMENTCREATOR_MAPNAME);
            storageMap.Put(creator, StdLib.Serialize(currentPayments));

        }
        
        public static List<string> PaymentStorageGetCreatorPayment(UInt160 creator) 
        {
            var storageMap = new StorageMap(Storage.CurrentContext, PAYMENTCREATOR_MAPNAME);
            ByteString value = storageMap.Get(creator);
            if (value is null) {
                return new List<string>();
            }

            List<string> creatorPayments = (List<string>)StdLib.Deserialize(value);
            return creatorPayments;
        }
    }
}
