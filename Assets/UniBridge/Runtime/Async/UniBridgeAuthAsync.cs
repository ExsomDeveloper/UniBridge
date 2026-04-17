#if UNIBRIDGE_UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniBridge.Async
{
    /// <summary>
    /// Async wrappers over <see cref="UniBridgeAuth"/>. Requires `com.cysharp.unitask` package.
    /// </summary>
    public static class UniBridgeAuthAsync
    {
        /// <summary>
        /// Runs the platform's sign-in flow. Resolves with <c>true</c> on success,
        /// <c>false</c> if the user cancelled or the platform is unsupported.
        /// </summary>
        public static UniTask<bool> AuthorizeAsync(CancellationToken ct = default)
            => AsyncHelpers.Await<bool>(cb => UniBridgeAuth.Authorize(cb), ct);
    }
}
#endif
