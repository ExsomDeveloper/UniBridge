using System;
using System.Collections.Generic;

namespace UniBridge
{
    public interface IAnalyticsSource
    {
        bool IsInitialized { get; }

        void Initialize(UniBridgeAnalyticsConfig config, Action onSuccess, Action onFailed);
        void LogEvent(string eventName, Dictionary<string, object> parameters = null);
        void ReportPurchase(string productId, decimal price, string currency, string transactionId = null);
    }
}
