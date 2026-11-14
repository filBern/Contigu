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
        private CollectionView _collectionView;
        private AnimatedBackgroundView _animatedBackgroundView;
        private RectTransform _canvasRect;
        // Guards against a double-click re-triggering (and stacking) the cover sweep while it's already playing.
        private bool _isTransitioning;

        private void Awake()
        {
            EnsureEventSystem();
            _canvasRect = BuildCanvas();

            _mainMenuView = gameObject.AddComponent<MainMenuView>();
            _mainMenuView.Build(_canvasRect);

            // Its own SettingsView instance — safe to build a second one alongside GameBootstrap's, since
            // Settings carries no state beyond its static PlayerPrefs-backed classes. Built after MainMenuView
            // so it renders as a later sibling, on top of the menu's buttons rather than underneath them.
            _settingsView = gameObject.AddComponent<SettingsView>();
            _settingsView.Build(_canvasRect, false);

            // The Collection and its hover tooltip, above the menu's buttons.
            var tooltip = gameObject.AddComponent<TooltipView>();
            tooltip.Build(_canvasRect);
            _collectionView = gameObject.AddComponent<CollectionView>();
            _collectionView.Build(_canvasRect, tooltip);
            _mainMenuView.CollectionClicked += _collectionView.Show;

            _mainMenuView.PlayClicked += OnPlayClicked;
            _mainMenuView.SettingsClicked += _settingsView.Toggle;
            _mainMenuView.ExitClicked += OnExitClicked;
            _mainMenuView.Show();
        }

        /// <summary>Covers the screen with a diagonal sweep of colored tiles (see MainMenuTransitionView) before actually swapping scenes, instead of cutting straight to gameplay. The sweep survives the scene load (DontDestroyOnLoad) and continues on the other side — see GameBootstrap.Awake's own MainMenuTransitionView.PendingReveal check.</summary>
        private void OnPlayClicked()
        {
            if (_isTransitioning)
            {
                return;
            }
            _isTransitioning = true;
            MainMenuTransitionView.PlayCoverSweep(_canvasRect, () => SceneManager.LoadScene(GameplaySceneName));
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
            canvasGo.AddComponent<CanvasAspectFit>();

            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = UIFactory.CreatePanel(canvasGo.transform, "Background", UITheme.Background);
            UIFactory.StretchFull(bg.rectTransform);

            _animatedBackgroundView = gameObject.AddComponent<AnimatedBackgroundView>();
            _animatedBackgroundView.Build(canvasGo.transform);

            return canvasGo.GetComponent<RectTransform>();
        }
    }
}
