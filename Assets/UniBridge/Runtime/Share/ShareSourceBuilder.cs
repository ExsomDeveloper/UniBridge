using UnityEngine;

namespace UniBridge
{
    public class ShareSourceBuilder
    {
        public IShareSource Build(UniBridgeShareConfig config)
        {
#if UNITY_EDITOR
            return new DebugShareSource();
#else
            if (config != null && config.PreferredShareAdapter == "UNIBRIDGE_NONE")
                return null;

            if (config != null && config.PreferredShareAdapter == "UNIBRIDGESHARE_MOCK")
            {
                Debug.Log($"[{nameof(ShareSourceBuilder)}]: Принудительно выбрана MockShareSource.");
                return new MockShareSource();
            }

#if UNITY_ANDROID
            if (config == null || string.IsNullOrEmpty(config.PreferredShareAdapter) ||
                config.PreferredShareAdapter == "UNIBRIDGESHARE_ANDROID")
            {
                Debug.Log($"[{nameof(ShareSourceBuilder)}]: Выбрана AndroidShareSource.");
                return new AndroidShareSource();
            }
#endif

#if UNITY_IOS
            if (config == null || string.IsNullOrEmpty(config.PreferredShareAdapter) ||
                config.PreferredShareAdapter == "UNIBRIDGESHARE_IOS")
            {
                Debug.Log($"[{nameof(ShareSourceBuilder)}]: Выбрана iOSShareSource.");
                return new iOSShareSource();
            }
#endif

            var source = ShareSourceRegistry.Create(config);
            if (source != null)
                return source;

            Debug.LogWarning(
                $"[{nameof(ShareSourceBuilder)}]: " +
                "Нет подходящего адаптера шаринга. Используется MockShareSource.");

            return new MockShareSource();
#endif
        }
    }
}
