#if UNIBRIDGE_UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniBridge.Async
{
    /// <summary>
    /// Async wrappers over <see cref="SharingServices"/>. Requires `com.cysharp.unitask` package.
    /// </summary>
    public static class UniBridgeShareAsync
    {
        /// <summary>
        /// Shows the native share sheet. Resolves with <c>(result, error)</c>:
        /// <c>error</c> is non-null only on initialization / invalid-data errors;
        /// <c>result</c> contains <see cref="ShareResultCode"/> on platforms that report it.
        /// </summary>
        public static UniTask<(ShareSheetResult result, ShareError error)> ShowShareSheetAsync(
            CancellationToken ct = default,
            params ShareItem[] items)
            => AsyncHelpers.Await<ShareSheetResult, ShareError>(
                cb => SharingServices.ShowShareSheet(cb, items), ct);
    }
}
#endif
