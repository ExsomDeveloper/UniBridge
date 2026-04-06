using UnityEngine;

namespace UniBridge
{
    public class PurchaseSourceBuilder
    {
        public IPurchaseSource Build(UniBridgePurchasesConfig config)
        {
            if (config != null && config.PreferredPurchaseAdapter == "UNIBRIDGE_NONE") return null;

#if UNITY_EDITOR
            return new DebugPurchaseSource();
#else
            if (PurchaseSourceRegistry.HasAny)
            {
                var source = PurchaseSourceRegistry.Create(config);
                if (source != null)
                    return source;

                Debug.LogWarning($"[{nameof(PurchaseSourceBuilder)}]: Preferred purchase adapter not available. Using debug adapter.");
                return new DebugPurchaseSource();
            }

            Debug.LogWarning($"[{nameof(PurchaseSourceBuilder)}]: No platform purchase adapter registered. Using debug adapter.");
            return new DebugPurchaseSource();
#endif
        }
    }
}
