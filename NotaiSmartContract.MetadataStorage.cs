using System;
using System.Numerics;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using Neo;

namespace NotaiSmartContract
{
    public partial class NotaiSmartContract
    {
        private static readonly string METADATA_MAPNAME = "METADATA";

        private static void MetadataStorageSetOwner(UInt160 address) 
        {
            var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
            metadataMap.Put("Owner", address);
        }

        private static UInt160 MetadataStorageGetOwner() 
        {
            var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
            ByteString owner = metadataMap.Get("Owner");
            if (owner == null)
            {
                return UInt160.Zero;
            }
            else
            {
                return (UInt160) owner;
            }
        }
        
        private static void MetadataStorageSetServiceFee(BigInteger serviceFeeGas) 
        {
            var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
            metadataMap.Put("ServiceFee", serviceFeeGas);
        }

        private static BigInteger MetadataStorageGetServiceFee() 
        {
            var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
            ByteString serviceFee = metadataMap.Get("ServiceFee");
            if (serviceFee == null)
            {
                return 0;
            }
            else
            {
                return (BigInteger)serviceFee;
            }
        }
    }
}
