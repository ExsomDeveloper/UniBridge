#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

namespace StoreScreenshots
{
    public class ScreenshotCapture : MonoBehaviour
    {
        public static ScreenshotCapture Instance { get; private set; }

        private static bool _isCapturing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            var go = new GameObject("[StoreScreenshots]") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ScreenshotCapture>();
        }

        private void Update()
        {
            if (_isCapturing) return;

            var config = Resources.Load<ScreenshotConfig>(nameof(ScreenshotConfig));
            if (config == null) return;

            bool modifierOk = !config.RequireShift
                              || Input.GetKey(KeyCode.LeftShift)
                              || Input.GetKey(KeyCode.RightShift);

            if (modifierOk && Input.GetKeyDown(config.Hotkey))
                StartCoroutine(CaptureAll(config));
        }

        public IEnumerator CaptureAll(ScreenshotConfig config)
        {
            if (_isCapturing) yield break;
            _isCapturing = true;

            if (!GameViewUtils.IsAvailable)
            {
                Debug.LogError("[StoreScreenshots] GameView reflection API unavailable in this Unity version.");
                _isCapturing = false;
                yield break;
            }

            float savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), config.OutputFolder);
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            int originalSizeIndex = GameViewUtils.GetSelectedSizeIndex();
            var timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            int captured = 0;

            foreach (var res in config.Resolutions)
            {
                if (!res.Enabled || res.Width <= 0 || res.Height <= 0) continue;

                // Add custom size and select it
                string sizeLabel = $"SS_{res.Width}x{res.Height}";
                int sizeIndex = GameViewUtils.AddCustomSize(res.Width, res.Height, sizeLabel);
                if (sizeIndex < 0)
                {
                    Debug.LogWarning($"[StoreScreenshots] Failed to add custom size {res.Width}x{res.Height}");
                    continue;
                }

                GameViewUtils.SetSelectedSizeIndex(sizeIndex);

                // Wait for Game View to resize, re-layout, and render
                yield return null;
                yield return null;
                yield return new WaitForEndOfFrame();

                // Capture everything — UGUI, UI Toolkit, cameras
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex != null)
                {
                    var resDir = Path.Combine(outputDir, res.Label);
                    if (!Directory.Exists(resDir))
                        Directory.CreateDirectory(resDir);

                    var filename = $"{timestamp}.png";
                    var filePath = Path.Combine(resDir, filename);
                    File.WriteAllBytes(filePath, tex.EncodeToPNG());
                    captured++;
                    Debug.Log($"[StoreScreenshots] Captured: {filePath} ({tex.width}x{tex.height})");
                    Destroy(tex);
                }

                // Wait a frame before removing the size
                yield return null;

                // Clean up the custom size
                GameViewUtils.RemoveCustomSize(sizeIndex);
            }

            // Restore original Game View size
            if (originalSizeIndex >= 0)
                GameViewUtils.SetSelectedSizeIndex(originalSizeIndex);

            Time.timeScale = savedTimeScale;

            Debug.Log($"[StoreScreenshots] Done! {captured} screenshot(s) saved to: {outputDir}");

            _isCapturing = false;
        }
    }
}
#endif
