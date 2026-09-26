using System;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Settings overlay (spec extension, explicit request: "un menu
    /// settings pour gérer le volume de musique, de sfx, général et une
    /// checkbox pour daltonisme") — reachable any time via the Escape key,
    /// same "always-available, not editor-only" convention as Tab/C/H (see
    /// GameBootstrap.Update). Master/Music/SFX are real UnityEngine.UI.
    /// Slider controls (the first in this codebase — every existing HUD bar
    /// is a hand-rolled fill-rect instead) persisted via VolumeSettings;
    /// only Master does anything audible today (see
    /// GameBootstrap.ApplyVolumeSettings) since Contigu has no music/SFX
    /// clips yet. The colorblind checkbox is a second, always-visible entry
    /// point to the exact same ColorblindMode.Toggle() the C key already
    /// calls — no separate state of its own.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour
    {
        private const float RowLabelWidth = 220f;
        private const float RowSliderWidth = 360f;
        private const float RowValueWidth = 60f;
        private const float RowHeight = 24f;
        private const float LabelColumnX = -226f;
        private const float ControlColumnX = 80f;
        private const float ValueColumnX = 306f;
        private const float CheckboxSize = 26f;

        private RectTransform _root;

        public RectTransform Build(Transform parent)
        {
            var overlay = UIFactory.CreatePanel(parent, "SettingsOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "Settings", 32, UITheme.TextPrimary);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -36f);
            header.rectTransform.sizeDelta = new Vector2(1000f, 46f);

            BuildVolumeRow(_root, -140f, "Master Volume", UISprites.ScoreBarFill, VolumeSettings.MasterVolume, VolumeSettings.SetMasterVolume);
            BuildVolumeRow(_root, -230f, "Music Volume", UISprites.PiecesBarFill, VolumeSettings.MusicVolume, VolumeSettings.SetMusicVolume);
            BuildVolumeRow(_root, -320f, "SFX Volume", UISprites.SfxBarFill, VolumeSettings.SfxVolume, VolumeSettings.SetSfxVolume);
            BuildColorblindRow(_root, -410f);

            var closeBtn = UIFactory.CreateButton(_root, "Close", "Close", UISprites.ChooseButtonBackground, 18);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 30f);
            closeRect.sizeDelta = new Vector2(220f, 52f);
            closeBtn.onClick.AddListener(Hide);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>One label + slider + live "NN%" readout, all in the same 3-column grid every row shares (see LabelColumnX/ControlColumnX/ValueColumnX) so the 3 volume rows and the checkbox row line up.</summary>
        private void BuildVolumeRow(Transform parent, float y, string label, Sprite fillSprite, float initialValue, Action<float> onChanged)
        {
            var labelText = UIFactory.CreateText(parent, label + "Label", label, 18, UITheme.TextPrimary, TextAnchor.MiddleRight);
            labelText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            labelText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            labelText.rectTransform.pivot = new Vector2(0.5f, 1f);
            labelText.rectTransform.anchoredPosition = new Vector2(LabelColumnX, y);
            labelText.rectTransform.sizeDelta = new Vector2(RowLabelWidth, RowHeight + 6f);

            var slider = BuildSlider(parent, label + "Slider", fillSprite, initialValue);
            var sliderRect = slider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 1f);
            sliderRect.anchorMax = new Vector2(0.5f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 1f);
            sliderRect.anchoredPosition = new Vector2(ControlColumnX, y);
            sliderRect.sizeDelta = new Vector2(RowSliderWidth, RowHeight);

            var valueLabel = UIFactory.CreateText(parent, label + "Value", Mathf.RoundToInt(initialValue * 100) + "%", 16, UITheme.TextMuted);
            valueLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            valueLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            valueLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            valueLabel.rectTransform.anchoredPosition = new Vector2(ValueColumnX, y);
            valueLabel.rectTransform.sizeDelta = new Vector2(RowValueWidth, RowHeight + 6f);

            slider.onValueChanged.AddListener(v =>
            {
                onChanged(v);
                valueLabel.text = Mathf.RoundToInt(v * 100) + "%";
            });
        }

        /// <summary>Standard 3-part UnityEngine.UI.Slider anatomy (background/fill/handle) built entirely from Colorful UI sprites — the first real Slider in this codebase (every HUD bar elsewhere is a hand-rolled fill-rect instead, since none of them needed to be draggable).</summary>
        private static Slider BuildSlider(Transform parent, string name, Sprite fillSprite, float initialValue)
        {
            var root = UIFactory.CreateUIObject(name, parent);

            var background = UIFactory.CreateSlicedImage(root, "Background", UISprites.BarTrack);
            UIFactory.StretchFull(background.rectTransform);

            var fillArea = UIFactory.CreateUIObject("FillArea", root);
            UIFactory.StretchFull(fillArea);
            fillArea.offsetMin = new Vector2(6f, 4f);
            fillArea.offsetMax = new Vector2(-6f, -4f);

            var fill = UIFactory.CreateSlicedImage(fillArea, "Fill", fillSprite);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = UIFactory.CreateUIObject("HandleArea", root);
            UIFactory.StretchFull(handleArea);

            var handle = UIFactory.CreateSlicedImage(handleArea, "Handle", UISprites.SliderHandle);
            handle.rectTransform.sizeDelta = new Vector2(28f, 28f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initialValue;

            return slider;
        }

        /// <summary>A small square Button standing in for a checkbox (no checkmark sprite exists in the Colorful UI pack) — tinted UITheme.ButtonSelected plus a literal "X" label when checked, both driven straight off ColorblindMode.IsEnabled/Toggle(), the exact same state the C key already reads and flips.</summary>
        private static void BuildColorblindRow(Transform parent, float y)
        {
            var labelText = UIFactory.CreateText(parent, "ColorblindLabel", "Colorblind Mode", 18, UITheme.TextPrimary, TextAnchor.MiddleRight);
            labelText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            labelText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            labelText.rectTransform.pivot = new Vector2(0.5f, 1f);
            labelText.rectTransform.anchoredPosition = new Vector2(LabelColumnX, y);
            labelText.rectTransform.sizeDelta = new Vector2(RowLabelWidth, RowHeight + 6f);

            var checkboxButton = UIFactory.CreateButton(parent, "ColorblindCheckbox", "",
                ColorblindMode.IsEnabled ? UITheme.ButtonSelected : UITheme.PanelLight, 14);
            var checkboxImage = checkboxButton.GetComponent<Image>();
            var checkboxRect = checkboxButton.GetComponent<RectTransform>();
            checkboxRect.anchorMin = new Vector2(0.5f, 1f);
            checkboxRect.anchorMax = new Vector2(0.5f, 1f);
            checkboxRect.pivot = new Vector2(0.5f, 1f);
            checkboxRect.anchoredPosition = new Vector2(ControlColumnX - RowSliderWidth / 2f + CheckboxSize / 2f, y);
            checkboxRect.sizeDelta = new Vector2(CheckboxSize, CheckboxSize);

            var checkmark = UIFactory.CreateText(checkboxButton.transform, "Check", "X", 16, UITheme.TextPrimary);
            checkmark.raycastTarget = false;
            UIFactory.StretchFull(checkmark.rectTransform);
            checkmark.gameObject.SetActive(ColorblindMode.IsEnabled);

            checkboxButton.onClick.AddListener(() =>
            {
                ColorblindMode.Toggle();
                checkboxImage.color = ColorblindMode.IsEnabled ? UITheme.ButtonSelected : UITheme.PanelLight;
                checkmark.gameObject.SetActive(ColorblindMode.IsEnabled);
            });
        }

        public void Show()
        {
            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        public void Toggle()
        {
            _root.gameObject.SetActive(!_root.gameObject.activeSelf);
        }
    }
}
