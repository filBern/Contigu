using System;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Settings overlay, reachable any time via the Escape key. Master/Music/SFX are real
    /// UnityEngine.UI.Slider controls persisted via VolumeSettings; only Master does anything audible today
    /// (see GameBootstrap.ApplyVolumeSettings).
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

        /// <summary>The player confirmed they want to give up the current run (the run is scored as a defeat, then the challenge picker opens).</summary>
        public event Action AbandonRunRequested;

        /// <summary>The player confirmed they want to leave for the main menu (the run in progress, if any, is scored as a defeat).</summary>
        public event Action MainMenuRequested;

        private RectTransform _root;
        private readonly System.Collections.Generic.List<(Text Label, string Text)> _confirmLabels = new System.Collections.Generic.List<(Text, string)>();

        public bool IsVisible
        {
            get { return _root != null && _root.gameObject.activeSelf; }
        }

        /// <param name="inRun">False on the main menu, where there is no run to abandon or save: those two buttons are left out.</param>
        public RectTransform Build(Transform parent, bool inRun = true)
        {
            var overlay = UIFactory.CreatePanel(parent, "SettingsOverlay", new Color(0f, 0f, 0f, 0.88f));
            _root = overlay.rectTransform;
            UIFactory.StretchFull(_root);

            var header = UIFactory.CreateText(_root, "Header", "Menu", 32, UITheme.TextOnBackground);
            header.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            header.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            header.rectTransform.anchoredPosition = new Vector2(0f, -36f);
            header.rectTransform.sizeDelta = new Vector2(1000f, 46f);

            if (inRun)
            {
                // Said before the player quits, not after: what Continue gives back.
                var saveHint = UIFactory.CreateText(_root, "SaveHint", "Save & quit resumes at the start of the current round, with a fresh hand (in the shop: as it was when it opened, purchases undone).", 15, UITheme.TextOnBackground);
                saveHint.color = UITheme.TextMutedOnBackground;
                saveHint.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                saveHint.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                saveHint.rectTransform.pivot = new Vector2(0.5f, 1f);
                saveHint.rectTransform.anchoredPosition = new Vector2(0f, -86f);
                saveHint.rectTransform.sizeDelta = new Vector2(900f, 44f);
                saveHint.raycastTarget = false;
            }

            BuildVolumeRow(_root, -140f, "Master Volume", UISprites.ScoreBarFill, VolumeSettings.MasterVolume, VolumeSettings.SetMasterVolume);
            BuildVolumeRow(_root, -230f, "Music Volume", UISprites.PiecesBarFill, VolumeSettings.MusicVolume, VolumeSettings.SetMusicVolume);
            BuildVolumeRow(_root, -320f, "SFX Volume", UISprites.SfxBarFill, VolumeSettings.SfxVolume, VolumeSettings.SetSfxVolume);

            var closeBtn = UIFactory.CreateButton(_root, "Close", "Resume", UISprites.ChooseButtonBackground, 18);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 30f);
            closeRect.sizeDelta = new Vector2(220f, MobileSetup.TouchMin(52f));
            closeBtn.onClick.AddListener(Hide);

            if (inRun)
            {
                BuildConfirmButton(_root, "AbandonRun", "Abandon run", 170f, () => { Hide(); if (AbandonRunRequested != null) AbandonRunRequested(); });
                BuildConfirmButton(_root, "MainMenu", "Save & quit", 240f, () => { Hide(); if (MainMenuRequested != null) MainMenuRequested(); }, requireConfirm: false);
            }
            BuildResetButton(_root);
            BuildMotionToggle(_root);

            _root.gameObject.SetActive(false);
            return _root;
        }

        /// <summary>"Reduced motion" switch: when on, the shop's pops, pulses, swells and shakes are skipped.</summary>
        private static void BuildMotionToggle(Transform parent)
        {
            var button = UIFactory.CreateButton(parent, "ReducedMotion", "", UISprites.CancelButtonBackground, 16);
            UIFactory.DisablePlants(button);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 310f);
            rect.sizeDelta = new Vector2(300f, MobileSetup.TouchMin(46f));
            var label = button.GetComponentInChildren<Text>();
            label.text = Loc.T(MotionSettings.Reduced ? "settings.motionOn" : "settings.motionOff");
            button.onClick.AddListener(() =>
            {
                MotionSettings.Reduced = !MotionSettings.Reduced;
                label.text = Loc.T(MotionSettings.Reduced ? "settings.motionOn" : "settings.motionOff");
            });
        }

        /// <summary>A two-click button (the first click arms it for a few seconds, the second confirms), for actions that throw progress away.</summary>
        private void BuildConfirmButton(Transform parent, string name, string text, float y, Action onConfirmed, bool requireConfirm = true)
        {
            var button = UIFactory.CreateButton(parent, name, text, UISprites.CancelButtonBackground, 16);
            UIFactory.DisablePlants(button);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(220f, MobileSetup.TouchMin(46f));
            var label = button.GetComponentInChildren<Text>();
            _confirmLabels.Add((label, text));
            bool armed = false;
            float armedAt = 0f;
            button.onClick.AddListener(() =>
            {
                if (requireConfirm && (!armed || Time.unscaledTime - armedAt > 4f))
                {
                    armed = true;
                    armedAt = Time.unscaledTime;
                    label.text = "Sure? Click again";
                    return;
                }
                armed = false;
                label.text = text;
                onConfirmed();
            });
        }

        private void ResetConfirmLabels()
        {
            for (int i = 0; i < _confirmLabels.Count; i++)
            {
                _confirmLabels[i].Label.text = _confirmLabels[i].Text;
            }
        }

        /// <summary>Two-click "Reset data": wipes saved stats and every preference (tutorial seen, volume), then reloads the scene so the next launch behaves like a first one.</summary>
        private static void BuildResetButton(Transform parent)
        {
            var resetBtn = UIFactory.CreateButton(parent, "ResetData", "Reset data", UISprites.CancelButtonBackground, 16);
            UIFactory.DisablePlants(resetBtn);
            var rect = resetBtn.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 100f);
            rect.sizeDelta = new Vector2(220f, MobileSetup.TouchMin(46f));
            var label = resetBtn.GetComponentInChildren<Text>();
            bool armed = false;
            float armedAt = 0f;
            resetBtn.onClick.AddListener(() =>
            {
                if (!armed || Time.unscaledTime - armedAt > 4f)
                {
                    armed = true;
                    armedAt = Time.unscaledTime;
                    label.text = "Sure? Click again";
                    return;
                }
                new MetaStatsFileStore().Delete();
                RunSaveStore.Delete();
                CollectionTracker.Reset();
                GamePrefs.DeleteAll();
                GamePrefs.Save();
                VolumeSettings.ResetToDefaults();
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            });
        }

        /// <summary>One label + slider + live "NN%" readout, all in the same 3-column grid every row shares (see LabelColumnX/ControlColumnX/ValueColumnX) so the 3 volume rows and the checkbox row line up.</summary>
        private void BuildVolumeRow(Transform parent, float y, string label, Sprite fillSprite, float initialValue, Action<float> onChanged)
        {
            var labelText = UIFactory.CreateText(parent, label + "Label", label, 18, UITheme.TextOnBackground, TextAnchor.MiddleRight);
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

            var valueLabel = UIFactory.CreateText(parent, label + "Value", Mathf.RoundToInt(initialValue * 100) + "%", 16, UITheme.TextMutedOnBackground);
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

        /// <summary>Standard 3-part UnityEngine.UI.Slider anatomy (background/fill/handle).</summary>
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

            // Plain (unsliced) square image: the Slider only drives the handle's X anchors, so Y is pinned
            // to the middle with a fixed height — a vertically stretched anchor would distort the flower.
            var handle = UIFactory.CreatePanel(handleArea, "Handle", Color.white);
            handle.sprite = UISprites.SliderHandle;
            handle.type = Image.Type.Simple;
            handle.preserveAspect = true;
            handle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(34f, 34f);

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

        public void Show()
        {
            ResetConfirmLabels();
            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        public void Toggle()
        {
            ResetConfirmLabels();
            _root.gameObject.SetActive(!_root.gameObject.activeSelf);
        }
    }
}
