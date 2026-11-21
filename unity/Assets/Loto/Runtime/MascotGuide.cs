using System.Collections.Generic;
using Loto.Core;
using UnityEngine;

namespace Loto
{
    /// <summary>
    /// "Chispa", the safety assistant: a small robot that floats beside the student, looks at them and talks in a
    /// speech bubble at each moment of the attempt (start, each step, each error, stored energy, the end). When the
    /// server sends an explanation written by an AI model (StatusPanel.ExplanationShown), Chispa says it too.
    /// Voice: if Resources/Voice/&lt;key&gt; has a clip (recorded with a local voice model, see docs/equipo/VOZ-MASCOTA.md),
    /// it plays; otherwise a short chirp. It never decides anything: the Referee does.
    /// </summary>
    public class MascotGuide : MonoBehaviour
    {
        public LotoSession session;
        public Transform body;                 // bobs and turns to the student
        public Renderer[] eyes;                // glow while talking, blink
        public UnityEngine.UI.Text bubbleText;
        public GameObject bubble;
        public AudioSource voice;
        public Vector3 offsetFromHead = new Vector3(-0.45f, -0.18f, 0.55f);   // left, a bit low, in front
        public float bubbleSeconds = 6f;

        static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
        {
            { "intro", "¡Hola! Soy Chispa. Antes de tocar una máquina: apagar, candado, verificar y recién tocar." },
            { "intro_MOLINO", "Este molino tiene una polea pesada. Ojo: sigue girando un rato después de apagarlo." },
            { "intro_PRENSA", "Esta prensa guarda presión en el aceite. Apagarla no alcanza: hay que purgar." },
            { "intro_NEUMÁTICA", "Esta estación guarda aire comprimido. Apagar no alcanza: hay que purgar." },
            { "ok1", "Bien, palanca abajo. Ahora tu candado en el punto de bloqueo." },
            { "ok2", "Candado puesto. Nadie puede encenderla. Ahora verifica." },
            { "ok3", "No arranca: quedó sin energía. Ahora sí puedes tocarla." },
            { "done", "¡Excelente! Secuencia completa. Así se trabaja seguro." },
            { "hint", "Espera: todavía hay energía guardada. Mira la máquina antes de verificar." },
            { "hint_PRENSA", "Todavía hay presión. Toca PURGAR y espera que el manómetro marque cero." },
            { "hint_NEUMÁTICA", "Todavía hay aire en la línea. Toca PURGAR y espera el cero." },
            { "fail", "¡Alto! Te saltaste un paso. Mira el panel: te marca cuál." },
            { "warn", "Cuidado, ese paso no va todavía. Revisa el orden." },
        };

        Transform _head;
        float _bubbleUntil, _talkUntil, _nextBlink;
        int _lastStep = 1, _lastCount;
        string _lastMachine;
        Color _eyeOn = new Color(0.25f, 0.95f, 1f), _eyeIdle = new Color(0.10f, 0.45f, 0.55f);
        AudioClip _chirp;
        MachineSelector _selector;

        void OnEnable()
        {
            if (session != null) { session.Changed += OnChanged; session.AttemptEnded += OnEnded; session.ResetDone += OnReset; }
            StatusPanel.ExplanationShown += OnExplanation;
            StatusPanel.HintShown += OnHint;
        }

        void OnDisable()
        {
            if (session != null) { session.Changed -= OnChanged; session.AttemptEnded -= OnEnded; session.ResetDone -= OnReset; }
            StatusPanel.ExplanationShown -= OnExplanation;
            StatusPanel.HintShown -= OnHint;
        }

        void Start()
        {
            _chirp = ToneSynth.Ping();
            _selector = Object.FindAnyObjectByType<MachineSelector>();
            Say("intro", null);
        }

