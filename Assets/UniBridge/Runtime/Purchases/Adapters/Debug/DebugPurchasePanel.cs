using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniBridge
{
    public class DebugPurchasePanel : MonoBehaviour
    {
        private Action<bool> _callback;
        private bool _uiReady;

        private VisualElement _overlay;
        private Label _labelName;
        private Label _labelPrice;
        private Label _labelType;

        // Loaded once; applied to every label/button so text renders
        // even when there is no theme in the active PanelSettings.
        private Font _font;

        private void EnsureUI()
        {
            if (_uiReady) return;
            _uiReady = true;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Use an existing UIDocument in the scene — it already has fonts and
            // a correctly sized rootVisualElement (screen size).
            // If none found, create a fallback UIDocument.
            VisualElement parent = null;

            var existing = FindObjectOfType<UIDocument>();
            if (existing != null)
            {
                parent = existing.rootVisualElement;
            }
            else
            {
                var settings = ScriptableObject.CreateInstance<PanelSettings>();
                settings.sortingOrder = 1000;
                settings.scaleMode   = PanelScaleMode.ConstantPixelSize;
                var doc = gameObject.AddComponent<UIDocument>();
                doc.panelSettings = settings;
                parent = doc.rootVisualElement;
            }

            BuildUI(parent);
        }

        // Creates a Label with an explicit font so text always renders.
        private Label MakeLabel(string text = "")
        {
            var lbl = new Label(text);
            if (_font != null)
                lbl.style.unityFont = new StyleFont(_font);
            return lbl;
        }

        private void BuildUI(VisualElement parent)
        {
            // Absolute overlay pinned to all four edges of the parent (full-screen).
            // Using left/top/right/bottom is more reliable than width/height: 100%
            // for absolute-positioned elements.
            _overlay = new VisualElement();
            _overlay.style.position        = Position.Absolute;
            _overlay.style.left            = 0;
            _overlay.style.top             = 0;
            _overlay.style.right           = 0;
            _overlay.style.bottom          = 0;
            _overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
            _overlay.style.alignItems      = Align.Center;
            _overlay.style.justifyContent  = Justify.Center;
            _overlay.style.display         = DisplayStyle.None;
            parent.Add(_overlay);

            // ── Dialog card ───────────────────────────────────────────────
            var dialog = new VisualElement();
            dialog.style.backgroundColor       = new Color(0.12f, 0.12f, 0.14f);
            dialog.style.borderTopLeftRadius     = 20;
            dialog.style.borderTopRightRadius    = 20;
            dialog.style.borderBottomLeftRadius  = 20;
            dialog.style.borderBottomRightRadius = 20;
            dialog.style.borderTopWidth    = 2; dialog.style.borderBottomWidth = 2;
            dialog.style.borderLeftWidth   = 2; dialog.style.borderRightWidth  = 2;
            dialog.style.borderTopColor    = new Color(0.27f, 0.27f, 0.31f);
            dialog.style.borderBottomColor = new Color(0.27f, 0.27f, 0.31f);
            dialog.style.borderLeftColor   = new Color(0.27f, 0.27f, 0.31f);
            dialog.style.borderRightColor  = new Color(0.27f, 0.27f, 0.31f);
            dialog.style.paddingTop    = 48; dialog.style.paddingBottom = 48;
            dialog.style.paddingLeft   = 56; dialog.style.paddingRight  = 56;
            dialog.style.minWidth      = 480;
            dialog.style.alignItems    = Align.Center;
            _overlay.Add(dialog);

            // Badge
            var badge = MakeLabel("DEBUG PURCHASE");
            badge.style.color                   = new Color(0.39f, 0.82f, 0.39f);
            badge.style.fontSize                = 20;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.unityTextAlign          = TextAnchor.MiddleCenter;
            badge.style.marginBottom            = 28;
            dialog.Add(badge);

            // Product name
            _labelName = MakeLabel();
            _labelName.style.fontSize                = 42;
            _labelName.style.color                   = new Color(0.9f, 0.9f, 0.9f);
            _labelName.style.unityFontStyleAndWeight = FontStyle.Bold;
            _labelName.style.unityTextAlign          = TextAnchor.MiddleCenter;
            _labelName.style.marginBottom            = 12;
            dialog.Add(_labelName);

            // Price
            _labelPrice = MakeLabel();
            _labelPrice.style.fontSize        = 34;
            _labelPrice.style.color           = new Color(0.7f, 0.86f, 0.7f);
            _labelPrice.style.unityTextAlign  = TextAnchor.MiddleCenter;
            _labelPrice.style.marginBottom    = 8;
            dialog.Add(_labelPrice);

            // Type
            _labelType = MakeLabel();
            _labelType.style.fontSize        = 22;
            _labelType.style.color           = new Color(0.55f, 0.63f, 0.78f);
            _labelType.style.unityTextAlign  = TextAnchor.MiddleCenter;
            _labelType.style.marginBottom    = 44;
            dialog.Add(_labelType);

            // ── Buttons ───────────────────────────────────────────────────
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.width         = Length.Percent(100);
            dialog.Add(row);

            var btnBuy = new Button(OnConfirm) { text = "Buy" };
            if (_font != null) btnBuy.style.unityFont = new StyleFont(_font);
            ApplyButtonStyle(btnBuy, new Color(0.24f, 0.67f, 0.31f), new Color(0.94f, 1f, 0.94f));
            btnBuy.style.marginRight = 12;
            row.Add(btnBuy);

            var btnCancel = new Button(OnCancel) { text = "Cancel" };
            if (_font != null) btnCancel.style.unityFont = new StyleFont(_font);
            ApplyButtonStyle(btnCancel, new Color(0.27f, 0.27f, 0.31f), new Color(0.82f, 0.82f, 0.86f));
            btnCancel.style.marginLeft = 12;
            row.Add(btnCancel);
        }

        private static void ApplyButtonStyle(Button btn, Color bg, Color fg)
        {
            btn.style.flexGrow                 = 1;
            btn.style.fontSize                 = 30;
            btn.style.unityFontStyleAndWeight  = FontStyle.Bold;
            btn.style.backgroundColor          = bg;
            btn.style.color                    = fg;
            btn.style.borderTopLeftRadius      = 14;
            btn.style.borderTopRightRadius     = 14;
            btn.style.borderBottomLeftRadius   = 14;
            btn.style.borderBottomRightRadius  = 14;
            btn.style.borderTopWidth    = 0; btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth   = 0; btn.style.borderRightWidth  = 0;
            btn.style.paddingTop    = 20; btn.style.paddingBottom = 20;
            btn.style.paddingLeft   = 44; btn.style.paddingRight  = 44;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        public void Show(string productId, string price, string type, Action<bool> callback)
        {
            EnsureUI();
            _callback        = callback;
            _labelName.text  = productId;
            _labelPrice.text = price;
            _labelType.text  = type;
            _overlay.style.display = DisplayStyle.Flex;
        }

        private void OnConfirm() { var cb = _callback; Hide(); cb?.Invoke(true); }
        private void OnCancel()  { var cb = _callback; Hide(); cb?.Invoke(false); }

        private void Hide()
        {
            if (_overlay != null)
                _overlay.style.display = DisplayStyle.None;
            _callback = null;
        }
    }
}
