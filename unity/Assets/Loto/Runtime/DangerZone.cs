using UnityEngine;

namespace Loto
{
    /// <summary>
    /// Trigger volume around the machine. A fingertip entering it = "touch the machine".
    /// Also spins the machine's moving part while the lever is ON, so the danger is visible.
    /// </summary>
    public class DangerZone : MonoBehaviour
    {
        public LotoSession session;
        public Lever lever;
        public Transform spinner;           // optional: the part that turns while there is energy
        public float spinDegreesPerSecond = 360f;

        float _nextTouchTime;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<FingerTip>() == null) return;
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
            if (spinner == null) return;
            bool energized = lever == null || !lever.IsOff;
            // Stopped when off, and frozen when the simulation stopped on an error.
            if (energized && !session.Referee.Failed)
                spinner.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
