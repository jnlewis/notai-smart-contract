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
        private static readonly string PAYMENT_MAPNAME = "PAYMENT";
        private static readonly string PAYMENTCREATOR_MAPNAME = "PAYMENTCREATOR";

        private static void PaymentStoragePut(UInt160 paymentId, Payment payment) 
        {
            var storageMap = new StorageMap(Storage.CurrentContext, PAYMENT_MAPNAME);
            storageMap.Put(paymentId, StdLib.Serialize(payment));
        }

        private static Payment PaymentStorageGet(UInt160 paymentId) 
        {
            var storageMap = new StorageMap(Storage.CurrentContext, PAYMENT_MAPNAME);
            ByteString value = storageMap.Get(paymentId);
            if (value is null) {
                return new Payment();
            }

            Payment payment = (Payment)StdLib.Deserialize(value);
            return payment;
        }

        private static void PaymentStorageUpdateStatus(UInt160 paymentId, string newStatus) 
        {
            var payment = PaymentStorageGet(paymentId);
            if (payment.PaymentId != null) {
                payment.Status = newStatus;
                PaymentStoragePut(paymentId, payment);
            }
        }
        
        private static void PaymentStorageAddCreatorPayment(UInt160 creator, UInt160 paymentId) 
        {
            List<UInt160> currentPayments = PaymentStorageGetCreatorPayment(creator);
            currentPayments.Add(paymentId);
            
            var storageMap = new StorageMap(Storage.CurrentContext, PAYMENTCREATOR_MAPNAME);
            storageMap.Put(creator, StdLib.Serialize(currentPayments));

        }
        
        private static List<UInt160> PaymentStorageGetCreatorPayment(UInt160 creator) 
        {
            var storageMap = new StorageMap(Storage.CurrentContext, PAYMENTCREATOR_MAPNAME);
            ByteString value = storageMap.Get(creator);
            if (value is null) {
                return new List<UInt160>();
            }

            List<UInt160> creatorPayments = (List<UInt160>)StdLib.Deserialize(value);
            return creatorPayments;
        }
    }
}
