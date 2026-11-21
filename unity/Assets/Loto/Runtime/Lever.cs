using Oculus.Interaction;
using UnityEngine;

namespace Loto
{
    /// <summary>
    /// Reads the lever's local angle around its X axis (0 = up = ON, offAngle = pulled toward the user = OFF)
    /// and tells the session when it crosses the thresholds. The hand moves it through Meta's
    /// OneGrabRotateTransformer; this script only narrows that transformer's limits when the padlock is on.
    /// </summary>
    public class Lever : MonoBehaviour
    {
        public LotoSession session;
        public OneGrabRotateTransformer transformer;
        [Tooltip("The lever's Grabbable (set by LotoSceneBuilder.Hands). Used to wait until the hand lets go before a reset moves the lever.")]
        public Grabbable grabbable;

        public float offAngle = 90f;        // degrees at the OFF end
        public float offThreshold = 60f;    // above this the lever counts as OFF
        public float onThreshold = 30f;     // below this it counts as ON again
        public float lockedPlay = 8f;       // degrees the lever can still wiggle with the padlock on
        [Tooltip("Sign of the transformer's angle when the lever moves to OFF. If in the headset the lever only moves the wrong way, change -1 to 1.")]
        public float direction = -1f;

        public bool IsOff { get; private set; }
        public bool Locked { get; private set; }

        float _offset;      // transformer's relative angle = _offset + direction * Angle
        float _low;         // allowed range of Angle
        float _high;
        bool _blockedReported;
        bool _resetPending;  // a reset came while a hand held the lever: it waits for the release (open issue A)

        bool HeldByHand { get { return grabbable != null && grabbable.SelectingPointsCount > 0; } }

        void Awake()
        {
            _low = 0f;
            _high = offAngle;
        }

        void OnEnable() { if (session != null) session.ResetDone += HandleReset; }
        void OnDisable() { if (session != null) session.ResetDone -= HandleReset; }

        void Start() { ApplyLimits(); }

        /// <summary>0 when the arm points up (ON), 90 when it points to the local -Z (toward the user, OFF).</summary>
        public float Angle
        {
            get
            {
                Vector3 up = transform.localRotation * Vector3.up;
                return Mathf.Atan2(-up.z, up.y) * Mathf.Rad2Deg;
            }
        }

        void Update()
        {
            if (_resetPending)
            {
                // Moving the lever under a grabbing hand left it out of step with the transformer.
                // While the hand still holds it nothing is reported; on release it goes back to ON.
                if (HeldByHand) return;
                _resetPending = false;
                ForceAngle(0f);
                return;
            }

            float angle = Angle;
            if (!IsOff && angle > offThreshold)
            {
                IsOff = true;
                session.OnLeverOff();
            }
            else if (IsOff && !Locked && angle < onThreshold)
            {
                IsOff = false;
                session.OnLeverOn();
            }
            else if (IsOff && Locked)
            {
                // The padlock holds the lever: trying to lift it is reported once per try (LEVER_BLOCKED).
                if (!_blockedReported && angle < _low + lockedPlay * 0.4f)
                {
                    _blockedReported = true;
                    session.OnLeverOn();
                }
                else if (_blockedReported && angle > _low + lockedPlay * 0.8f)
                {
                    _blockedReported = false;
                }
            }
        }

        /// <summary>Padlock on: the lever keeps only a few degrees of play. Padlock off: full travel again.</summary>
        public void SetLocked(bool locked)
        {
            Locked = locked;
            _blockedReported = false;
            if (locked)
            {
                _low = Mathf.Max(onThreshold + 5f, Angle - lockedPlay);
                _high = offAngle;
            }
            else
            {
                _low = 0f;
                _high = offAngle;
            }
            ApplyLimits();
        }

        /// <summary>Puts the lever at an angle without a hand (reset, and the keyboard test in the Editor).</summary>
        public void ForceAngle(float angle)
        {
            float before = Angle;
            transform.localRotation = Quaternion.AngleAxis(-angle, Vector3.right);
            // The transformer keeps its own running angle and has no reset: shift our offset instead.
            _offset += direction * (before - angle);
            ApplyLimits();
        }

        void HandleReset()
        {
            Locked = false;
            IsOff = false;
            _blockedReported = false;
            _low = 0f;
            _high = offAngle;
            if (HeldByHand)
            {
                _resetPending = true;
                ApplyLimits();
                Debug.Log("[Loto] Reset while the lever is held: it goes back to ON when the hand lets go.");
                return;
            }
            ForceAngle(0f);
        }

        void ApplyLimits()
        {
            if (transformer == null) return;
            float a = _offset + direction * _low;
            float b = _offset + direction * _high;
            var constraints = transformer.Constraints;
            constraints.MinAngle.Constrain = true;
            constraints.MinAngle.Value = Mathf.Min(a, b);
            constraints.MaxAngle.Constrain = true;
            constraints.MaxAngle.Value = Mathf.Max(a, b);
        }
    }
}
