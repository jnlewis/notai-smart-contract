using Neo.SmartContract.Framework.Services;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;

namespace NotaiSmartContract
{
    public static class PaymentStorage
    {
        // public static readonly string PAYMENT_MAPNAME = "PAYMENT";

        // public static void Put(UInt160 paymentId, Payment payment) 
        // {
        //     var storageMap = new StorageMap(Storage.CurrentContext, PAYMENT_MAPNAME);
        //     ByteString paymentKey = Helper.ToByteString(Helper.ToByteArray(paymentId));
        //     storageMap.Put(paymentKey, StdLib.Serialize(payment));
        // }

        // public static Payment Get(UInt160 paymentId) 
        // {
        //     var storageMap = new StorageMap(Storage.CurrentContext, PAYMENT_MAPNAME);
        //     ByteString paymentKey = Helper.ToByteString(Helper.ToByteArray(paymentId));
        //     ByteString paymentValue = storageMap.Get(paymentKey);
        //     if (paymentId is null) {
        //         return new Payment();
        //     }

        //     Payment payment = (Payment)StdLib.Deserialize(paymentValue);
        //     return payment;
        // }

        // public static void UpdateStatus(UInt160 paymentId, string newStatus) 
        // {
        //     var payment = Get(paymentId);
        //     if (payment.PaymentId != null) {
        //         payment.Status = newStatus;
        //         Put(paymentId, payment);
        //     }
        // }
    }
}
