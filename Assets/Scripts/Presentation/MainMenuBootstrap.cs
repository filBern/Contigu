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
    /// SceneManager.LoadScene once "Jouer" is clicked. Mirrors
    /// GameBootstrap's own EventSystem/Canvas/CanvasScaler/
    /// AnimatedBackgroundView setup exactly, so the two scenes look and
    /// scale identically and the transition between them isn't jarring.
    /// </summary>
    public sealed class MainMenuBootstrap : MonoBehaviour
    {
        private const string GameplaySceneName = "Main";

        private MainMenuView _mainMenuView;
        private AnimatedBackgroundView _animatedBackgroundView;

        private void Awake()
        {
            EnsureEventSystem();
            var canvasRect = BuildCanvas();

            _mainMenuView = gameObject.AddComponent<MainMenuView>();
            _mainMenuView.Build(canvasRect);
            _mainMenuView.PlayClicked += OnPlayClicked;
            _mainMenuView.Show();
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene(GameplaySceneName);
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
