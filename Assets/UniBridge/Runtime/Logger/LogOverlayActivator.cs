using UnityEngine;

namespace UniBridge
{
    internal class LogOverlayActivator : MonoBehaviour
    {
        private const int   RequiredTaps = 6;
        private const float WindowSeconds = 3f;
        private const float CornerFraction = 0.2f;

        private int   _tapCount;
        private float _firstTapTime;

        private void Update()
        {
            if (!UniBridgeLogger.IsEnabled) return;

            Vector2? pos = null;
            if (Input.GetMouseButtonDown(0)) pos = Input.mousePosition;
            else if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) pos = t.position;
            }

            if (!pos.HasValue) return;

            var p = pos.Value;
            bool inCorner = p.x < Screen.width * CornerFraction && p.y > Screen.height * (1f - CornerFraction);
            if (!inCorner) return;

            float now = Time.unscaledTime;
            if (_tapCount == 0 || now - _firstTapTime > WindowSeconds)
            {
                _tapCount = 1;
                _firstTapTime = now;
                return;
            }

            _tapCount++;
            if (_tapCount >= RequiredTaps)
            {
                _tapCount = 0;
                Debug.Log($"[{nameof(LogOverlayActivator)}] Tap-combo triggered — showing overlay");
                UniBridgeLogger.ShowOverlay();
            }
        }
    }
}
