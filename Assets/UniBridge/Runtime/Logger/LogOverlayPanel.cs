using System;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UnityEngine;
using UnityEngine.UI;

namespace UniBridge
{
    /// <summary>
    /// Legacy uGUI overlay rendered on a dedicated Canvas (Screen Space Overlay, max sortingOrder).
    /// uGUI is bulletproof at runtime — no themes, no PanelSettings, no asset dependencies. Always visible.
    /// </summary>
    internal class LogOverlayPanel : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void UniBridgeLogger_CopyToClipboard(string text, Action<int> onOk, Action<int> onFail);
        [DllImport("__Internal")] private static extern void UniBridgeLogger_DownloadAsFile(string text, string name);
#endif

        private Canvas    _canvas;
        private GameObject _root;
        private Text       _statusText;
        private Text       _logText;
        private ScrollRect _scrollRect;
        private InputField _rawField;
        private GameObject _rawPanel;
        private Font       _font;

        private float  _statusUntil;

        private void Awake()
        {
            BuildCanvas();
            BuildUI();
            SetVisible(false);
            Debug.Log($"[{nameof(LogOverlayPanel)}] Built (hidden). canvas.sortingOrder={_canvas.sortingOrder}");
        }

        public void Show()
        {
            RefreshEntries();
            SetVisible(true);
            Debug.Log($"[{nameof(LogOverlayPanel)}] Overlay shown (entries={UniBridgeLogger.GetSnapshot().Length})");
        }

        public void Hide() => SetVisible(false);

        public void Toggle()
        {
            if (_root == null) return;
            SetVisible(!_root.activeSelf);
            if (_root.activeSelf) RefreshEntries();
        }

        private void SetVisible(bool v) { if (_root != null) _root.SetActive(v); }

        // ─── Canvas ───────────────────────────────────────────────────────────

        private void BuildCanvas()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = short.MaxValue;
            _canvas.overrideSorting = true;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            scaler.referencePixelsPerUnit = 100;

