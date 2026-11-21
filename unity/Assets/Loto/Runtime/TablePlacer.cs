using Meta.XR;
using UnityEngine;

namespace Loto
{
    /// <summary>
    /// Puts the whole board on a real, horizontal surface (a table or a bench of the workshop) found by the Quest 3
    /// depth sensor through MRUK's EnvironmentRaycastManager: no room scan needed. For the first seconds it tries
    /// along the user's gaze; later, B or Y on a controller places it where that controller points. When it finds a
    /// surface it hides the drawn table, so the machine stands on the real one. Without depth (the editor, the
    /// desktop scene, an unsupported device) it does nothing and the board keeps its fixed place.
    /// </summary>
    public class TablePlacer : MonoBehaviour
    {
        public Transform board;
        public Transform panel;               // the status panel moves with the board
        public Transform head;                // CenterEyeAnchor
        public Transform rightController;     // RightControllerAnchor
        public Transform leftController;      // LeftControllerAnchor
        public EnvironmentRaycastManager raycaster;
        public GameObject[] drawnTable;       // TableTop and TableLeg: hidden once a real surface is used

        [Tooltip("Board-local height of the drawn table top (LotoSceneBuilder.TableTop).")]
        public float tableTop = 0.90f;
        [Tooltip("Board-local distance from the board origin to the table center (LotoSceneBuilder.TableDistance).")]
        public float tableDistance = 0.55f;
        [Tooltip("Seconds after start during which the board follows the gaze until a surface is found.")]
        public float autoSeconds = 15f;
        [Tooltip("Accepted surface heights above the floor, in meters: a table or a bench, never the floor.")]
        public float minHeight = 0.45f;
        public float maxHeight = 1.25f;

        /// <summary>True once the board stands on a real surface.</summary>
        public bool OnRealSurface { get; private set; }

        Vector3 _panelLocal;
        Quaternion _panelLocalRotation = Quaternion.identity;
        float _startTime;
        float _nextTry;
        float _nextLog;

        void Start()
        {
            _startTime = Time.time;
            if (board != null && panel != null)
            {
                _panelLocal = board.InverseTransformPoint(panel.position);
                _panelLocalRotation = Quaternion.Inverse(board.rotation) * panel.rotation;
            }
            if (!Supported()) Debug.Log("[Loto] Real-surface placement off (no environment depth here): the board keeps its fixed place.");
        }

        void Update()
        {
            if (!Supported()) return;

            if (rightController != null && OVRInput.GetDown(OVRInput.RawButton.B))
                TryPlace(new Ray(rightController.position, rightController.forward), true);
            else if (leftController != null && OVRInput.GetDown(OVRInput.RawButton.Y))
                TryPlace(new Ray(leftController.position, leftController.forward), true);
            else if (!OnRealSurface && head != null && Time.time - _startTime < autoSeconds && Time.time >= _nextTry)
            {
                _nextTry = Time.time + 0.25f;
                TryPlace(new Ray(head.position, head.forward), false);
            }
        }

        static void Unanchor(Transform t)
        {
            if (t == null) return;
            OVRSpatialAnchor anchor = t.GetComponent<OVRSpatialAnchor>();
            if (anchor != null) DestroyImmediate(anchor);
        }

        bool Supported()
        {
            if (raycaster == null || board == null) return false;
            try { return EnvironmentRaycastManager.IsSupported; }
            catch (System.Exception) { return false; }
        }

        void TryPlace(Ray ray, bool manual)
        {
            EnvironmentRaycastHit hit;
            bool say = manual || Time.time >= _nextLog;
            if (say && !manual) _nextLog = Time.time + 3f;
            if (!raycaster.Raycast(ray, out hit, 3f))
            {
                if (say) Debug.Log("[Loto] Placement: no surface (" + hit.status + ").");
                return;
            }
            // Floor-level origin: y is the height above the real floor.
            bool flat = Vector3.Dot(hit.normal, Vector3.up) > 0.9f;
            bool tableHeight = hit.point.y >= minHeight && hit.point.y <= maxHeight;
            if (!flat || !tableHeight)
            {
                if (say) Debug.Log("[Loto] Placement: not a table (height " + hit.point.y.ToString("F2") + " m, flat " + flat + ").");
                return;
            }

            PlaceAt(hit.point, manual ? "controller" : "gaze");
        }

        /// <summary>
        /// Puts the center of the drawn table top on point (a real surface), facing the user, and hides the drawn
        /// table. Used by the gaze and controller rays and by QrPlacer (a QR code stuck on the real table).
        /// </summary>
        public bool PlaceAt(Vector3 point, string source)
        {
            if (board == null) return false;
            // Face the user: the board's +Z points from the user to the surface.
            Vector3 toward = head != null ? point - head.position : board.forward;
            toward.y = 0f;
            if (toward.sqrMagnitude < 0.0001f) toward = Vector3.forward;
            Quaternion yaw = Quaternion.LookRotation(toward.normalized, Vector3.up);

            // An anchored object cannot move: drop the old anchors, move, then anchor again so the board does not
            // drift away from the real table (Meta, "MRUK Instant Placement with Environment Raycasting").
            Unanchor(board);
            Unanchor(panel);
            board.SetPositionAndRotation(point - yaw * new Vector3(0f, tableTop, tableDistance), yaw);
            if (panel != null) panel.SetPositionAndRotation(board.TransformPoint(_panelLocal), board.rotation * _panelLocalRotation);
            board.gameObject.AddComponent<OVRSpatialAnchor>();
            if (panel != null) panel.gameObject.AddComponent<OVRSpatialAnchor>();

            if (drawnTable != null)
                foreach (GameObject part in drawnTable)
                    if (part != null) part.SetActive(false);

            OnRealSurface = true;
            Debug.Log("[Loto] Board placed on a real surface, " + point.y.ToString("F2") + " m high (" + source + ").");
            return true;
        }
    }
}
