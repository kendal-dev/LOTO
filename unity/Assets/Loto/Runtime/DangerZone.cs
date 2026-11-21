using UnityEngine;

namespace Loto
{
    /// <summary>
    /// Trigger volume around the machine. A fingertip entering it = "touch the machine".
    /// Also spins the machine's moving part while the lever is ON, so the danger is visible, and lets it
    /// coast to a stop after the lever goes down (stored energy): VERIFY waits until it stands still.
    /// </summary>
    public class DangerZone : MonoBehaviour
    {
        public LotoSession session;
        public Lever lever;
        public Transform spinner;           // optional: the part that turns while there is energy
        public float spinDegreesPerSecond = 360f;
        [Tooltip("Seconds the moving part keeps turning after the lever goes down (inertia: switching off is not the same as no energy).")]
        public float coastSeconds = 6f;

        float _nextTouchTime;
        float _speed = 1f;   // 0..1 of spinDegreesPerSecond

        /// <summary>True while the moving part still turns, also during the coast after the lever went down.</summary>
        public bool Moving { get { return _speed > 0.01f; } }
        /// <summary>The machine's energy level: 1 with the lever up, falling to 0 during the coast (MachineMotion reads it).</summary>
        public float Speed { get { return _speed; } }
        /// <summary>Seconds left until the coast ends (0 when stopped).</summary>
        public float SecondsToStop { get { return _speed * coastSeconds; } }

        void OnTriggerEnter(Collider other)
        {
            FingerTip tip = other.GetComponent<FingerTip>();
            // Putting the headset on (recenter, hand found again) makes the sphere jump: not a touch.
            if (tip == null || tip.JustArrived) return;
            Touch();
        }

        /// <summary>Also called by the keyboard test.</summary>
        public void Touch()
        {
            if (Time.unscaledTime < _nextTouchTime) return;
            _nextTouchTime = Time.unscaledTime + 0.5f;
            session.OnTouchMachine();
        }

        void Update()
        {
            bool energized = lever == null || !lever.IsOff;
            if (session.Referee.Failed) _speed = 0f;   // frozen when the simulation stopped on an error
            else if (energized) _speed = Mathf.MoveTowards(_speed, 1f, Time.deltaTime / 0.3f);
            // Lever down: the part slows down with its own inertia instead of stopping at once.
            else _speed = Mathf.MoveTowards(_speed, 0f, Time.deltaTime / Mathf.Max(0.1f, coastSeconds));
            if (spinner != null && _speed > 0f)
                spinner.Rotate(Vector3.up, spinDegreesPerSecond * _speed * Time.deltaTime, Space.Self);
        }
    }
}
