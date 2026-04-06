using System.Collections.Generic;

namespace UniBridge.Editor
{
    public static class ChecklistRegistry
    {
        private static readonly Dictionary<string, IChecklistProvider> _storeChecklists =
            new Dictionary<string, IChecklistProvider>
            {
                { StorePlatformDefines.STORE_GOOGLEPLAY, new UniBridgeShareChecklist() },
                { StorePlatformDefines.STORE_RUSTORE,    new UniBridgeShareChecklist() },
            };

        private static readonly Dictionary<string, IChecklistProvider> _sdkChecklists =
            new Dictionary<string, IChecklistProvider>
            {
                { "UNIBRIDGEPURCHASES_RUSTORE",       new RuStoreBillingChecklist() },
                { "UNIBRIDGE_PLAYGAMA",            new PlaygamaChecklist() },
                { "UNIBRIDGEANALYTICS_APPMETRICA",    new AppMetricaChecklist() },
            };

        public static IChecklistProvider GetStoreChecklist(string define)
        {
            if (define == null) return null;
            _storeChecklists.TryGetValue(define, out var provider);
            return provider;
        }

        public static IChecklistProvider GetSdkChecklist(string define)
        {
            if (define == null) return null;
            _sdkChecklists.TryGetValue(define, out var provider);
            return provider;
        }
    }
}
