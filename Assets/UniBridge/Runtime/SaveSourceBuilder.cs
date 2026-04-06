using UnityEngine;

namespace UniBridge
{
    public static class SaveSourceBuilder
    {
        public static ISaveSource Build(UniBridgeSavesConfig config)
        {
            string preferred = config?.PreferredSavesAdapter;

#if UNITY_EDITOR
            if (preferred == "UNIBRIDGESAVES_SIMULATED")
                return new SimulatedSaveSource();

            return new LocalSaveSource();
#else
            if (preferred == "UNIBRIDGESAVES_SIMULATED")
                return new SimulatedSaveSource();

#if UNITY_IOS && UNIBRIDGE_STORE_APPSTORE
            if (preferred == "UNITY_IOS_ICLOUD")
                return new iCloudSaveSource();
#endif

            if (preferred == "UNIBRIDGE_NONE" || preferred == "")
                return new LocalSaveSource();

            if (!string.IsNullOrEmpty(preferred) && SaveSourceRegistry.HasFactory(preferred))
                return SaveSourceRegistry.Create(preferred);

            if (SaveSourceRegistry.HasAnyFactory)
                return SaveSourceRegistry.CreateHighestPriority();

            Debug.LogWarning("[UniBridgeSaves] No platform save adapter registered. Using local save source.");
            return new LocalSaveSource();
#endif
        }
    }
}
