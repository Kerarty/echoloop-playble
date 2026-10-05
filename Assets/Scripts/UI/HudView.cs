using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Игровой интерфейс: таймер петли, счётчик циклов, осколки и число живых эхо.
    /// Вся информация подана одной строкой вверху — чтобы взгляд игрока
    /// почти не покидал персонажа, это критично для таймлупа.
    /// </summary>
    [DisallowMultipleComponent]
    public class HudView : MonoBehaviour
    {
        Canvas _canvas;
        RectTransform _root;

        Text _levelLabel;
        Text _cycleLabel;
        Text _shardLabel;
        Text _hintLabel;

        Image _timerFill;
        Image _echoRoot;
        Image _shardPipRoot;
        readonly List<Image> _echoPips = new List<Image>();
        readonly List<Image> _shardPips = new List<Image>();

        RectTransform _overlay;
        Text _overlayTitle;
        Text _overlaySubtitle;

        Image _flash;
        Color _flashColor;
        float _flashAmount;
        float _pulse;

        public bool IsVisible { get; private set; }

        public void Initialize(Transform parent)
        {
            _canvas = UiFactory.CreateCanvas(parent, "HUD", 100);
            _root = _canvas.transform as RectTransform;

            BuildTopBar();
            BuildPips();
            BuildHint();
            BuildOverlay();
            BuildFlash();
        }

        void BuildTopBar()
        {
            _levelLabel = UiFactory.Label(_root, "Level", "УРОВЕНЬ", 30, TextAnchor.MiddleLeft, Palette.Player, FontStyle.Bold);
            UiFactory.SetAnchors(_levelLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f, -44f), new Vector2(700f, 44f));
            UiFactory.Outline(_levelLabel, Palette.WithAlpha(Color.black, 0.75f));

            _cycleLabel = UiFactory.Label(_root, "Cycle", "ЦИКЛ 01", 32, TextAnchor.MiddleCenter, Palette.Accent, FontStyle.Bold);
            UiFactory.SetAnchors(_cycleLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(420f, 44f));
            UiFactory.Outline(_cycleLabel, Palette.WithAlpha(Color.black, 0.75f));

            _shardLabel = UiFactory.Label(_root, "Shards", "0/0", 30, TextAnchor.MiddleRight, Palette.AccentWarm, FontStyle.Bold);
            UiFactory.SetAnchors(_shardLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56f, -44f), new Vector2(400f, 44f));
            UiFactory.Outline(_shardLabel, Palette.WithAlpha(Color.black, 0.75f));

            // Таймер петли.
            var barBg = UiFactory.Sprite(_root, "TimerBg", Palette.WithAlpha(Palette.StructureDark, 0.9f));
            UiFactory.SetAnchors(barBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(520f, 14f));

            _timerFill = UiFactory.Sprite(barBg.transform, "TimerFill", Palette.Accent);
            _timerFill.type = Image.Type.Filled;
            _timerFill.fillMethod = Image.FillMethod.Horizontal;
            _timerFill.fillOrigin = 0;
            _timerFill.fillAmount = 1f;

            var rect = _timerFill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(2f, 2f);
            rect.offsetMax = new Vector2(-2f, -2f);
        }

        void BuildPips()
        {
            _echoRoot = UiFactory.Sprite(_root, "EchoPips", Color.clear);
            UiFactory.SetAnchors(_echoRoot.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-10f, -70f), new Vector2(10f, 10f));

            _shardPipRoot = UiFactory.Sprite(_root, "ShardPips", Color.clear);
            UiFactory.SetAnchors(_shardPipRoot.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56f, -70f), new Vector2(10f, 10f));
        }

        void BuildHint()
        {
            _hintLabel = UiFactory.Label(_root, "Hint", "", 24, TextAnchor.MiddleCenter, Palette.WithAlpha(Palette.Player, 0.72f));
            UiFactory.SetAnchors(_hintLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(1500f, 40f));
            UiFactory.Outline(_hintLabel, Palette.WithAlpha(Color.black, 0.7f));
        }

        void BuildOverlay()
        {
            _overlay = UiFactory.Stretch(_root, "Overlay");
            var bg = _overlay.gameObject.AddComponent<Image>();
            bg.color = Palette.WithAlpha(Color.black, 0.72f);

            _overlayTitle = UiFactory.Label(_overlay, "Title", "", 76, TextAnchor.MiddleCenter, Palette.Player, FontStyle.Bold);
            UiFactory.SetAnchors(_overlayTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1600f, 110f));
            UiFactory.Outline(_overlayTitle, Palette.WithAlpha(Color.black, 0.8f), 3f);

            _overlaySubtitle = UiFactory.Label(_overlay, "Subtitle", "", 30, TextAnchor.UpperCenter, Palette.WithAlpha(Palette.Player, 0.85f));
            UiFactory.SetAnchors(_overlaySubtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1400f, 60f));
            _overlaySubtitle.verticalOverflow = VerticalWrapMode.Truncate;
            _overlaySubtitle.rectTransform.pivot = new Vector2(0.5f, 1f);

            _overlay.gameObject.SetActive(false);
        }

        void BuildFlash()
        {
            _flash = UiFactory.Panel(_root, "Flash", Color.clear);
        }

        // ---------------------------------------------------------------

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (_root != null) _root.gameObject.SetActive(visible);
        }

        public void SetLevel(string name, string subtitle)
        {
            _levelLabel.text = subtitle.ToUpperInvariant() + "  ·  " + name.ToUpperInvariant();
        }

        public void SetCycle(int index)
        {
            _cycleLabel.text = "ЦИКЛ " + (index + 1).ToString("00");
        }

        public void SetHint(string hint)
        {
            _hintLabel.text = hint;
        }

        public void SetTimer(float normalized)
        {
            float t = Mathf.Clamp01(normalized);
            _timerFill.fillAmount = t;

            // На последних секундах таймер уходит в красный — это единственное
            // предупреждение, которое нужно успеть заметить.
            float danger = Mathf.InverseLerp(0.35f, 0f, t);
            Color color = Color.Lerp(Palette.Accent, Palette.Danger, danger);
            if (danger > 0.01f) color = Color.Lerp(color, Color.white, Mathf.PingPong(Time.time * 8f, 1f) * 0.35f);
            _timerFill.color = color;
        }

        public void SetShards(int collected, int total)
        {
            _shardLabel.text = "ОСКОЛКИ  " + collected + " / " + total;
            RebuildPips(_shardPipRoot, _shardPips, total, i => i < collected, Palette.AccentWarm);
        }

        public void SetEchoes(int live, int max)
        {
            RebuildPips(_echoRoot, _echoPips, max, i => i < live, Palette.Accent);
        }

        static void RebuildPips(RectTransform root, List<Image> pips, int count, System.Func<int, bool> filled, Color color)
        {
            while (pips.Count > count)
            {
                if (pips[pips.Count - 1] != null) Destroy(pips[pips.Count - 1].gameObject);
                pips.RemoveAt(pips.Count - 1);
            }

            while (pips.Count < count)
            {
                var pip = UiFactory.Sprite(root, "Pip" + pips.Count, Color.clear);
                var r = pip.rectTransform;
                r.sizeDelta = new Vector2(16f, 16f);
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(pips.Count * 22f, 0f);
                pips.Add(pip);
            }

            for (int i = 0; i < pips.Count; i++)
            {
                if (pips[i] == null) continue;
                pips[i].color = filled(i) ? color : Palette.WithAlpha(Palette.StructureLight, 0.55f);
            }
        }

        public void ShowOverlay(string title, string subtitle)
        {
            _overlay.gameObject.SetActive(true);
            _overlayTitle.text = title;
            _overlaySubtitle.text = subtitle;
        }

        public void HideOverlay() => _overlay.gameObject.SetActive(false);

        public void Flash(Color color, float amount)
        {
            _flashColor = color;
            _flashAmount = Mathf.Max(_flashAmount, amount);
        }

        void Update()
        {
            if (_flashAmount > 0f)
            {
                _flashAmount = Mathf.MoveTowards(_flashAmount, 0f, Time.deltaTime * 1.9f);
                _flash.color = Palette.WithAlpha(_flashColor, _flashAmount);
            }
            else if (_flash.color.a > 0f)
            {
                _flash.color = Color.clear;
            }

            _pulse += Time.deltaTime;
            _cycleLabel.color = Color.Lerp(Palette.Accent, Color.white, Mathf.PingPong(_pulse * 0.6f, 1f) * 0.18f);
        }
    }
}