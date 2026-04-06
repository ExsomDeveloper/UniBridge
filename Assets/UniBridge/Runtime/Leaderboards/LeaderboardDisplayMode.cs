namespace UniBridge
{
    /// <summary>
    /// Describes how the active leaderboard adapter behaves on the current platform.
    /// Read <see cref="UniBridgeLeaderboards.DisplayMode"/> after initialization
    /// and configure the UI according to the mode.
    /// </summary>
    public enum LeaderboardDisplayMode
    {
        /// <summary>
        /// Leaderboards are unavailable on this platform or the adapter is not configured.
        /// Hide all leaderboard UI — do not submit scores or attempt to show the table.
        /// </summary>
        NotSupported,

        /// <summary>
        /// Full data access. <c>GetEntries</c> and <c>GetPlayerEntry</c> return
        /// real leaderboard data — render a custom UI from the returned list.
        /// Mode used by GPGS, Game Center, Simulated, and Debug adapters.
        /// </summary>
        InGame,

        /// <summary>
        /// Calling <c>GetEntries</c> opens the platform's native popup.
        /// No data is returned in the callback — the popup renders the table itself.
        /// Show a "Leaderboard" button that calls <c>GetEntries</c> on tap;
        /// do not attempt to populate a custom entry list.
        /// </summary>
        NativePopup,

        /// <summary>
        /// Score submission works, but fetching entries is unavailable.
        /// Hide the leaderboard table UI; continue calling <c>SubmitScore</c> in the background.
        /// </summary>
        SubmitOnly,
    }
}
