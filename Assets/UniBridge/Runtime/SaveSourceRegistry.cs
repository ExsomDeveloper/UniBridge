using System;
using System.Collections.Generic;

namespace UniBridge
{
    public static class SaveSourceRegistry
    {
        private static readonly Dictionary<string, (Func<ISaveSource> factory, int priority)> _adapters = new();

        public static void Register(string key, Func<ISaveSource> factory, int priority)
        {
            if (!_adapters.TryGetValue(key, out var existing) || priority > existing.priority)
                _adapters[key] = (factory, priority);
        }

        internal static ISaveSource Create(string preferredKey)
        {
            if (!string.IsNullOrEmpty(preferredKey) && _adapters.TryGetValue(preferredKey, out var preferred))
                return preferred.factory();

            return CreateHighestPriority();
        }

        internal static ISaveSource CreateHighestPriority()
        {
            Func<ISaveSource> best = null;
            int bestPriority = int.MinValue;

            foreach (var kvp in _adapters)
            {
                if (kvp.Value.priority > bestPriority)
                {
                    bestPriority = kvp.Value.priority;
                    best = kvp.Value.factory;
                }
            }

            return best?.Invoke();
        }

        internal static bool HasFactory(string key) =>
            !string.IsNullOrEmpty(key) && _adapters.ContainsKey(key);

        internal static bool HasAnyFactory => _adapters.Count > 0;
    }
}
