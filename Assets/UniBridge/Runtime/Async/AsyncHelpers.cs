#if UNIBRIDGE_UNITASK
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UniBridge.Async
{
    /// <summary>
    /// Internal helpers to convert UniBridge's callback-style APIs into <see cref="UniTask{T}"/>.
    /// Cancellation is honored both before dispatch and (best-effort) after the callback fires.
    /// </summary>
    internal static class AsyncHelpers
    {
        /// <summary>
        /// Dispatches a callback-style call and returns a UniTask that completes with the callback's argument.
        /// </summary>
        public static UniTask<T> Await<T>(Action<Action<T>> dispatch, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return UniTask.FromCanceled<T>(ct);

            var tcs = new UniTaskCompletionSource<T>();
            CancellationTokenRegistration reg = default;
            if (ct.CanBeCanceled)
                reg = ct.Register(() => tcs.TrySetCanceled());

            dispatch(result =>
            {
                reg.Dispose();
                if (ct.IsCancellationRequested) tcs.TrySetCanceled();
                else tcs.TrySetResult(result);
            });

            return tcs.Task;
        }

        /// <summary>
        /// Same as <see cref="Await{T}"/> but for a 2-argument callback — marshals it into a tuple.
        /// </summary>
        public static UniTask<(T1, T2)> Await<T1, T2>(Action<Action<T1, T2>> dispatch, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return UniTask.FromCanceled<(T1, T2)>(ct);

            var tcs = new UniTaskCompletionSource<(T1, T2)>();
            CancellationTokenRegistration reg = default;
            if (ct.CanBeCanceled)
                reg = ct.Register(() => tcs.TrySetCanceled());

            dispatch((a, b) =>
            {
                reg.Dispose();
                if (ct.IsCancellationRequested) tcs.TrySetCanceled();
                else tcs.TrySetResult((a, b));
            });

            return tcs.Task;
        }
    }
}
#endif
