using UnityEngine;

namespace UniBridge
{
    public class RateSourceBuilder
    {
        public IRateSource Build(UniBridgeRateConfig config)
        {
            if (config != null && config.PreferredRateAdapter == UniBridgeAdapterKeys.None) return null;

#if UNITY_EDITOR
            return new DebugRateSource();
#else
            if (config != null && config.PreferredRateAdapter == "UNIBRIDGERATE_MOCK")
            {
                Debug.Log($"[{nameof(RateSourceBuilder)}]: Принудительно выбрана MockRateSource.");
                return new MockRateSource();
            }

#if UNITY_IOS && UNIBRIDGE_STORE_APPSTORE
            if (config != null && config.PreferredRateAdapter == "UNITY_IOS_STOREREVIEW")
            {
                Debug.Log($"[{nameof(RateSourceBuilder)}]: Принудительно выбрана AppStoreReviewSource.");
                return new AppStoreReviewSource();
            }
#endif

            var source = RateSourceRegistry.Create(config);
            if (source != null)
                return source;

            Debug.LogWarning(
                $"[{nameof(RateSourceBuilder)}]: " +
                "Нет подходящего адаптера оценки. Используется MockRateSource.");

            return new MockRateSource();
#endif
        }
    }
}
