using UnityEngine;

namespace UniBridge
{
    public class AdSourceBuilder
    {
        public IAdSource Build(UniBridgeConfig config, bool youngMode = true, Transform root = null)
        {
            if (config != null && config.PreferredAdsAdapter == UniBridgeAdapterKeys.None) return null;

            if (root == null)
                root = new GameObject("AdSource").transform;

            var adSource = GetAdSource(config, root);
            if (adSource == null) return null;

            if (youngMode)
                adSource.EnableYoungMode();
            else
                adSource.DisableYoungMode();

            return adSource;
        }

        private IAdSource GetAdSource(UniBridgeConfig config, Transform root = null)
        {
#if UNITY_EDITOR
            // Always use debug adapter in editor
            return CreateDebugAdapter(root);
#else
            // Use preferred registered adapter, or fall back to debug
            var adSource = AdSourceRegistry.Create(config);
            if (adSource != null)
                return adSource;

            Debug.LogWarning("[UniBridge] Preferred ad adapter not available. Using debug adapter.");
            return CreateDebugAdapter(root);
#endif
        }

        private DebugAdSource CreateDebugAdapter(Transform root)
        {
            var debugPrefab = Resources.Load<GameObject>("DebugAdCanvas");
            if (debugPrefab != null && debugPrefab.TryGetComponent(out DebugAdSource debugAdSource))
            {
                return Object.Instantiate(debugAdSource, root);
            }

            Debug.LogWarning($"[{nameof(AdSourceBuilder)}]: DebugAdCanvas prefab not found, creating default debug source.");
            var go = new GameObject("DebugAdSource");
            go.transform.SetParent(root);
            return go.AddComponent<DebugAdSource>();
        }
    }
}
