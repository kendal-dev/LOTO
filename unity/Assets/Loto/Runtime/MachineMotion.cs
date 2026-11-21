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
            if (slider != null)
            {
                _phase += Time.deltaTime * cyclesPerSecond * energy * 2f * Mathf.PI;
                float down = (0.5f - 0.5f * Mathf.Cos(_phase)) * energy;
                slider.localPosition = _slideRest + slideOffset * down;
            }
            if (needle != null)
                needle.localRotation = _needleRest * Quaternion.AngleAxis(-needleAngle * energy, needleAxis);
            if (handle != null)
            {
                bool closed = lever != null && lever.IsOff;
                _closed = Mathf.MoveTowards(_closed, closed ? 1f : 0f, Time.deltaTime / 0.4f);
                handle.localRotation = _handleRest * Quaternion.AngleAxis(handleAngle * _closed, handleAxis);
            }
        }
    }
}
