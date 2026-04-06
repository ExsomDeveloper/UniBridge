namespace UniBridge
{
    public static class UniBridgePlatform
    {
        public const string GooglePlay = "GooglePlay";
        public const string RuStore = "RuStore";
        public const string AppStore = "AppStore";
        public const string Playgama = "Playgama";

        public static string Current =>
#if UNIBRIDGE_STORE_GOOGLEPLAY
            GooglePlay;
#elif UNIBRIDGE_STORE_RUSTORE
            RuStore;
#elif UNIBRIDGE_STORE_APPSTORE
            AppStore;
#elif UNIBRIDGE_STORE_PLAYGAMA
            Playgama;
#else
            "Unknown";
#endif
    }
}
