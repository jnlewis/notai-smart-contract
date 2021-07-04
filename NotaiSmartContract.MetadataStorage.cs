using System;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
using Neo;

namespace NotaiSmartContract
{
    public partial class NotaiSmartContract
    {
        public static readonly string METADATA_MAPNAME = "METADATA";

        public static void MetadataStorageSetOwner(UInt160 address) 
        {
            var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
            metadataMap.Put("Owner", address);
        }

        public static UInt160 MetadataStorageGetOwner() 
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
    }
}
