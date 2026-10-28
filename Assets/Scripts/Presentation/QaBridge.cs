using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    [Serializable] public sealed class QaButton { public string name; public string text; public float x; public float y; public float w; public float h; public bool visible; }
    [Serializable] public sealed class QaText { public string text; public float x; public float y; public float w; public float h; }
    [Serializable] public sealed class QaToken { public string shape; public string color; public string[] traits; public int rotation; public bool locked; }
    [Serializable] public sealed class QaMove { public int slot; public int x; public int y; }
    [Serializable] public sealed class QaEnemy { public string id; public int hp; public int max; public bool dead; }
    [Serializable] public sealed class QaShopSlot { public string kind; public string name; public int price; public bool sold; }
    [Serializable] public sealed class QaPoint { public float x; public float y; }

    /// <summary>What the game reports about the run in progress (filled by GameBootstrap; null on screens with no run, like the main menu).</summary>
    [Serializable]
    public sealed class QaGame
    {
        public string state;
        public string challenge;
        public int round;
        public int roundScore;
        public int quota;
        public int total;
        public int pollen;
        public int shuffles;
        public int seedsLeft;
        public string weather;
        public bool sequence;
        public string pending;
        public QaToken[] hand;
        public QaMove[] moves;
        public QaEnemy[] enemies;
        public string[] fertilizers;
        public QaShopSlot[] blister;
        public QaShopSlot[] workshop;
        public string[] grid;
        public QaPoint[] cells;
    }

    [Serializable]
    public sealed class QaState
    {
        public int tick;
        public string scene;
        public int screenW;
        public int screenH;
        public float timeScale;
        public QaButton[] buttons;
        public QaText[] texts;
        public QaGame game;
    }

    [Serializable]
    public sealed class QaCommand
    {
        public int id;
        public string cmd;
        public float x;
        public float y;
        public string text;
        public string arg;
    }

    /// <summary>
    /// QA hook for the WebGL build (only when the page is opened with ?qa=1): publishes the screen's buttons, texts and the run's state to
    /// <c>window.__qa.stateJson</c> and executes commands queued in <c>window.__qa.queue</c> (real pointer clicks through the event system, so
    /// the game's own handlers run exactly as for a player). Everything is a no-op outside a WebGL page opened that way.
    /// </summary>
    public sealed class QaBridge : MonoBehaviour
    {
        /// <summary>Set by GameBootstrap while it is alive.</summary>
        public static Func<QaGame> GameState;

        /// <summary>Set by GameBootstrap: debug commands that need the game's own objects (unlockAll, forceRoundWin, grantPollen...). Returns a short result text.</summary>
        public static Func<string, string, string> GameCommand;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int QaBridge_Enabled();
        [DllImport("__Internal")] private static extern void QaBridge_Publish(string json);
        [DllImport("__Internal")] private static extern void QaBridge_Result(string json);
        [DllImport("__Internal")] private static extern string QaBridge_Poll();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
#if !CONTIGU_DEBUG
            // A production build never opens this hook, whatever the URL says: it can unlock everything and hand out stars.
            return;
#else
            if (QaBridge_Enabled() != 1)
            {
                return;
            }
            var go = new GameObject("QaBridge");
            DontDestroyOnLoad(go);
            go.AddComponent<QaBridge>();
#endif
        }
#endif

        private GameObject _hovered;
#if UNITY_WEBGL && !UNITY_EDITOR
        // Only the web build publishes its state on a timer (the editor has no page to publish to).
        private const float PublishInterval = 0.25f;
        private float _nextPublish;
        private bool _publishNow = true;
#endif

        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            for (int guard = 0; guard < 8; guard++)
            {
                string json = QaBridge_Poll();
                if (string.IsNullOrEmpty(json))
                {
                    break;
                }
                QaBridge_Result(Execute(JsonUtility.FromJson<QaCommand>(json)));
                _publishNow = true;
            }
            if (_publishNow || Time.unscaledTime >= _nextPublish)
            {
                _publishNow = false;
                _nextPublish = Time.unscaledTime + PublishInterval;
                QaBridge_Publish(BuildStateJson());
            }
