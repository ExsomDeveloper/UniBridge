using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniBridge
{
    public class DebugAnalyticsSource : IAnalyticsSource
    {
        public bool IsInitialized { get; private set; }

        public void Initialize(UniBridgeAnalyticsConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(DebugAnalyticsSource)}]: Инициализирован (Editor)");
            onSuccess?.Invoke();
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            var paramStr = parameters != null && parameters.Count > 0
                ? " | " + string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"))
                : "";
            Debug.Log($"[Analytics] {eventName}{paramStr}");
        }

        public void ReportPurchase(string productId, decimal price, string currency, string transactionId = null)
        {
            Debug.Log($"[Analytics] ReportPurchase | {productId}, {price} {currency}, tx={transactionId}");
        }
    }
}
