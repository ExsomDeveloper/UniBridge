namespace UniBridge
{
    /// <summary>
    /// Result of a <see cref="SharingServices.ShowShareSheet"/> call.
    /// </summary>
    public class ShareSheetResult
    {
        public ShareResultCode ResultCode { get; }

        public ShareSheetResult(ShareResultCode resultCode)
        {
            ResultCode = resultCode;
        }
    }
}
