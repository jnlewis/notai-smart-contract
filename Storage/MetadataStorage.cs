using Neo.SmartContract.Framework.Services;
using Neo.SmartContract.Framework;

namespace NotaiSmartContract
{
    public static class MetadataStorage
    {
        public static readonly string METADATA_MAPNAME = "METADATA";

        public static void SetIsDeployed(bool isDeployed) 
        {
            var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
            metadataMap.Put("IsDeployed", isDeployed.ToString());
        }
        public static bool IsDeployed() 
        {
            var metadataMap = new StorageMap(Storage.CurrentContext, METADATA_MAPNAME);
            ByteString isDeployed = metadataMap.Get("IsDeployed");
            if (isDeployed == null)
            {
                return false;
            }
            else
            {
                return isDeployed.ToString() == "true";
            }
        }
    }
}
