using Neo.SmartContract.Framework.Services;
using Neo.SmartContract.Framework;
using Neo;
using System.Numerics;

namespace NotaiSmartContract
{
    public static class MetadataStorage
    {
        // public static readonly string METADATA_MAPNAME = "METADATA";

        // public static void SetIsDeployed(bool isDeployed) 
        // {
        //     var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
        //     metadataMap.Put("IsDeployed", isDeployed.ToString());
        // }
        // public static bool IsDeployed() 
        // {
        //     var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
        //     ByteString isDeployed = metadataMap.Get("IsDeployed");
        //     if (isDeployed == null)
        //     {
        //         return false;
        //     }
        //     else
        //     {
        //         return isDeployed.ToString() == "true";
        //     }
        // }

        // public static void SetOwner(UInt160 address) 
        // {
        //     var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
        //     metadataMap.Put("Owner", address);
        // }
        // public static UInt160 GetOwner() 
        // {
        //     var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
        //     ByteString owner = metadataMap.Get("Owner");
        //     if (owner == null)
        //     {
        //         return UInt160.Zero;
        //     }
        //     else
        //     {
        //         return (UInt160) owner;
        //     }
        // }
    }
}
