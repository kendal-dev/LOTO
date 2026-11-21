using System.Collections.Generic;

namespace Loto.Core
{
    // Pure C# judge of the LOTO sequence (no UnityEngine here).
    // Correct order: 1 LeverOff, 2 LockOn, 3 Verify, 4 TouchMachine.
    public sealed class Referee
    {
        public const string TouchBeforeOff = "TOUCH_BEFORE_OFF";
        public const string TouchWithoutLock = "TOUCH_WITHOUT_LOCK";
        public const string TouchWithoutVerify = "TOUCH_WITHOUT_VERIFY";
        public const string LockBeforeOff = "LOCK_BEFORE_OFF";
        public const string VerifyWithoutLock = "VERIFY_WITHOUT_LOCK";
        public const string LeverBlocked = "LEVER_BLOCKED";

        private readonly List<LotoAction> _history = new List<LotoAction>();
        private bool _leverOff;
        private bool _lockOn;
        private bool _verified;

        public bool Completed { get; private set; }
        public bool Failed { get; private set; }

        // Code of the error that stopped the attempt (null if not failed).
        public string FailCode { get; private set; }

        public IReadOnlyList<LotoAction> History { get { return _history; } }

        // 1..4 = step the student has to do next, 5 = completed.
        public int CurrentStep
        {
            get
            {
                if (Completed) return 5;
                if (_verified) return 4;
                if (_lockOn) return 3;
                if (_leverOff) return 2;
                return 1;
            }
        }

        public void Reset()
        {
            _history.Clear();
            _leverOff = false;
            _lockOn = false;
            _verified = false;
            Completed = false;
            Failed = false;
            FailCode = null;
        }

        public StepResult Register(LotoAction a)
        {
            // After a stopping error nothing counts until Reset.
            if (Failed) return Result(false, FailCode);
            // After completing, extra actions are ignored.
            if (Completed) return Result(true, null);

            _history.Add(a);

            switch (a)
            {
                case LotoAction.LeverOff:
                    _leverOff = true;
                    return Result(true, null);

                case LotoAction.LockOn:
                    if (_lockOn) return Result(true, null);
                    if (!_leverOff) return Result(false, LockBeforeOff);
                    _lockOn = true;
                    return Result(true, null);

                case LotoAction.Verify:
                    if (_verified) return Result(true, null);
                    if (!_lockOn) return Result(false, VerifyWithoutLock);
                    _verified = true;
                    return Result(true, null);

                case LotoAction.TouchMachine:
                    if (!_leverOff) return Fail(TouchBeforeOff);
                    if (!_lockOn) return Fail(TouchWithoutLock);
                    if (!_verified) return Fail(TouchWithoutVerify);
                    Completed = true;
                    return Result(true, null);

                case LotoAction.LockOff:
                    // Removing the lock invalidates the verification (back to step 2).
                    _lockOn = false;
                    _verified = false;
                    return Result(true, null);

                case LotoAction.LeverOn:
                    // The lock physically prevents re-energizing: the point of LOTO.
                    if (_lockOn) return Result(false, LeverBlocked);
                    _leverOff = false;
                    return Result(true, null);
            }

            return Result(true, null);
        }

        private StepResult Fail(string code)
        {
            Failed = true;
            FailCode = code;
            return Result(false, code);
        }

        private StepResult Result(bool ok, string code)
        {
            int step = CurrentStep;
            return new StepResult
            {
                Ok = ok,
                ErrorCode = code,
                Completed = Completed,
                ExpectedStep = step > 4 ? 4 : step
            };
        }
    }
}
