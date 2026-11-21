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

        void Awake()
        {
            _collider = GetComponent<Collider>();
            if (cap != null) _capRest = cap.localPosition;
        }

        void OnEnable() { if (session != null) session.Changed += Refresh; }
        void OnDisable() { if (session != null) session.Changed -= Refresh; }
        void Start() { Refresh(); }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<FingerTip>() == null) return;
            Press();
        }

        /// <summary>Also called by the keyboard test.</summary>
        public void Press()
        {
            if (Time.unscaledTime < _nextPressTime) return;
            _nextPressTime = Time.unscaledTime + 0.7f;
            _pressedUntil = Time.unscaledTime + 0.25f;
            if (action == ButtonAction.Verify) session.OnVerify();
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
