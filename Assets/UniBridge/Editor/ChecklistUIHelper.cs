using UnityEngine;
using UnityEngine.UIElements;

namespace UniBridge.Editor
{
    public static class ChecklistUIHelper
    {
        public static readonly Color Green  = new(0.25f, 0.80f, 0.25f);
        public static readonly Color Orange = new(0.85f, 0.40f, 0.20f);
        public static readonly Color Grey   = new(0.55f, 0.55f, 0.55f);

        public static VisualElement BuildGroup(string title, ChecklistItem[] items)
        {
            var group = new VisualElement();
            group.style.marginBottom = 8;

            var header = new Label(title);
            header.style.fontSize                = 11;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color                   = new StyleColor(Grey);
            header.style.marginBottom            = 4;
            group.Add(header);

            foreach (var item in items)
                group.Add(BuildItem(item));

            return group;
        }

        public static VisualElement BuildItem(ChecklistItem item)
        {
            var wrapper = new VisualElement();
            wrapper.style.marginBottom = 4;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.Center;

            var dot = new VisualElement();
            dot.style.width                   = 8;
            dot.style.height                  = 8;
            dot.style.borderTopLeftRadius     = 4;
            dot.style.borderTopRightRadius    = 4;
            dot.style.borderBottomLeftRadius  = 4;
            dot.style.borderBottomRightRadius = 4;
            dot.style.marginRight             = 6;
            dot.style.flexShrink              = 0;
            dot.style.backgroundColor         = new StyleColor(item.Ok ? Green : Orange);

            string labelText = item.IsOptional ? $"{item.Label} (необязательно)" : item.Label;
            var lbl = new Label(labelText);
            lbl.style.fontSize = 11;

            row.Add(dot);
            row.Add(lbl);
            wrapper.Add(row);

            if (!item.Ok && !string.IsNullOrEmpty(item.Hint))
            {
                var hint = new Label(item.Hint);
                hint.style.fontSize    = 10;
                hint.style.color       = new StyleColor(Grey);
                hint.style.paddingLeft = 14;
                hint.style.marginTop   = 1;
                wrapper.Add(hint);
            }

            return wrapper;
        }
    }
}