#endif
        }

        public static string BuildStateJson()
        {
            var state = new QaState
            {
                tick = ++_staticTick,
                scene = SceneManager.GetActiveScene().name,
                screenW = Screen.width,
                screenH = Screen.height,
                timeScale = Time.timeScale,
                buttons = CollectButtons(),
                texts = CollectTexts(),
                game = GameState != null ? GameState() : null
            };
            return JsonUtility.ToJson(state);
        }

        private static int _staticTick;

        private string Execute(QaCommand c)
        {
            string result = "ok";
            try
            {
                switch (c.cmd)
                {
                    case "click":
                        result = ClickAt(FromTopLeft(c.x, c.y)) ? "ok" : "nothing there";
                        break;
                    case "hover":
                        result = HoverAt(FromTopLeft(c.x, c.y)) ? "ok" : "nothing there";
                        break;
                    case "clickText":
                        result = ClickButtonWithText(c.text) ? "ok" : "no button with that text";
                        break;
                    default:
                        result = GameCommand != null ? GameCommand(c.cmd, c.arg) : "unknown command";
                        break;
                }
            }
            catch (Exception e)
            {
                result = "exception: " + e.GetType().Name + ": " + e.Message;
            }
            return "{\"id\":" + c.id + ",\"result\":\"" + result.Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ") + "\"}";
        }

        private static Vector2 FromTopLeft(float x, float y)
        {
            return new Vector2(x, Screen.height - y);
        }

        private static Vector2 ToTopLeft(Vector2 screen)
        {
            return new Vector2(screen.x, Screen.height - screen.y);
        }

        private static RaycastResult? TopHit(Vector2 screenPoint, out PointerEventData ped)
        {
            ped = null;
            var es = EventSystem.current;
            if (es == null)
            {
                return null;
            }
            ped = new PointerEventData(es) { position = screenPoint, button = PointerEventData.InputButton.Left, clickCount = 1 };
            var hits = new List<RaycastResult>();
            es.RaycastAll(ped, hits);
            return hits.Count > 0 ? hits[0] : (RaycastResult?)null;
        }

        private bool HoverAt(Vector2 screenPoint)
        {
            PointerEventData ped;
            var hit = TopHit(screenPoint, out ped);
            if (_hovered != null)
            {
                ExecuteEvents.ExecuteHierarchy(_hovered, ped ?? new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
                _hovered = null;
            }
            if (!hit.HasValue)
            {
                return false;
            }
            ped.pointerCurrentRaycast = hit.Value;
            _hovered = hit.Value.gameObject;
            ExecuteEvents.ExecuteHierarchy(_hovered, ped, ExecuteEvents.pointerEnterHandler);
            return true;
        }

        private bool ClickAt(Vector2 screenPoint)
        {
            PointerEventData ped;
            var hit = TopHit(screenPoint, out ped);
            if (!hit.HasValue)
            {
                return false;
            }
            var go = hit.Value.gameObject;
            ped.pointerCurrentRaycast = hit.Value;
            ped.pointerPressRaycast = hit.Value;
            ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerEnterHandler);
            _hovered = go;
            var pressed = ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerDownHandler);
            ped.pointerPress = pressed != null ? pressed : go;
            ped.rawPointerPress = go;
            ped.eligibleForClick = true;
            ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(go, ped, ExecuteEvents.pointerClickHandler);
            return true;
        }

        private bool ClickButtonWithText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }
            foreach (var b in CollectButtons())
            {
                if (b.visible && b.text != null && b.text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ClickAt(FromTopLeft(b.x, b.y));
                }
            }
            return false;
        }

        private static string Clean(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : Regex.Replace(s, "<.*?>", "").Trim();
        }

        private static Rect ScreenRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float minX = Mathf.Min(corners[0].x, corners[2].x);
            float maxX = Mathf.Max(corners[0].x, corners[2].x);
            float minY = Mathf.Min(corners[0].y, corners[2].y);
            float maxY = Mathf.Max(corners[0].y, corners[2].y);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>Screen-space center (top-left origin) of a UI element, for the game's own state reporting.</summary>
        public static QaPoint CenterOf(RectTransform rt)
        {
            if (rt == null)
            {
                return new QaPoint { x = -1f, y = -1f };
            }
            var r = ScreenRect(rt);
            var p = ToTopLeft(r.center);
            return new QaPoint { x = Mathf.Round(p.x), y = Mathf.Round(p.y) };
        }

        private static QaButton[] CollectButtons()
        {
            var list = new List<QaButton>();
            var buttons = FindObjectsOfType<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                if (!b.isActiveAndEnabled || !b.interactable)
                {
                    continue;
                }
                var rt = (RectTransform)b.transform;
                var r = ScreenRect(rt);
                if (r.width < 2f || r.height < 2f || r.xMax < 0f || r.yMax < 0f || r.xMin > Screen.width || r.yMin > Screen.height)
                {
                    continue;
                }
                PointerEventData ped;
                var hit = TopHit(r.center, out ped);
                bool visible = hit.HasValue && (hit.Value.gameObject == b.gameObject || hit.Value.gameObject.transform.IsChildOf(b.transform));
                var label = b.GetComponentInChildren<Text>();
                var p = ToTopLeft(r.center);
                list.Add(new QaButton { name = b.name, text = label != null ? Clean(label.text) : "", x = Mathf.Round(p.x), y = Mathf.Round(p.y), w = Mathf.Round(r.width), h = Mathf.Round(r.height), visible = visible });
            }
            return list.ToArray();
        }

        private static QaText[] CollectTexts()
        {
            var list = new List<QaText>();
            var texts = FindObjectsOfType<Text>();
            for (int i = 0; i < texts.Length && list.Count < 160; i++)
            {
                var t = texts[i];
                if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text) || t.canvasRenderer.GetInheritedAlpha() < 0.05f)
                {
                    continue;
                }
                var r = ScreenRect(t.rectTransform);
                if (r.xMax < 0f || r.yMax < 0f || r.xMin > Screen.width || r.yMin > Screen.height)
                {
                    continue;
                }
                var p = ToTopLeft(r.center);
                string clean = Clean(t.text);
                if (clean.Length == 0)
                {
                    continue;
                }
                list.Add(new QaText { text = clean, x = Mathf.Round(p.x), y = Mathf.Round(p.y), w = Mathf.Round(r.width), h = Mathf.Round(r.height) });
            }
            return list.ToArray();
        }
    }
}
