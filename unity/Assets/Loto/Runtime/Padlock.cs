using Loto.Core;
using Oculus.Interaction;
using UnityEngine;

namespace Loto
{
    /// <summary>
    /// The padlock, with its own snap: when the hand lets go within snapDistance of the lock point,
    /// it asks the session; if the Referee accepts (lever off) it becomes a child of the lock point
    /// and blocks the lever. Anywhere else, or refused, it goes back to its tray.
    /// Grabbing it again while it is on the lock point takes it off.
    /// </summary>
    public class Padlock : MonoBehaviour
    {
        public LotoSession session;
        public Lever lever;
        public Transform anchor;            // the lock point on the switch box
        public Grabbable grabbable;
        public float snapDistance = 0.08f;

        public bool Snapped { get; private set; }

        Transform _homeParent;
        Vector3 _homePosition;
        Quaternion _homeRotation;
        bool _held;
        bool _dropPending;

        void Awake()
        {
            _homeParent = transform.parent;
            _homePosition = transform.localPosition;
            _homeRotation = transform.localRotation;
        }

        void OnEnable()
        {
            if (grabbable != null) grabbable.WhenPointerEventRaised += HandlePointerEvent;
            if (session != null) session.ResetDone += GoHome;
        }

        void OnDisable()
        {
            if (grabbable != null) grabbable.WhenPointerEventRaised -= HandlePointerEvent;
            if (session != null) session.ResetDone -= GoHome;
        }

        void HandlePointerEvent(PointerEvent evt)
        {
            if (evt.Type == PointerEventType.Select)
            {
                _held = true;
                _dropPending = false;
                if (Snapped) TakeOff();
            }
            else if (evt.Type == PointerEventType.Unselect || evt.Type == PointerEventType.Cancel)
            {
                if (grabbable.SelectingPointsCount == 0)
                {
                    _held = false;
                    _dropPending = true;    // handled in LateUpdate, after the grab has finished moving it
                }
            }
        }

        void LateUpdate()
        {
            if (!_dropPending || _held) return;
            _dropPending = false;
            Drop();
        }

        /// <summary>The hand let go: snap if close enough and the Referee accepts, otherwise back to the tray.</summary>
        void Drop()
        {
            if (anchor != null && Vector3.Distance(transform.position, anchor.position) <= snapDistance)
            {
                StepResult result = session.OnLockOn();
                if (result.Ok)
                {
                    PutOn();
                    return;
                }
            }
            GoHome();
        }

        void PutOn()
        {
            Snapped = true;
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (lever != null) lever.SetLocked(true);
        }

        void TakeOff()
        {
            Snapped = false;
            transform.SetParent(_homeParent, true);
            if (lever != null) lever.SetLocked(false);
            session.OnLockOff();
        }

        /// <summary>Back to the tray without telling the session (reset, or a drop that did not lock).</summary>
        public void GoHome()
        {
            Snapped = false;
            transform.SetParent(_homeParent, false);
            transform.localPosition = _homePosition;
            transform.localRotation = _homeRotation;
            if (lever != null && lever.Locked) lever.SetLocked(false);
        }

        // --- Keyboard test in the Editor (DesktopDebugKeys) ---

        /// <summary>Same path as letting go of the padlock on the lock point.</summary>
        public void DebugPutOn()
        {
            if (Snapped) return;
            transform.position = anchor.position;
            Drop();
        }

        /// <summary>Same path as grabbing the padlock off the lock point and leaving it on the tray.</summary>
        public void DebugTakeOff()
        {
            if (!Snapped) return;
            TakeOff();
            GoHome();
        }
    }
}
