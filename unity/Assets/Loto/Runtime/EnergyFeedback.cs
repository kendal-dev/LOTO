using UnityEngine;

namespace Loto
{
    /// <summary>
    /// The energy you can see and hear. While the lever is up the machine hums and its lamp is red; when the
    /// lever goes down the hum winds down and the lamp turns grey. Each correct step pings, an error that lets
    /// the student go on beeps twice, VERIFY plays the start-up test (a dry clack, the lamp blinks amber and
    /// "PRUEBA DE ARRANQUE / NO ARRANCA" shows over the machine), and the end of the attempt plays a chime
    /// (completed) or a buzzer (stopped). Same rule as DangerZone: no energy when the lever is off or the
    /// simulation stopped. It only listens to LotoSession: the Referee and the keys do not change.
    /// </summary>
    public class EnergyFeedback : MonoBehaviour
    {
        public LotoSession session;
        public Lever lever;
        public MeshRenderer lamp;
        public AudioSource hum;
        public AudioSource sfx;
        public GameObject tryOutLabel;
        public float humVolume = 0.35f;
        public float sfxVolume = 0.6f;

        static readonly Color LampOn = new Color(0.95f, 0.10f, 0.08f);
        static readonly Color LampOff = new Color(0.12f, 0.13f, 0.14f);
        static readonly Color LampBlink = new Color(1.00f, 0.60f, 0.10f);

        AudioClip _ping, _warn, _clac, _buzz, _chime;
        Material _lampMaterial;
        Color _shownColor;
        float _level = 1f;
        float _blinkUntil;
        float _labelUntil;
        int _lastStep = 1;
        int _lastCount;

        void Awake()
        {
            _ping = ToneSynth.Ping();
            _warn = ToneSynth.Warn();
            _clac = ToneSynth.Clac();
            _buzz = ToneSynth.Buzz();
            _chime = ToneSynth.Chime();
            if (lamp != null) _lampMaterial = lamp.material;   // own copy: the color changes at run time
            if (hum != null)
            {
                hum.clip = ToneSynth.Hum();
                hum.loop = true;
                hum.volume = humVolume;
                hum.Play();
            }
            if (tryOutLabel != null) tryOutLabel.SetActive(false);
        }

        void OnEnable()
        {
            if (session == null) return;
            session.Changed += OnChanged;
            session.AttemptEnded += OnEnded;
            session.ResetDone += OnReset;
        }

        void OnDisable()
        {
            if (session == null) return;
            session.Changed -= OnChanged;
            session.AttemptEnded -= OnEnded;
            session.ResetDone -= OnReset;
        }

        void Update()
        {
            bool energized = (lever == null || !lever.IsOff) && !session.Referee.Failed;
            // Energy back (padlock off and lever up right after VERIFY): the start-up test no longer holds.
            if (energized && (_labelUntil > 0f || _blinkUntil > 0f))
            {
                _labelUntil = 0f;
                _blinkUntil = 0f;
                if (tryOutLabel != null) tryOutLabel.SetActive(false);
            }
            // Spins up in 0.3 s, winds down in 1.2 s.
            _level = Mathf.MoveTowards(_level, energized ? 1f : 0f, Time.deltaTime / (energized ? 0.3f : 1.2f));
            if (hum != null)
            {
                hum.volume = humVolume * _level;
                hum.pitch = 0.55f + 0.45f * _level;
            }

            Color color = energized ? LampOn : LampOff;
            if (Time.time < _blinkUntil && Mathf.Sin(Time.time * 25f) > 0f) color = LampBlink;
            if (_lampMaterial != null && color != _shownColor)
            {
                _shownColor = color;
                if (_lampMaterial.HasProperty("_BaseColor")) _lampMaterial.SetColor("_BaseColor", color);
                if (_lampMaterial.HasProperty("_Color")) _lampMaterial.SetColor("_Color", color);
            }

            if (tryOutLabel != null && tryOutLabel.activeSelf && Time.time > _labelUntil) tryOutLabel.SetActive(false);
        }

        void OnChanged()
        {
            int count = session.Referee.History.Count;
            int step = session.Referee.CurrentStep;
            // A reset, or an action ignored after the end, adds nothing to the history: no sound.
            if (count <= _lastCount)
            {
                _lastCount = count;
                _lastStep = step;
                return;
            }
            _lastCount = count;
            // The end of the attempt is played by OnEnded (AttemptEnded comes before Changed).
            if (session.Ended)
            {
                _lastStep = step;
                return;
            }
            if (!string.IsNullOrEmpty(session.WarningCode)) Play(_warn);
            else if (_lastStep == 3 && step == 4) TryOut();
            else if (step > _lastStep) Play(_ping);
            // Going back a step (taking the padlock off) makes no sound.
            _lastStep = step;
        }

        void OnEnded()
        {
            Play(session.Referee.Completed ? _chime : _buzz);
        }

        void OnReset()
        {
            _lastStep = 1;
            _lastCount = 0;
            _blinkUntil = 0f;
            _labelUntil = 0f;
            if (tryOutLabel != null) tryOutLabel.SetActive(false);
        }

        /// <summary>VERIFY: the student tries to start the machine and it does not start.</summary>
        void TryOut()
        {
            Play(_clac);
            _blinkUntil = Time.time + 0.8f;
            if (tryOutLabel != null)
            {
                tryOutLabel.SetActive(true);
                _labelUntil = Time.time + 2.5f;
            }
        }

        void Play(AudioClip clip)
        {
            if (sfx != null && clip != null) sfx.PlayOneShot(clip, sfxVolume);
        }
    }
}
