using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Boots the dedicated main menu scene (Assets/Scenes/MainMenu.unity) —
    /// spec extension, explicit request: "j'aurais aimé qu'il soit dans une
    /// scene a part". The main menu used to be built alongside every other
    /// screen inside the gameplay scene and just shown/hidden like an
    /// overlay (see GameBootstrap); it's now the sole thing in its own
    /// scene, loaded first, handing off to the gameplay scene via
    /// SceneManager.LoadScene once "Play" is clicked. Mirrors
    /// GameBootstrap's own EventSystem/Canvas/CanvasScaler/
    /// AnimatedBackgroundView setup exactly, so the two scenes look and
    /// scale identically and the transition between them isn't jarring.
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

            // Its own SettingsView instance — Settings is a plain,
            // self-contained overlay (Master/Music/SFX volume + colorblind
            // toggle, all persisted via static PlayerPrefs-backed classes),
            // so building a second one here alongside GameBootstrap's is
            // safe: neither carries any state of its own beyond what those
            // static classes already own. Built AFTER MainMenuView (matches
            // GameBootstrap's own ordering) so it renders as a later sibling
            // — on TOP of the menu's title/Play/Settings/Exit buttons —
            // instead of underneath them: those buttons were visibly
            // (and clickably) poking through the settings overlay before
            // this ordering fix (explicit report, from a screenshot: "il
            // faut hide certains éléments du menu lorsqu'on est dans les
            // settings").
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
