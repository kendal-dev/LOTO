using Loto.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Loto
{
    /// <summary>
    /// A pin that bobs over the object of the step that is missing. Off on the first try (the attempt measures
    /// what the student knows); it turns on after a stopping error (red, over the skipped step) or a yellow
    /// warning, and stays on, amber, while the same student retries. A new student turns it off.
    /// Key G toggles "always on" (for a guided first demo). The script sits on the root and only hides the
    /// child "visual", so Update keeps running.
    /// </summary>
    public class StepPointer : MonoBehaviour
    {
        public LotoSession session;
        public GameObject visual;
        public MeshRenderer head;
        public MeshRenderer stem;
        // Four single fields (not an array): the scene builder's Wire sets one reference per field.
        public Transform target1;   // the lever's red knob
        public Transform target2;   // the padlock (on its tray, or in the hand)
        public Transform target3;   // the VERIFY button
        public Transform target4;   // the machine
        public float lift1 = 0.09f;
        public float lift2 = 0.10f;
        public float lift3 = 0.12f;
        public float lift4 = 0.25f;
        public bool alwaysGuide = false;

        static readonly Color Red = new Color(1.00f, 0.20f, 0.15f);
        static readonly Color Amber = new Color(1.00f, 0.65f, 0.10f);

        bool _guide;
        int _guideStudent = -1;
        Material _material;
        int _shownColor = -1;   // 0 amber, 1 red

        void Awake()
        {
            if (head != null)
            {
                _material = head.material;   // own copy: the color changes at run time
                if (stem != null) stem.sharedMaterial = _material;
            }
        }

        void OnEnable()
        {
            if (session == null) return;
            session.AttemptEnded += OnEnded;
            session.ResetDone += OnReset;
            session.Changed += OnChanged;
        }

        void OnDisable()
        {
            if (session == null) return;
            session.AttemptEnded -= OnEnded;
            session.ResetDone -= OnReset;
            session.Changed -= OnChanged;
        }

        void OnEnded()
        {
            _guide = session.Referee.Failed;
            _guideStudent = session.studentNumber;
        }

        /// <summary>ResetDone comes after studentNumber++: RETRY after an error keeps the guide, a new student clears it.</summary>
        void OnReset()
        {
            if (session.studentNumber != _guideStudent) _guide = false;
        }

        void OnChanged()
        {
            if (!string.IsNullOrEmpty(session.WarningCode))
            {
                _guide = true;
                _guideStudent = session.studentNumber;
            }
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame)
            {
                alwaysGuide = !alwaysGuide;
                Debug.Log("[Loto] Step guide always on: " + alwaysGuide);
            }

            Referee referee = session.Referee;
            bool show = !referee.Completed && (referee.Failed || _guide || alwaysGuide);
            Transform target = null;
            float lift = 0f;
            switch (referee.CurrentStep)
            {
                case 1: target = target1; lift = lift1; break;
                case 2: target = target2; lift = lift2; break;
                case 3: target = target3; lift = lift3; break;
                case 4: target = target4; lift = lift4; break;
            }
            if (target == null) show = false;
            if (visual != null && visual.activeSelf != show) visual.SetActive(show);
            if (!show) return;

            transform.position = target.position + Vector3.up * (lift + 0.02f * Mathf.Sin(Time.time * 4f));
            transform.Rotate(0f, 90f * Time.deltaTime, 0f);

            int color = referee.Failed ? 1 : 0;
            if (_material != null && color != _shownColor)
            {
                _shownColor = color;
                Color c = color == 1 ? Red : Amber;
                if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", c);
                if (_material.HasProperty("_Color")) _material.SetColor("_Color", c);
            }
        }
    }
}
