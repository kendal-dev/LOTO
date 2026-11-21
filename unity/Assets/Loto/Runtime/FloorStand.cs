using UnityEngine;

namespace Loto
{
    /// <summary>
    /// Keeps a real-scale machine standing on the real floor (world y = 0: the rig uses a floor-level origin), even
    /// when TablePlacer moves the board onto a real table of any height. Only y changes: the machine stays beside the
    /// table, where the board put it.
    /// </summary>
    public class FloorStand : MonoBehaviour
    {
        void LateUpdate()
        {
            Vector3 p = transform.position;
            if (Mathf.Abs(p.y) > 0.0005f) transform.position = new Vector3(p.x, 0f, p.z);
        }
    }
}
