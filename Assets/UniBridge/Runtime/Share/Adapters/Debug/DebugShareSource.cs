using System;
using UnityEngine;

namespace UniBridge
{
    public class DebugShareSource : IShareSource
    {
        public bool IsInitialized { get; private set; }
        public bool IsSupported   => true;

        public void Initialize(UniBridgeShareConfig config, Action onSuccess, Action onFailed)
        {
            IsInitialized = true;
            Debug.Log($"[{nameof(DebugShareSource)}]: Инициализирован");
            onSuccess?.Invoke();
        }

        public void Share(ShareData data, Action<ShareSheetResult, ShareError> onComplete)
        {
            var info = "";
            if (data.Text != null)     info += $"Текст: '{data.Text}'";
            if (data.Image != null)    info += (info.Length > 0 ? ", " : "") + "Картинка: [Texture2D]";
            if (data.ImageUrl != null) info += (info.Length > 0 ? ", " : "") + $"URL: '{data.ImageUrl}'";
            Debug.Log($"[{nameof(DebugShareSource)}]: Share — {info} (Editor mock)");
            onComplete?.Invoke(new ShareSheetResult(ShareResultCode.Completed), null);
        }
    }
}
