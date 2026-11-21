using UnityEngine;

namespace Loto
{
    /// <summary>
    /// The moving parts of the press and the pneumatic station, driven by the machine's energy level
    /// (DangerZone.Speed: 1 with the lever up, falling to 0 during the coast after the lever goes down).
    /// slider: the ram or the rod, pressing down and up while there is energy; when the energy goes it stays where
    /// the pressure holds it and slowly goes back up. needle: the gauge, at its working pressure with energy and
    /// falling to 0 with the stored pressure. handle: a valve that closes (turns) when the lever goes down.
    /// The scene builder (LotoSceneBuilder.Models.cs) measures the axes and the travel in each model.
    /// </summary>
    public class MachineMotion : MonoBehaviour
    {
        public DangerZone zone;
        public Lever lever;

        public Transform slider;
        public Vector3 slideOffset;             // full stroke, in the slider's parent space
        public float cyclesPerSecond = 0.5f;

        public Transform needle;
        public Vector3 needleAxis = Vector3.forward;   // in the needle pivot's own space, pointing at the user
        public float needleAngle = 180f;               // working pressure; turns clockwise as seen by the user

        public Transform handle;
        public Vector3 handleAxis = Vector3.up;
        public float handleAngle = 90f;
        [Tooltip("The handle is a bleed valve: it opens on PURGAR instead of closing with the lever.")]
        public bool handleOnBleed;

        bool _failSeen, _slam;
        float _slamT;
        Vector3 _slideRest;
        Quaternion _needleRest, _handleRest;
        float _phase, _closed;

        void Awake()
        {
            if (slider != null) _slideRest = slider.localPosition;
            if (needle != null) _needleRest = needle.localRotation;
            if (handle != null) _handleRest = handle.localRotation;
        }

        void Update()
        {
            float energy = zone != null ? zone.Speed : 0f;
            // A touch that stops the attempt with energy in (lever up, or pressure not bled): the ram or the rod
            // slams down at once. What would have happened to the hand. Looks only: the Referee already decided.
            bool failed = zone != null && zone.session != null && zone.session.Referee != null && zone.session.Referee.Failed;
            if (failed && !_failSeen)
            {
                _failSeen = true;
                _slam = (zone.lever != null && !zone.lever.IsOff) || (zone.holdsPressure && !zone.Bled);
            }
            else if (!failed)
            {
                _failSeen = false;
                _slam = false;
            }
            _slamT = Mathf.MoveTowards(_slamT, _slam ? 1f : 0f, Time.deltaTime / (_slam ? 0.12f : 0.8f));
            if (slider != null && _slamT > 0f)
            {
                slider.localPosition = _slideRest + slideOffset * _slamT;
            }
            else if (slider != null)
            {
                // The pump or the compressor drives the stroke only while the lever is up; the stored pressure then
                // holds the ram where it stopped until PURGAR lets it out.
                bool motorOn = lever == null || !lever.IsOff;
                if (motorOn) _phase += Time.deltaTime * cyclesPerSecond * 2f * Mathf.PI;
                float down = (0.5f - 0.5f * Mathf.Cos(_phase)) * energy;
                slider.localPosition = _slideRest + slideOffset * down;
            }
            if (needle != null)
                needle.localRotation = _needleRest * Quaternion.AngleAxis(-needleAngle * energy, needleAxis);
            if (handle != null)
            {
                bool closed = handleOnBleed ? zone != null && zone.Bled : lever != null && lever.IsOff;
                _closed = Mathf.MoveTowards(_closed, closed ? 1f : 0f, Time.deltaTime / 0.4f);
                handle.localRotation = _handleRest * Quaternion.AngleAxis(handleAngle * _closed, handleAxis);
            }
        }
    }
}
