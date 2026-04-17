#if UNIBRIDGE_UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniBridge.Async
{
    /// <summary>
    /// Async wrappers over <see cref="UniBridgeSaves"/>. Requires `com.cysharp.unitask` package.
    /// All methods accept an optional CancellationToken; callers are responsible for tying it
    /// to scene / object lifetime to avoid leaked completion sources.
    /// </summary>
    public static class UniBridgeSavesAsync
    {
        public static UniTask<bool> SaveAsync<T>(string key, T data, CancellationToken ct = default)
            => AsyncHelpers.Await<bool>(cb => UniBridgeSaves.Save(key, data, cb), ct);

        public static UniTask<(bool ok, T data)> LoadAsync<T>(string key, CancellationToken ct = default)
            => AsyncHelpers.Await<bool, T>(cb => UniBridgeSaves.Load<T>(key, cb), ct);

        /// <summary>
        /// Returns the raw JSON string without deserialization. Use for session-level caching
        /// or when the caller handles typing itself (avoids the <c>T = string</c> pitfall).
        /// </summary>
        public static UniTask<(bool ok, string json)> LoadRawAsync(string key, CancellationToken ct = default)
            => AsyncHelpers.Await<bool, string>(cb => UniBridgeSaves.LoadRaw(key, cb), ct);

        public static UniTask<bool> DeleteAsync(string key, CancellationToken ct = default)
            => AsyncHelpers.Await<bool>(cb => UniBridgeSaves.Delete(key, cb), ct);

        public static UniTask<bool> HasKeyAsync(string key, CancellationToken ct = default)
            => AsyncHelpers.Await<bool>(cb => UniBridgeSaves.HasKey(key, cb), ct);
    }
}
#endif
