namespace UniBridge
{
    /// <summary>
    /// Result of a share operation.
    /// </summary>
    public enum ShareResultCode
    {
        /// <summary>The user completed sharing (iOS, Playgama).</summary>
        Completed,

        /// <summary>The user dismissed the share dialog without sending (iOS).</summary>
        Cancelled,

        /// <summary>The platform does not report a result (Android, Mock).</summary>
        Unknown,
    }
}