            gameObject.AddComponent<GraphicRaycaster>();
        }

        // ─── UI ───────────────────────────────────────────────────────────────

        private void BuildUI()
        {
            _root = NewChild("Root", _canvas.transform);
            var rootRT = _root.GetComponent<RectTransform>();
            Stretch(rootRT);

            var bg = _root.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.94f);
            bg.raycastTarget = true;

            // Title
            var title = AddText(_root.transform, "UniBridge Logs", 22, FontStyle.Bold, Color.white);
            var titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1); titleRT.anchorMax = new Vector2(0.6f, 1);
            titleRT.pivot = new Vector2(0, 1);
            titleRT.anchoredPosition = new Vector2(16, -12);
            titleRT.sizeDelta = new Vector2(0, 30);

            // Status
            _statusText = AddText(_root.transform, "", 14, FontStyle.Italic, new Color(0.7f, 0.9f, 1f));
            _statusText.alignment = TextAnchor.UpperRight;
            var stRT = _statusText.GetComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0.6f, 1); stRT.anchorMax = new Vector2(1, 1);
            stRT.pivot = new Vector2(1, 1);
            stRT.anchoredPosition = new Vector2(-16, -16);
            stRT.sizeDelta = new Vector2(0, 26);

            // Buttons grid (wraps automatically when narrow)
            var buttonsGO = NewChild("Buttons", _root.transform);
            var bRT = buttonsGO.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0, 1); bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(0, 1);
            bRT.anchoredPosition = new Vector2(16, -50);
            bRT.sizeDelta = new Vector2(-32, 70); // height for up to 2 wrapped rows
            var grid = buttonsGO.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(90, 30);
            grid.spacing  = new Vector2(6, 4);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            var btnFitter = buttonsGO.AddComponent<ContentSizeFitter>();
            btnFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            btnFitter.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;

            AddButton(buttonsGO.transform, "Copy",     OnCopy);
            AddButton(buttonsGO.transform, "Download", OnDownload);
            AddButton(buttonsGO.transform, "Refresh",  RefreshEntries);
            AddButton(buttonsGO.transform, "Clear",    () => { UniBridgeLogger.Clear(); RefreshEntries(); SetStatus("Cleared"); });
            AddButton(buttonsGO.transform, "Raw",      ToggleRaw);
            AddButton(buttonsGO.transform, "Close",    Hide);

            // Scroll list
            var scrollGO = NewChild("Scroll", _root.transform);
            var srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0, 0); srRT.anchorMax = new Vector2(1, 1);
            srRT.offsetMin = new Vector2(16, 16); srRT.offsetMax = new Vector2(-16, -130);

            var scrollImg = scrollGO.AddComponent<Image>();
            scrollImg.color = new Color(0.08f, 0.08f, 0.08f, 0.7f);

            _scrollRect = scrollGO.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false; _scrollRect.vertical = true;

            var viewportGO = NewChild("Viewport", scrollGO.transform);
            var vpRT = viewportGO.GetComponent<RectTransform>();
            Stretch(vpRT);
            viewportGO.AddComponent<RectMask2D>();
            var vpImg = viewportGO.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.001f);
            _scrollRect.viewport = vpRT;

            var contentGO = NewChild("Content", viewportGO.transform);
            var cntRT = contentGO.GetComponent<RectTransform>();
            cntRT.anchorMin = new Vector2(0, 1); cntRT.anchorMax = new Vector2(1, 1);
            cntRT.pivot = new Vector2(0.5f, 1);
            cntRT.anchoredPosition = Vector2.zero;
            cntRT.sizeDelta = new Vector2(0, 0);

            // VerticalLayoutGroup drives children's heights so ContentSizeFitter can compute total content height.
            // Without this, ContentSizeFitter on an empty GO sees no ILayoutElement and content stays at height 0
            // → ScrollRect has nothing to scroll.
            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(6, 6, 4, 4);
            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _scrollRect.content = cntRT;

            _logText = AddText(contentGO.transform, "", 14, FontStyle.Normal, new Color(0.85f, 0.85f, 0.85f));
            _logText.supportRichText = true;
            _logText.alignment = TextAnchor.UpperLeft;
            _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _logText.verticalOverflow = VerticalWrapMode.Overflow;
            // Text implements ILayoutElement → VerticalLayoutGroup picks up its preferredHeight.
            // No manual anchor/sizeDelta: VerticalLayoutGroup manages layout fully.

            // Raw fallback (hidden by default)
            _rawPanel = NewChild("RawPanel", _root.transform);
            var rpRT = _rawPanel.GetComponent<RectTransform>();
            Stretch(rpRT);
            rpRT.offsetMin = new Vector2(16, 16); rpRT.offsetMax = new Vector2(-16, -130);
            var rpBg = _rawPanel.AddComponent<Image>();
            rpBg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);
            _rawPanel.SetActive(false);

            var rawTextGO = NewChild("Text", _rawPanel.transform);
            var rawText = rawTextGO.AddComponent<Text>();
            if (_font != null) rawText.font = _font;
            rawText.color = Color.white;
            rawText.fontSize = 12;
            rawText.alignment = TextAnchor.UpperLeft;
            rawText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var rawRT = rawTextGO.GetComponent<RectTransform>();
            Stretch(rawRT);
            rawRT.offsetMin = new Vector2(8, 8); rawRT.offsetMax = new Vector2(-8, -8);

            _rawField = rawTextGO.AddComponent<InputField>();
            _rawField.textComponent = rawText;
            _rawField.lineType = InputField.LineType.MultiLineNewline;
            _rawField.readOnly = true;

            var placeholderGO = NewChild("Placeholder", rawTextGO.transform);
            var placeholder = placeholderGO.AddComponent<Text>();
            if (_font != null) placeholder.font = _font;
            placeholder.text = "(no logs)";
            placeholder.color = new Color(0.5f, 0.5f, 0.5f);
            placeholder.fontSize = 12;
            var phRT = placeholderGO.GetComponent<RectTransform>();
            Stretch(phRT);
            _rawField.placeholder = placeholder;
        }

        private void ToggleRaw()
        {
            _rawPanel.SetActive(!_rawPanel.activeSelf);
            if (_rawPanel.activeSelf) _rawField.text = UniBridgeLogger.ExportAsText();
        }

        private void RefreshEntries()
        {
            // Preserve user scroll position: only auto-stick to bottom if already there.
            bool wasAtBottom = _scrollRect == null || _scrollRect.verticalNormalizedPosition <= 0.05f;

            var entries = UniBridgeLogger.GetSnapshot();
            var sb = new StringBuilder(entries.Length * 96);
            foreach (var e in entries)
            {
                var color = e.Type switch
                {
                    LogType.Error or LogType.Exception => "#ff7373",
                    LogType.Assert  => "#ff9966",
                    LogType.Warning => "#ffd966",
                    _               => "#dddddd",
                };
                sb.Append("<color=").Append(color).Append(">[")
                    .Append(e.Timestamp.ToString("HH:mm:ss.fff")).Append("] ")
                    .Append(e.Type).Append(": ").Append(e.Condition)
                    .Append("</color>\n");
            }
            _logText.text = sb.ToString();
            _statusText.text = $"{entries.Length} entries";
            if (_rawPanel.activeSelf) _rawField.text = UniBridgeLogger.ExportAsText();
            Canvas.ForceUpdateCanvases();
            if (wasAtBottom) _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void Update()
        {
            if (Time.unscaledTime > _statusUntil && _statusText != null)
                _statusText.text = $"{UniBridgeLogger.GetSnapshot().Length} entries";
        }

        private void SetStatus(string msg)
        {
            if (_statusText == null) return;
            _statusText.text = msg;
            _statusUntil = Time.unscaledTime + 3f;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static GameObject NewChild(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private Text AddText(Transform parent, string text, int size, FontStyle style, Color color)
        {
            var go = NewChild("Text", parent);
            var t = go.AddComponent<Text>();
            if (_font != null) t.font = _font;
            t.text = text; t.fontSize = size; t.fontStyle = style; t.color = color;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            return t;
        }

        private void AddButton(Transform parent, string label, Action onClick)
        {
            var go = NewChild(label + "Btn", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(90, 32);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.7f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var labelGO = NewChild("Label", go.transform);
            var lblRT = labelGO.GetComponent<RectTransform>();
            Stretch(lblRT);
            var lbl = labelGO.AddComponent<Text>();
            if (_font != null) lbl.font = _font;
            lbl.text = label; lbl.fontSize = 13; lbl.color = Color.white;
            lbl.alignment = TextAnchor.MiddleCenter;
        }

        // ─── Copy / Download ──────────────────────────────────────────────────

        private void OnCopy()
        {
            var text = UniBridgeLogger.ExportAsText();
            if (_rawPanel.activeSelf) _rawField.text = text;
#if UNITY_WEBGL && !UNITY_EDITOR
            UniBridgeLogger_CopyToClipboard(text, OnCopyOk, OnCopyFail);
            SetStatus("Copy requested…");
#else
            GUIUtility.systemCopyBuffer = text;
            SetStatus("Copied to clipboard");
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnCopyOk(int _)   => Debug.Log($"[{nameof(LogOverlayPanel)}] Copied to clipboard");

        [MonoPInvokeCallback(typeof(Action<int>))]
        private static void OnCopyFail(int _) => Debug.LogWarning($"[{nameof(LogOverlayPanel)}] Clipboard blocked — open Raw panel and select-all");
#endif

        private void OnDownload()
        {
            var text = UniBridgeLogger.ExportAsText();
            var name = $"unibridge_log_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
#if UNITY_WEBGL && !UNITY_EDITOR
            UniBridgeLogger_DownloadAsFile(text, name);
            SetStatus("Download triggered");
#else
            try
            {
                var path = System.IO.Path.Combine(Application.persistentDataPath, name);
                System.IO.File.WriteAllText(path, text);
                SetStatus($"Saved: {path}");
                Debug.Log($"[{nameof(LogOverlayPanel)}] Saved log to {path}");
            }
            catch (Exception ex) { SetStatus($"Save failed: {ex.Message}"); }
#endif
        }
    }
}
