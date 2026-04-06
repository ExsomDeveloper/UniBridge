namespace UniBridge
{
    public enum PurchaseStatus
    {
        None = 0,
        Success = 1,
        Failed = 2,
        Cancelled = 3,
        AlreadyOwned = 4,
        NotInitialized = 5,
        NotSupported = 6,
        PendingConfirmation = 7,
        Restored = 8
    }
}
