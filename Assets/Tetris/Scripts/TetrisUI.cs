using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TetrisGame
{
    public sealed class TetrisUI : MonoBehaviour
    {
        TetrisGame game;
        Text scoreText;
        Text levelText;
        Text linesText;
        Text overlayTitle;
        Text overlayHint;
        GameObject overlay;
        GameObject restartButton;

        public void Build(TetrisGame tetrisGame)
        {
            game = tetrisGame;

            Font font = LoadFont();
            CreateHud(font);
            CreateOverlayCanvas(font);
            CreateEventSystem();
        }

        public void Render()
        {
            if (game == null)
            {
                return;
            }

            scoreText.text = game.Score.ToString("D6");
            levelText.text = game.Level.ToString();
            linesText.text = game.Lines.ToString();

            bool showOverlay = game.State == PlayState.Paused || game.State == PlayState.GameOver;
            overlay.SetActive(showOverlay);
            restartButton.SetActive(game.State == PlayState.GameOver);

            if (game.State == PlayState.GameOver)
            {
                overlayTitle.text = "ИГРА ОКОНЧЕНА";
                overlayHint.text = "Счёт: " + game.Score;
            }
            else if (game.State == PlayState.Paused)
            {
                overlayTitle.text = "ПАУЗА";
                overlayHint.text = "P / Esc — продолжить";
            }
        }

        void CreateHud(Font font)
        {
            Canvas canvas = CreateCanvas("HudCanvas", RenderMode.WorldSpace, 10, false);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(420f, 980f);
            canvas.transform.position = new Vector3(13.35f, 9.6f, 0f);
            canvas.transform.localScale = Vector3.one * 0.0125f;
            canvas.worldCamera = Camera.main;

            RectTransform panel = CreatePanel(canvas.transform, "Hud", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 980f), new Color(0.08f, 0.09f, 0.14f, 0.0f));

            CreateLabel(panel, "Title", "ТЕТРИС", font, 58, FontStyle.Bold, new Vector2(0f, 450f), new Color(0.93f, 0.95f, 1f));
            CreateLabel(panel, "NextCaption", "СЛЕДУЮЩАЯ", font, 24, FontStyle.Bold, new Vector2(0f, 410f), new Color(0.72f, 0.76f, 0.88f));

            scoreText = CreateValueBlock(panel, font, "СЧЁТ", "000000", new Vector2(0f, -20f));
            levelText = CreateValueBlock(panel, font, "УРОВЕНЬ", "1", new Vector2(0f, -150f));
            linesText = CreateValueBlock(panel, font, "ЛИНИИ", "0", new Vector2(0f, -280f));

            CreateLabel(panel, "Help", "← →  движение\n↓     ускорить\n↑ / X  поворот\nZ      против часовой\nПробел  сброс вниз\nP / Esc пауза\nR      заново", font, 22, FontStyle.Normal, new Vector2(0f, -420f), new Color(0.64f, 0.68f, 0.80f));
        }

        void CreateOverlayCanvas(Font font)
        {
            Canvas canvas = CreateCanvas("OverlayCanvas", RenderMode.ScreenSpaceOverlay, 30, true);
            overlay = CreatePanel(canvas.transform, "Overlay", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f), new Color(0.02f, 0.02f, 0.04f, 0.55f)).gameObject;

            RectTransform box = CreatePanel(overlay.transform, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 280f), new Color(0.07f, 0.08f, 0.12f, 0.96f));
            overlayTitle = CreateLabel(box, "OverlayTitle", "", font, 42, FontStyle.Bold, new Vector2(0f, 70f), Color.white);
            overlayHint = CreateLabel(box, "OverlayHint", "", font, 24, FontStyle.Normal, new Vector2(0f, 10f), new Color(0.82f, 0.84f, 0.92f));
            restartButton = CreateButton(box, font, "Заново", new Vector2(0f, -70f), game.Restart);
            overlay.SetActive(false);
        }

        Text CreateValueBlock(RectTransform parent, Font font, string caption, string value, Vector2 position)
        {
            CreateLabel(parent, caption + "Caption", caption, font, 22, FontStyle.Bold, position + new Vector2(0f, 42f), new Color(0.72f, 0.76f, 0.88f));
            return CreateLabel(parent, caption + "Value", value, font, 48, FontStyle.Bold, position, Color.white);
        }

        static Canvas CreateCanvas(string name, RenderMode mode, int order, bool scaleWithScreen)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = mode;
            canvas.sortingOrder = order;

            if (scaleWithScreen)
            {
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void CreateEventSystem()
        {
            if (FindUi<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        static T FindUi<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = color.a > 0.01f;
            return rect;
        }

        static GameObject CreateButton(Transform parent, Font font, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreatePanel(parent, "RestartButton", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(220f, 52f), new Color(0.27f, 0.45f, 0.96f, 1f));
            var button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.40f, 0.58f, 1f);
            colors.pressedColor = new Color(0.18f, 0.32f, 0.78f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            Text text = CreateLabel(rect, "Label", label, font, 24, FontStyle.Bold, Vector2.zero, Color.white);
            text.raycastTarget = false;
            return rect.gameObject;
        }

        static Text CreateLabel(Transform parent, string name, string content, Font font, int fontSize, FontStyle style, Vector2 anchoredPosition, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(380f, 100f);

            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        static Font LoadFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
    }
}
