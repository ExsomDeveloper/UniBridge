namespace UniBridge
{
    /// <summary>
    /// Error from a share operation.
    /// Passed as the second argument in the <see cref="SharingServices.ShowShareSheet"/> callback.
    /// Is <c>null</c> on success or cancellation.
    /// </summary>
    public class ShareError
    {
        public string Description { get; }

        public ShareError(string description)
        {
            Description = description;
        }
    }
}
