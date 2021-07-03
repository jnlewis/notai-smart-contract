using System.Numerics;
using Neo;

namespace NotaiSmartContract
{
    public struct Payment
    {
        public UInt160 PaymentId;
        public UInt160 CreatorAddress;
        public UInt160 RecipientAddress;
        public string Title;
        public string Asset;
        public BigInteger Amount;
        public ulong Expiry;
        public string Status;
        public string ConditionApi;
        public string ConditionField;
        public string ConditionFieldType;
        public string ConditionOperator;
        public string ConditionValue;
    }
}

