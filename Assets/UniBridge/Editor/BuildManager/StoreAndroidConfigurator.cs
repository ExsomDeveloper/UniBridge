namespace UniBridge.Editor
{
    public static class StoreAndroidConfigurator
    {
        public static void OnStoreChanged(string previousDefine, string newDefine, bool ratShareEnabled = false)
        {
            if (previousDefine == StorePlatformDefines.STORE_RUSTORE)
                RuStoreAndroidConfigurator.Cleanup();

            if (newDefine == StorePlatformDefines.STORE_RUSTORE)
                RuStoreAndroidConfigurator.Configure();

            bool wasAndroid = IsAndroidStore(previousDefine);
            bool isAndroid  = IsAndroidStore(newDefine);

            if (wasAndroid && !isAndroid)
                UniBridgeShareAndroidConfigurator.Cleanup();
            else if (isAndroid && ratShareEnabled)
                UniBridgeShareAndroidConfigurator.Configure();
            else if (isAndroid && !ratShareEnabled)
                UniBridgeShareAndroidConfigurator.Cleanup();
        }

        private static bool IsAndroidStore(string define) =>
            define == StorePlatformDefines.STORE_GOOGLEPLAY ||
            define == StorePlatformDefines.STORE_RUSTORE;
    }
}
