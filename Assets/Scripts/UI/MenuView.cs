using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Меню и оверлеи: заголовок, список строк с подсветкой выбранной и подсказка
    /// внизу. Один вид на все экраны — так меньше кода и меньше визуального шума.
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuView : MonoBehaviour
    {
        Canvas _canvas;
        RectTransform _root;

        Text _title;
        Text _subtitle;
        Text _footer;
        Text _prompt;

        RectTransform _listRoot;
        readonly List<Text> _lines = new List<Text>();

        public bool IsVisible { get; private set; }

        public void Initialize(Transform parent)
        {
            _canvas = UiFactory.CreateCanvas(parent, "Menu", 200);
            _root = _canvas.transform as RectTransform;

            var backdrop = UiFactory.Panel(_root, "Backdrop", Palette.WithAlpha(Palette.Backdrop, 0.93f));

            _title = UiFactory.Label(_root, "Title", "", 86, TextAnchor.MiddleCenter, Palette.Player, FontStyle.Bold);
            UiFactory.SetAnchors(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1700f, 110f));

            _subtitle = UiFactory.Label(_root, "Subtitle", "", 32, TextAnchor.MiddleCenter, Palette.Accent);
            UiFactory.SetAnchors(_subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -222f), new Vector2(1600f, 46f));

            _listRoot = UiFactory.Place(_root, "List", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1200f, 600f));

            _prompt = UiFactory.Label(_root, "Prompt", "", 26, TextAnchor.MiddleCenter, Palette.WithAlpha(Palette.Player, 0.6f));
            UiFactory.SetAnchors(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1500f, 40f));

            _footer = UiFactory.Label(_root, "Footer", "", 22, TextAnchor.MiddleCenter, Palette.WithAlpha(Palette.Player, 0.42f));
            UiFactory.SetAnchors(_footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(1600f, 60f));
            _footer.verticalOverflow = VerticalWrapMode.Overflow;

            backdrop.gameObject.name = "Backdrop";
            Hide();
        }

        public void Show(string title, string subtitle, IReadOnlyList<string> lines, int selected, string footer)
        {
            IsVisible = true;
            _root.gameObject.SetActive(true);

            _title.text = title;
            _subtitle.text = subtitle;
            _footer.text = footer;

            EnsureLines(lines.Count);

            float step = 56f;
            float startY = (lines.Count - 1) * step * 0.5f;

            for (int i = 0; i < _lines.Count; i++)
            {
                bool active = i < lines.Count;
                var line = _lines[i];
                line.gameObject.SetActive(active);
                if (!active) continue;

                bool isSelected = i == selected;
                line.text = (isSelected ? "▸  " : "     ") + lines[i];
                line.color = isSelected ? Palette.Accent : Palette.WithAlpha(Palette.Player, 0.62f);
                line.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                UiFactory.SetAnchors(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, startY - i * step), new Vector2(1100f, 44f));
            }

            _prompt.text = lines.Count > 0 ? "W / S — выбор      SPACE — подтвердить" : "SPACE — продолжить";
        }

        void EnsureLines(int count)
        {
            while (_lines.Count < count)
            {
                var text = UiFactory.Label(_listRoot, "Line" + _lines.Count, "", 30, TextAnchor.MiddleCenter, Palette.Player);
                text.fontStyle = FontStyle.Normal;
                _lines.Add(text);
            }
        }

        public void Hide()
        {
            IsVisible = false;
            if (_root != null) _root.gameObject.SetActive(false);
        }
    }
}