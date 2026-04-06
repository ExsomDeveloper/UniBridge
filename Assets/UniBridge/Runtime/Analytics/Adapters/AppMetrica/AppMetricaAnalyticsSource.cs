#if UNIBRIDGEANALYTICS_APPMETRICA
using System;
using System.Collections.Generic;
using System.Linq;
using Io.AppMetrica;
using UnityEngine;

namespace UniBridge
{
    public class AppMetricaAnalyticsSource : IAnalyticsSource
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterAdapter()
            => AnalyticsSourceRegistry.Register("UNIBRIDGEANALYTICS_APPMETRICA",
                   config => new AppMetricaAnalyticsSource(config), 100);

        private readonly UniBridgeAnalyticsConfig _config;
        public bool IsInitialized { get; private set; }

        public AppMetricaAnalyticsSource(UniBridgeAnalyticsConfig config) => _config = config;

        public void Initialize(UniBridgeAnalyticsConfig config, Action onSuccess, Action onFailed)
        {
#if UNIBRIDGEANALYTICS_VERBOSE_LOG
            VLog($"Initialize: apiKey={_config.AppMetrica.ApiKey}");
#endif
            var cfg = new AppMetricaConfig(_config.AppMetrica.ApiKey);
            AppMetrica.Activate(cfg);
            IsInitialized = true;
#if UNIBRIDGEANALYTICS_VERBOSE_LOG
            VLog("Initialize: activated");
#endif
            onSuccess?.Invoke();
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
#if UNIBRIDGEANALYTICS_VERBOSE_LOG
            if (parameters == null || parameters.Count == 0)
                VLog($"LogEvent: '{eventName}'");
            else
                VLog($"LogEvent: '{eventName}' params=[{string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"))}]");
#endif
            if (parameters == null || parameters.Count == 0)
            {
                AppMetrica.ReportEvent(eventName);
            }
            else
            {
                var json = "{" + string.Join(",", parameters.Select(kv => $"\"{kv.Key}\":\"{kv.Value}\"")) + "}";
                AppMetrica.ReportEvent(eventName, json);
            }
        }

        public void ReportPurchase(string productId, decimal price, string currency, string transactionId = null)
        {
#if UNIBRIDGEANALYTICS_VERBOSE_LOG
            VLog($"ReportPurchase: productId='{productId}' price={price} {currency} txId={transactionId}");
#endif
            var revenue = new Revenue((long)(price * 1_000_000), currency)
            {
                ProductID = productId,
            };
            if (!string.IsNullOrEmpty(transactionId))
                revenue.Payload = $"{{\"OrderID\":\"{transactionId}\"}}";
            AppMetrica.ReportRevenue(revenue);
        }

#if UNIBRIDGEANALYTICS_VERBOSE_LOG
        private static void VLog(string msg) => Debug.Log($"[RAT] [{nameof(AppMetricaAnalyticsSource)}] {msg}");
#endif
    }
}
#endif
