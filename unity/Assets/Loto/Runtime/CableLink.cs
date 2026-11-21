using UnityEngine;

namespace Loto
{
    /// <summary>
    /// A cable (a cylinder primitive, 2 units tall) stretched between two points that can move apart: the table's
    /// edge (with the board) and the real-scale machine on the floor (FloorStand). Updated every frame, looks only.
    /// </summary>
    public class CableLink : MonoBehaviour
    {
        public Transform from;
        public Transform to;
        public float thickness = 0.016f;

        void LateUpdate()
        {
            if (from == null || to == null) return;
            Vector3 a = from.position, b = to.position, d = b - a;
            if (d.sqrMagnitude < 0.000001f) return;
            transform.position = (a + b) / 2f;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            transform.localScale = new Vector3(thickness, d.magnitude / 2f, thickness);
        }
    }
}
