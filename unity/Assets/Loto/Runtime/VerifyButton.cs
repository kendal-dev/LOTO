using UnityEngine;

namespace Loto
{
    /// <summary>
    /// A big button pressed by touching it with a fingertip (trigger volume, no SDK needed).
    /// action = Verify: "try to start the machine" to check it has no energy.
    /// action = Retry: start a new attempt; this one only shows when the attempt has ended.
    /// action = ToggleLever / TogglePadlock: plan B when grabbing with the hands does not work
    /// (the scene builder only creates them when asked). Same path as the keys of DesktopDebugKeys.
    /// </summary>
    public class VerifyButton : MonoBehaviour
    {
        public enum ButtonAction { Verify, Retry, ToggleLever, TogglePadlock }

        public LotoSession session;
        public ButtonAction action = ButtonAction.Verify;
        public Transform cap;                       // the part that moves in when pressed
        public Vector3 pressedOffset = new Vector3(0f, 0f, 0.012f);
        public UnityEngine.UI.Text label;           // optional: Retry changes its text
        public GameObject visual;                   // optional: hidden while the button is not available
        public Lever lever;                         // only for ToggleLever
        public Padlock padlock;                     // only for TogglePadlock

        Vector3 _capRest;
        float _pressedUntil;
        float _nextPressTime;
        Collider _collider;
        DangerZone _machine;
        StatusPanel _panel;

        void Awake()
        {
            _collider = GetComponent<Collider>();
            if (cap != null) _capRest = cap.localPosition;
        }

        void OnEnable() { if (session != null) session.Changed += Refresh; }
        void OnDisable() { if (session != null) session.Changed -= Refresh; }
        void Start()
        {
            _machine = Object.FindAnyObjectByType<DangerZone>();
            _panel = Object.FindAnyObjectByType<StatusPanel>();
            Refresh();
        }

        void OnTriggerEnter(Collider other)
        {
            FingerTip tip = other.GetComponent<FingerTip>();
            if (tip == null || tip.JustArrived) return;   // a jump in tracking is not a press
            Press();
        }

        /// <summary>Also called by the keyboard test.</summary>
        public void Press()
        {
            if (Time.unscaledTime < _nextPressTime) return;
            _nextPressTime = Time.unscaledTime + 0.7f;
            _pressedUntil = Time.unscaledTime + 0.25f;
            if (action == ButtonAction.Verify)
            {
                // Stored energy: the machine is off but still turns. Trying it now proves nothing, so the try
                // is not reported and the panel says why. Only with the lever down (with the lever up the
                // Referee already answers with its own error).
                if (_machine != null && _machine.Moving && _machine.lever != null && _machine.lever.IsOff && !session.Ended)
                {
                    if (_panel != null)
                        _panel.ShowHint("<b>ESPERA: TODAVÍA SE MUEVE</b>\nApagar no alcanza: la polea sigue girando por inercia.\nVerifica cuando esté quieta (" +
                            Mathf.CeilToInt(_machine.SecondsToStop) + " s).", 2.5f);
                    Debug.Log("[Loto] Verify while the machine still coasts: not reported, hint shown.");
                    return;
                }
                session.OnVerify();
            }
            else if (action == ButtonAction.Retry)
            {
                if (session.Ended) session.RetryOrNext();
            }
            else if (action == ButtonAction.ToggleLever && lever != null)
            {
                if (!lever.IsOff) lever.ForceAngle(lever.offAngle);
                // With the padlock on, the lever cannot go up: report the try, as a hand pulling would.
                else if (lever.Locked) session.OnLeverOn();
                else lever.ForceAngle(0f);
            }
            else if (action == ButtonAction.TogglePadlock && padlock != null)
            {
                if (padlock.Snapped) padlock.DebugTakeOff();
                else padlock.DebugPutOn();
            }
        }

        void Update()
        {
            if (cap != null)
                cap.localPosition = Time.unscaledTime < _pressedUntil ? _capRest + pressedOffset : _capRest;
        }

        void Refresh()
        {
            if (action != ButtonAction.Retry) return;
            bool available = session.Ended;
            if (visual != null) visual.SetActive(available);
            if (_collider != null) _collider.enabled = available;
            if (label != null) label.text = session.Referee.Completed ? "SIGUIENTE\nESTUDIANTE" : "REINTENTAR";
        }
    }
}
