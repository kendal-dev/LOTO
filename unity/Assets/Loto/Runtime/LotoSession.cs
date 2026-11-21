using System;
using Loto.Core;
using UnityEngine;

namespace Loto
{
    /// <summary>
    /// Owns the Referee. Every object in the scene (lever, padlock, button, machine) reports here,
    /// and the panel and the sender listen to the events. One attempt = from Reset to complete or fail.
    /// </summary>
    public class LotoSession : MonoBehaviour
    {
        [Tooltip("Class or group id sent to the server. Empty = sesion-<date>.")]
        public string sessionId = "";
        public int studentNumber = 1;

        public Referee Referee { get; private set; }
        public string StudentName { get { return "Estudiante " + studentNumber; } }
        public string SessionId { get { return sessionId; } }

        /// <summary>Seconds since the first action of this attempt (frozen when it ends).</summary>
        public float Seconds { get; private set; }
        /// <summary>Last error that did NOT stop the attempt (null if none). Shown as a yellow notice.</summary>
        public string WarningCode { get; private set; }
        /// <summary>True once the attempt completed or failed, until Reset.</summary>
        public bool Ended { get { return Referee.Completed || Referee.Failed; } }

        /// <summary>Something changed: the panel redraws.</summary>
        public event Action Changed = delegate { };
        /// <summary>The attempt just completed or failed (raised once per attempt).</summary>
        public event Action AttemptEnded = delegate { };
        /// <summary>A new attempt starts: lever up, padlock back to its place.</summary>
        public event Action ResetDone = delegate { };

        bool _running;
        bool _endRaised;

        void Awake()
        {
            Referee = new Referee();
            if (string.IsNullOrEmpty(sessionId)) sessionId = "sesion-" + DateTime.Now.ToString("yyyy-MM-dd");
        }

        void Update()
        {
            if (_running) Seconds += Time.deltaTime;
        }

        public StepResult OnLeverOff() { return Register(LotoAction.LeverOff); }
        public StepResult OnLeverOn() { return Register(LotoAction.LeverOn); }
        public StepResult OnLockOn() { return Register(LotoAction.LockOn); }
        public StepResult OnLockOff() { return Register(LotoAction.LockOff); }
        public StepResult OnVerify() { return Register(LotoAction.Verify); }
        public StepResult OnTouchMachine() { return Register(LotoAction.TouchMachine); }

        StepResult Register(LotoAction action)
        {
            bool endedBefore = Ended;
            if (!endedBefore && !_running) _running = true;   // the clock starts with the first action

            StepResult result = Referee.Register(action);
            if (!endedBefore)
            {
                WarningCode = (!result.Ok && !Referee.Failed) ? result.ErrorCode : null;
                Debug.Log("[Loto] " + action + " -> " + (result.Ok ? "ok" : result.ErrorCode) + ", step " + Referee.CurrentStep);
            }

            if (Ended && !_endRaised)
            {
                _endRaised = true;
                _running = false;
                WarningCode = null;
                Debug.Log("[Loto] Attempt ended: " + (Referee.Completed ? "completed" : Referee.FailCode) + ", " + Seconds.ToString("F1") + " s, " + StudentName);
                AttemptEnded();
            }
            Changed();
            return result;
        }

        /// <summary>Starts a new attempt. nextStudent = true moves to "Estudiante N+1".</summary>
        public void ResetAttempt(bool nextStudent)
        {
            if (nextStudent) studentNumber++;
            Referee.Reset();
            Seconds = 0f;
            WarningCode = null;
            _running = false;
            _endRaised = false;
            Debug.Log("[Loto] Reset, " + StudentName);
            ResetDone();
            Changed();
        }

        /// <summary>The retry button: same student after an error, next student after a completed attempt.</summary>
        public void RetryOrNext()
        {
            ResetAttempt(Referee.Completed);
        }
    }
}
