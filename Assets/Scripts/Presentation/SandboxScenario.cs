using UnityEngine;
using UnityEngine.SceneManagement;

namespace Contigu.Presentation
{
    /// <summary>The screens the demo scenes can open, already filled with a rich run (see GameBootstrap.ApplySandbox).</summary>
    public enum SandboxScreen
    {
        Board,
        Shop,
        RoundSummary,
        FertilizerPack,
        SproutPack,
        SproutSeedChoice,
        Forge,
        Compost,
        MasteryPack,
        SeedPack,
        Transplant,
        Victory,
        Defeat,
        ChallengeSelect
    }

    /// <summary>
    /// Put on the GameBootstrap object of a demo scene: opens <see cref="screen"/> with sample data, so a screen can be looked at (and its
    /// values tuned in Assets/Resources/UITweaks.asset) without playing a run to get there. Press Play, edit UITweaks in the Inspector: the scene
    /// reloads by itself half a second after each change (R reloads it by hand).
    /// </summary>
    public sealed class SandboxScenario : MonoBehaviour
    {
        [Tooltip("L'écran à ouvrir, rempli d'exemples.")] public SandboxScreen screen = SandboxScreen.Board;
        [Tooltip("Recharge la scène toute seule quand une valeur de UITweaks change.")] public bool reloadOnTweak = true;

        private int _seenVersion;
        private float _reloadAt = -1f;

        private void Start()
        {
            _seenVersion = UITweaks.EditVersion;
#if UNITY_EDITOR || CONTIGU_DEBUG
            var game = FindObjectOfType<GameBootstrap>();
            if (game != null)
            {
                game.ApplySandbox(screen);
            }
#endif
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Reload();
                return;
            }
            if (!reloadOnTweak)
            {
                return;
            }
            if (UITweaks.EditVersion != _seenVersion)
            {
                _seenVersion = UITweaks.EditVersion;
                _reloadAt = Time.unscaledTime + 0.5f;
            }
            if (_reloadAt > 0f && Time.unscaledTime >= _reloadAt)
            {
                _reloadAt = -1f;
                Reload();
            }
        }

        private static void Reload()
        {
            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // The demo scenes are not in the build settings: the editor loads them by path.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }
    }
}
