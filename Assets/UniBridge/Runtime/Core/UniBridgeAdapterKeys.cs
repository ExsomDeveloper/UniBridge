namespace UniBridge
{
    public static class UniBridgeAdapterKeys
    {
        /// <summary>
        /// Sentinel value meaning "no adapter / subsystem disabled".
        /// Used as `Preferred*Adapter` in configs and returned by facades'
        /// `AdapterName` when `_source` is null.
        /// </summary>
        public const string None = "UNIBRIDGE_NONE";
    }
}