        void Update()
        {
            if (_head == null && Camera.main != null) _head = Camera.main.transform;
            if (_head != null)
            {
                // Lazy follow: stays beside the student without sticking to the face.
                Vector3 flatForward = Vector3.ProjectOnPlane(_head.forward, Vector3.up).normalized;
                if (flatForward.sqrMagnitude < 0.01f) flatForward = Vector3.forward;
                Vector3 right = Vector3.Cross(Vector3.up, flatForward);
                Vector3 target = _head.position + right * offsetFromHead.x + Vector3.up * offsetFromHead.y + flatForward * offsetFromHead.z;
                transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Pow(0.15f, Time.deltaTime));
                Vector3 look = _head.position - transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-look), 1f - Mathf.Pow(0.05f, Time.deltaTime));
            }
            if (body != null)
            {
                float talk = Time.time < _talkUntil ? 1f : 0f;
                body.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2.1f) * 0.012f + talk * Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.004f, 0f);
            }
            bool blink = Time.time > _nextBlink && Time.time < _nextBlink + 0.12f;
            if (Time.time > _nextBlink + 0.12f) _nextBlink = Time.time + Random.Range(2.5f, 5f);
            Color eye = blink ? Color.black : (Time.time < _talkUntil && Mathf.Sin(Time.time * 18f) > 0f ? _eyeOn : _eyeIdle);
            if (eyes != null)
                foreach (Renderer r in eyes)
                    if (r != null && r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", eye);
            if (bubble != null && bubble.activeSelf && Time.time > _bubbleUntil) bubble.SetActive(false);

            // A new machine chosen: Chispa introduces it.
            string machine = CurrentMachine();
            if (machine != null && machine != _lastMachine)
            {
                bool first = _lastMachine == null;
                _lastMachine = machine;
                if (!first) Say("intro_" + machine, null);
            }
        }

        string CurrentMachine()
        {
            if (_selector == null || _selector.names == null || _selector.Current >= _selector.names.Length) return null;
            return _selector.names[_selector.Current];
        }

        void OnChanged()
        {
            int count = session.Referee.History.Count, step = session.Referee.CurrentStep;
            if (count <= _lastCount) { _lastCount = count; _lastStep = step; return; }
            _lastCount = count;
            if (session.Ended) { _lastStep = step; return; }
            if (!string.IsNullOrEmpty(session.WarningCode)) Say("warn", ErrorTexts.Title(session.WarningCode));
            else if (step > _lastStep && step >= 2 && step <= 4) Say("ok" + (step - 1), null);
            _lastStep = step;
        }

        void OnEnded()
        {
            if (session.Referee.Completed) Say("done", null);
            else Say("fail", ErrorTexts.Explain(session.Referee.FailCode));
        }

        void OnReset() { _lastStep = 1; _lastCount = 0; }

        void OnHint(string text)
        {
            string machine = CurrentMachine();
            if (text != null && text.Contains("ESPERA"))
                Say(machine != null && Lines.ContainsKey("hint_" + machine) ? "hint_" + machine : "hint", null);
        }

        /// <summary>The AI-written explanation from the server: shown as Chispa's words (no recorded voice for it).</summary>
        void OnExplanation(string text)
        {
            ShowBubble(text, 9f);
            Chirp();
        }

        void Say(string key, string extra)
        {
            if (!Lines.TryGetValue(key, out string line)) return;
            ShowBubble(string.IsNullOrEmpty(extra) ? line : line + "\n<size=30>" + extra + "</size>", bubbleSeconds);
            AudioClip clip = Resources.Load<AudioClip>("Voice/" + key);
            if (clip != null && voice != null)
            {
                voice.Stop();
                voice.clip = clip;
                voice.Play();
                _talkUntil = Time.time + clip.length;
            }
            else Chirp();
        }

        void ShowBubble(string text, float seconds)
        {
            if (bubbleText != null) bubbleText.text = text;
            if (bubble != null) bubble.SetActive(true);
            _bubbleUntil = Time.time + seconds;
            _talkUntil = Mathf.Max(_talkUntil, Time.time + 1.2f);
        }

        void Chirp()
        {
            if (voice != null && _chirp != null) voice.PlayOneShot(_chirp, 0.35f);
        }
    }
}
