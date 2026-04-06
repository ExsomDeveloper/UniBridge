namespace UniBridge
{
    public enum PlatformId
    {
        GameDistribution,
        Telegram,
        Y8,
        Lagged,
        Huawei,
        Msn,
        Discord,
        GamePush,
        CrazyGames,
        Facebook,
        Yandex,
        YouTube,
        Xiaomi,
        Vk,
    }

    public static class PlatformIdExtensions
    {
        public static string ToStringId(this PlatformId id) => id switch
        {
            PlatformId.GameDistribution => "game_distribution",
            PlatformId.Telegram         => "telegram",
            PlatformId.Y8               => "y8",
            PlatformId.Lagged           => "lagged",
            PlatformId.Huawei           => "huawei",
            PlatformId.Msn              => "msn",
            PlatformId.Discord          => "discord",
            PlatformId.GamePush         => "gamepush",
            PlatformId.CrazyGames       => "crazy_games",
            PlatformId.Facebook         => "facebook",
            PlatformId.Yandex           => "yandex",
            PlatformId.YouTube          => "youtube",
            PlatformId.Xiaomi           => "xiaomi",
            PlatformId.Vk               => "vk",
            _                           => "",
        };
    }
}
