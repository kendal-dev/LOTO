using System.Collections.Generic;
using Loto.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Loto
{
    /// <summary>
    /// The big text panel (1.5 m x 0.7 m, read from 1.6 m): student, time, the four steps, and a
    /// notice: yellow for an error that lets the student go on, red when the simulation stopped,
    /// green when the sequence was completed. The scene builder creates the texts and assigns them.
    /// </summary>
    public class StatusPanel : MonoBehaviour
    {
        public const int PixelsWide = 1500;
        public const int PixelsHigh = 700;
        public const float MetersWide = 1.5f;
        public const float MetersHigh = 0.7f;

        public static readonly Color PlateNormal = new Color(0.10f, 0.12f, 0.16f);
        public static readonly Color PlateError = new Color(0.55f, 0.07f, 0.07f);
        public static readonly Color PlateDone = new Color(0.06f, 0.36f, 0.16f);
        public static readonly Color TextMain = new Color(0.96f, 0.96f, 0.96f);
        public static readonly Color TextSoft = new Color(0.72f, 0.76f, 0.82f);
        public static readonly Color TextWarn = new Color(1.00f, 0.82f, 0.25f);

        static readonly string[] Steps =
        {
            "Baja la palanca: corta la energía",
            "Pon tu candado en la palanca",
            "Aprieta VERIFICAR: prueba que no arranca",
            "Recién ahora: toca la máquina",
        };

        public LotoSession session;
        public MeshRenderer backPlate;
        public Text header;
        public Text clock;
        public Text steps;
        public Text notice;

        string _serverExplanation;
        string _hint;
        float _hintUntil;
        Material _plateMaterial;
        int _shownSecond = -1;

        void OnEnable()
        {
            if (session == null) return;
            session.Changed += Redraw;
            session.ResetDone += ClearExplanation;
        }

        void OnDisable()
        {
            if (session == null) return;
            session.Changed -= Redraw;
            session.ResetDone -= ClearExplanation;
        }

        void Start()
        {
            if (backPlate != null) _plateMaterial = backPlate.material;   // own copy: the color changes at run time
            Redraw();
        }

        void Update()
        {
            if (_hint != null && Time.unscaledTime > _hintUntil) { _hint = null; Redraw(); }
            int second = Mathf.FloorToInt(session.Seconds);
            if (second == _shownSecond) return;
            _shownSecond = second;
            if (clock != null) clock.text = (second / 60) + ":" + (second % 60).ToString("00");
        }

        /// <summary>The server's explanation for the attempt that just ended (AttemptSender calls this).</summary>
        public void ShowServerExplanation(string text)
        {
            if (string.IsNullOrEmpty(text) || !session.Ended) return;
            // The notice box holds about 6 lines: a long text would push "Toca REINTENTAR" out of it.
            _serverExplanation = text.Length > 220 ? text.Substring(0, 217) + "..." : text;
            Redraw();
        }

        void ClearExplanation() { _serverExplanation = null; _hint = null; }

        /// <summary>A short yellow notice that is not a Referee result (VERIFY while the machine still coasts).</summary>
        public void ShowHint(string text, float seconds)
        {
            _hint = text;
            _hintUntil = Time.unscaledTime + seconds;
            Redraw();
        }

        void Redraw()
        {
            Referee referee = session.Referee;
            if (header != null) header.text = "BLOQUEO Y ETIQUETADO  ·  " + session.StudentName;

            if (steps != null)
            {
                int current = referee.CurrentStep;
                var text = new System.Text.StringBuilder();
                for (int i = 0; i < Steps.Length; i++)
                {
                    int number = i + 1;
                    if (number < current) text.Append("<color=#7CE08A>[ OK ]  " + number + ". " + Steps[i] + "</color>");
                    // When the attempt stopped, CurrentStep is exactly the step the student skipped.
                    else if (number == current && referee.Failed)
                        text.Append("<color=#FF6B6B><b>[ X ]  " + number + ". " + Steps[i] + "</b><size=30>  (TE LO SALTASTE)</size></color>");
                    else if (number == current) text.Append("<b>[  >  ]  " + number + ". " + Steps[i] + "</b>");
                    else text.Append("<color=#AEB6C2>[      ]  " + number + ". " + Steps[i] + "</color>");
                    if (i < Steps.Length - 1) text.Append('\n');
                }
                // Review after the attempt: what the student actually did, in order (the 4 lines above are the right order).
                if (session.Ended)
                    text.Append("\n<size=26>" + (referee.Completed ? "<color=#7CE08A>" : "<color=#FF9A9A>") +
                        "Tu orden: " + Order(referee.History) + "</color></size>");
                steps.text = text.ToString();
            }

            Color plate = PlateNormal;
            string message = "";
            Color messageColor = TextSoft;
            if (referee.Failed)
            {
                plate = PlateError;
                messageColor = TextMain;
                string explain = string.IsNullOrEmpty(_serverExplanation) ? ErrorTexts.Explain(referee.FailCode) : _serverExplanation;
                message = "<b>ALTO  ·  " + ErrorTexts.Title(referee.FailCode) + "</b>\n" + explain + "\n<b>Toca REINTENTAR en el tablero.</b>";
            }
            else if (referee.Completed)
            {
                plate = PlateDone;
                messageColor = TextMain;
                message = "<b>SECUENCIA COMPLETA</b> en " + session.Seconds.ToString("F0") + " s.\nLa máquina quedó sin energía, con tu candado y verificada: ahora sí es seguro tocarla.";
            }
            else if (_hint != null)
            {
                messageColor = TextWarn;
                message = _hint;
            }
            else if (!string.IsNullOrEmpty(session.WarningCode))
            {
                messageColor = TextWarn;
                message = "<b>" + ErrorTexts.Title(session.WarningCode) + "</b>\n" + ErrorTexts.Explain(session.WarningCode);
            }
            else if (referee.CurrentStep == 1)
            {
                message = "La máquina está encendida. Sigue los pasos en orden.";
            }

            if (notice != null)
            {
                notice.text = message;
                notice.color = messageColor;
            }
            if (_plateMaterial != null)
            {
                if (_plateMaterial.HasProperty("_BaseColor")) _plateMaterial.SetColor("_BaseColor", plate);
                if (_plateMaterial.HasProperty("_Color")) _plateMaterial.SetColor("_Color", plate);
            }
        }

        /// <summary>Short Spanish name of an action for the "Tu orden" line (ASCII and Latin-1 only: the built-in font).</summary>
        static string Short(LotoAction action)
        {
            switch (action)
            {
                case LotoAction.LeverOff: return "Apagar";
                case LotoAction.LockOn: return "Candado";
                case LotoAction.Verify: return "Verificar";
                case LotoAction.TouchMachine: return "Tocar";
                case LotoAction.LockOff: return "Sacar candado";
                case LotoAction.LeverOn: return "Subir palanca";
                default: return action.ToString();
            }
        }

        /// <summary>The last 5 actions joined with " > ", with "... " in front when there were more.</summary>
        static string Order(IReadOnlyList<LotoAction> history)
        {
            if (history == null || history.Count == 0) return "(nada)";
            int from = history.Count > 5 ? history.Count - 5 : 0;
            var text = new System.Text.StringBuilder(from > 0 ? "... " : "");
            for (int i = from; i < history.Count; i++)
            {
                if (i > from) text.Append(" > ");
                text.Append(Short(history[i]));
            }
            return text.ToString();
        }

        // --- Builders shared with the scene builder (labels on the board use them too) ---

        /// <summary>A world-space canvas of pixelsWide x pixelsHigh pixels that measures metersWide across.</summary>
        public static RectTransform MakeCanvas(Transform parent, string name, int pixelsWide, int pixelsHigh, float metersWide)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObject.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(pixelsWide, pixelsHigh);
            rect.localScale = Vector3.one * (metersWide / pixelsWide);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        /// <summary>A text box placed in pixels from the canvas' top-left corner.</summary>
        public static Text AddText(RectTransform parent, string name, string content, Font font, Material material,
            int size, FontStyle style, Color color, TextAnchor anchor, float x, float y, float w, float h)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            var text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = content;
            if (material != null) text.material = material;
            return text;
        }
    }
}
