using System;
using System.Collections.Generic;

namespace UniBridge
{
    /// <summary>
    /// Registry for purchase source factories. Adapters register themselves at runtime.
    /// This pattern avoids cyclic assembly dependencies.
    /// </summary>
    public static class PurchaseSourceRegistry
    {
        private static readonly Dictionary<string, (Func<UniBridgePurchasesConfig, IPurchaseSource> Factory, int Priority)> _entries
            = new Dictionary<string, (Func<UniBridgePurchasesConfig, IPurchaseSource>, int)>();

        /// <summary>
        /// Register a purchase source factory keyed by the SDK define (e.g. "UNIBRIDGEPURCHASES_IAP").
        /// If a key is registered twice, the one with higher priority wins.
        /// </summary>
        public static void Register(string sdkDefine, Func<UniBridgePurchasesConfig, IPurchaseSource> factory, int priority)
        {
            if (!_entries.TryGetValue(sdkDefine, out var existing) || priority > existing.Priority)
                _entries[sdkDefine] = (factory, priority);
        }

        internal static IPurchaseSource Create(UniBridgePurchasesConfig config)
        {
            var preferred = config?.PreferredPurchaseAdapter;

            if (!string.IsNullOrEmpty(preferred))
            {
                // Preferred adapter explicitly set — use it if installed, else null → Debug fallback
                return _entries.TryGetValue(preferred, out var e) ? e.Factory(config) : null;
            }

            // No preference — fall back to highest-priority registered adapter
            Func<UniBridgePurchasesConfig, IPurchaseSource> bestFactory = null;
            int bestPriority = -1;
            foreach (var entry in _entries.Values)
            {
                if (entry.Priority > bestPriority)
                {
                    bestPriority = entry.Priority;
                    bestFactory  = entry.Factory;
                }
            }
            return bestFactory?.Invoke(config);
        }

        internal static bool HasAny => _entries.Count > 0;
    }
}
