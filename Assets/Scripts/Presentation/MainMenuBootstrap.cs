using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Boots the dedicated main menu scene (Assets/Scenes/MainMenu.unity), loaded first and handing off to
    /// the gameplay scene via SceneManager.LoadScene once "Play" is clicked. Mirrors GameBootstrap's own
    /// EventSystem/Canvas/CanvasScaler/AnimatedBackgroundView setup so the two scenes look and scale identically.
    /// </summary>
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        private const string GameplaySceneName = "Main";

        private MainMenuView _mainMenuView;
        private SettingsView _settingsView;
        private AnimatedBackgroundView _animatedBackgroundView;

        private void Awake()
        {
            EnsureEventSystem();
            var canvasRect = BuildCanvas();

            _mainMenuView = gameObject.AddComponent<MainMenuView>();
            _mainMenuView.Build(canvasRect);

            // Its own SettingsView instance — safe to build a second one alongside GameBootstrap's, since
            // Settings carries no state beyond its static PlayerPrefs-backed classes. Built after MainMenuView
            // so it renders as a later sibling, on top of the menu's buttons rather than underneath them.
            _settingsView = gameObject.AddComponent<SettingsView>();
            _settingsView.Build(canvasRect);

            _mainMenuView.PlayClicked += OnPlayClicked;
            _mainMenuView.SettingsClicked += _settingsView.Toggle;
            _mainMenuView.ExitClicked += OnExitClicked;
            _mainMenuView.Show();
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene(GameplaySceneName);
        }

        private static void OnExitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }

        private RectTransform BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 800f);
            scaler.matchWidthOrHeight = 1f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = UIFactory.CreatePanel(canvasGo.transform, "Background", UITheme.Background);
            UIFactory.StretchFull(bg.rectTransform);

            _animatedBackgroundView = gameObject.AddComponent<AnimatedBackgroundView>();
            _animatedBackgroundView.Build(canvasGo.transform);

            return canvasGo.GetComponent<RectTransform>();
        }
    }
}
