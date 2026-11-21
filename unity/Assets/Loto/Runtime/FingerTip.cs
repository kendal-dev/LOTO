using Oculus.Interaction.Input;
using UnityEngine;

namespace Loto
{
    /// <summary>
    /// A small kinematic trigger sphere that follows the tip of one index finger. The machine's danger
    /// zone and the buttons react to it. With controllers (no tracked hand) it follows the controller anchor.
    /// Needs on the same object: Rigidbody (kinematic) + SphereCollider (trigger). The scene builder adds them.
    /// </summary>
    public class FingerTip : MonoBehaviour
    {
        public Handedness handedness = Handedness.Right;
        [Tooltip("Used when no tracked hand gives a joint pose (controllers).")]
        public Transform fallbackAnchor;
        public Vector3 fallbackOffset = new Vector3(0f, -0.01f, 0.06f);

        /// <summary>False while there is no hand or controller to follow (the sphere is parked far away).</summary>
        public bool Tracking { get; private set; }

        static readonly Vector3 Parked = new Vector3(0f, -100f, 0f);
        Hand[] _hands;
        float _nextSearch;

        void LateUpdate()
        {
            if (_hands == null || (_hands.Length == 0 && Time.unscaledTime > _nextSearch))
            {
                _hands = FindObjectsByType<Hand>();
                _nextSearch = Time.unscaledTime + 2f;
            }

            for (int i = 0; i < _hands.Length; i++)
            {
                Hand hand = _hands[i];
                if (hand == null || !hand.isActiveAndEnabled || !hand.Started) continue;
                if (!hand.IsTrackedDataValid || hand.Handedness != handedness) continue;
                Pose tip;
                if (hand.GetJointPose(HandJointId.HandIndexTip, out tip))
                {
                    transform.position = tip.position;
                    Tracking = true;
                    return;
                }
            }

            // Controllers: OVRCameraRig moves the anchor's parent (Left/RightHandAnchor); the ControllerAnchor
            // itself keeps local position zero. The parent sits at the tracking origin until something is tracked.
            // Without the Interaction SDK and with bare hands that parent is the wrist, not the fingertip.
            Transform moved = fallbackAnchor != null && fallbackAnchor.parent != null ? fallbackAnchor.parent : fallbackAnchor;
            if (fallbackAnchor != null && moved.localPosition != Vector3.zero)
            {
                transform.position = fallbackAnchor.TransformPoint(fallbackOffset);
                Tracking = true;
                return;
            }

            transform.position = Parked;
            Tracking = false;
        }
    }
}
