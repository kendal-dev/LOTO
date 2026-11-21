using System;
using System.Collections.Generic;
using Loto.Core;

namespace Loto.CoreTests
{
    // Tiny test runner without NuGet packages. Exit code != 0 if anything fails.
    public static class Program
    {
        private static int _checks;
        private static int _failures;
        private static string _current;

        public static int Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Run("Happy path completes in 4 steps", HappyPath);
            Run("Touch with lever on -> TOUCH_BEFORE_OFF (stops)", TouchBeforeOff);
            Run("Touch without lock -> TOUCH_WITHOUT_LOCK (stops)", TouchWithoutLock);
            Run("Touch without verify -> TOUCH_WITHOUT_VERIFY (stops)", TouchWithoutVerify);
            Run("Failed attempt ignores everything until Reset", FailedIsSticky);
            Run("Lock with lever on -> LOCK_BEFORE_OFF (does not stop)", LockBeforeOff);
            Run("Verify without lock -> VERIFY_WITHOUT_LOCK (does not stop)", VerifyWithoutLock);
            Run("Lever on with lock -> LEVER_BLOCKED (does not stop)", LeverBlocked);
            Run("Reset clears everything", ResetClears);
            Run("Repeating a done action is Ok without change", RepeatActions);
            Run("LockOff after verify invalidates verification", LockOffAfterVerify);
            Run("LeverOn without lock goes back to step 1", LeverOnWithoutLock);
            Run("Actions after completing are ignored", AfterCompleted);
            Run("History records the attempt", HistoryRecords);
            Run("ErrorTexts has Spanish text for every code", ErrorTextsCoverCodes);

            Console.WriteLine();
            Console.WriteLine(_checks + " checks, " + _failures + " failed");
            Console.WriteLine(_failures == 0 ? "ALL TESTS PASSED" : "TESTS FAILED");
            return _failures == 0 ? 0 : 1;
        }

        // ---------- tests ----------

        private static void HappyPath()
        {
            var r = new Referee();
            Eq(1, r.CurrentStep, "starts at step 1");

            var s = r.Register(LotoAction.LeverOff);
            Ok(s); Eq(2, s.ExpectedStep, "expected step after LeverOff");

            s = r.Register(LotoAction.LockOn);
            Ok(s); Eq(3, s.ExpectedStep, "expected step after LockOn");

            s = r.Register(LotoAction.Verify);
            Ok(s); Eq(4, s.ExpectedStep, "expected step after Verify");
            True(!s.Completed, "not completed before touching");

            s = r.Register(LotoAction.TouchMachine);
            Ok(s);
            True(s.Completed, "result says completed");
            True(r.Completed, "referee says completed");
            True(!r.Failed, "not failed");
            Eq(5, r.CurrentStep, "CurrentStep is 5 when completed");
            Eq(4, s.ExpectedStep, "ExpectedStep stays in 1..4");
        }

        private static void TouchBeforeOff()
        {
            var r = new Referee();
            var s = r.Register(LotoAction.TouchMachine);
            Stops(r, s, "TOUCH_BEFORE_OFF", 1);
        }

        private static void TouchWithoutLock()
        {
            var r = new Referee();
            r.Register(LotoAction.LeverOff);
            var s = r.Register(LotoAction.TouchMachine);
            Stops(r, s, "TOUCH_WITHOUT_LOCK", 2);
        }

        private static void TouchWithoutVerify()
        {
            var r = new Referee();
            r.Register(LotoAction.LeverOff);
            r.Register(LotoAction.LockOn);
            var s = r.Register(LotoAction.TouchMachine);
            Stops(r, s, "TOUCH_WITHOUT_VERIFY", 3);
        }

        private static void FailedIsSticky()
        {
            var r = new Referee();
            r.Register(LotoAction.TouchMachine);
            int count = r.History.Count;

            var s = r.Register(LotoAction.LeverOff);
            True(!s.Ok, "action after failing is not Ok");
            Eq("TOUCH_BEFORE_OFF", s.ErrorCode, "keeps the original error code");
            True(r.Failed, "still failed");
            Eq(1, r.CurrentStep, "state did not advance");
            Eq(count, r.History.Count, "ignored action is not recorded");

            r.Register(LotoAction.LockOn);
            r.Register(LotoAction.Verify);
            s = r.Register(LotoAction.TouchMachine);
            True(!r.Completed && !s.Completed, "cannot complete a failed attempt");
        }

        private static void LockBeforeOff()
        {
            var r = new Referee();
            var s = r.Register(LotoAction.LockOn);
            SoftError(r, s, "LOCK_BEFORE_OFF", 1);
            Continues(r, LotoAction.LeverOff, LotoAction.LockOn, LotoAction.Verify, LotoAction.TouchMachine);
        }

        private static void VerifyWithoutLock()
        {
            var r = new Referee();
            var s = r.Register(LotoAction.Verify);
            SoftError(r, s, "VERIFY_WITHOUT_LOCK", 1);

            r.Register(LotoAction.LeverOff);
            s = r.Register(LotoAction.Verify);
            SoftError(r, s, "VERIFY_WITHOUT_LOCK", 2);
            Continues(r, LotoAction.LockOn, LotoAction.Verify, LotoAction.TouchMachine);
        }

        private static void LeverBlocked()
        {
            var r = new Referee();
            r.Register(LotoAction.LeverOff);
            r.Register(LotoAction.LockOn);
            var s = r.Register(LotoAction.LeverOn);
            SoftError(r, s, "LEVER_BLOCKED", 3);

            r.Register(LotoAction.Verify);
            s = r.Register(LotoAction.LeverOn);
            SoftError(r, s, "LEVER_BLOCKED", 4);
            Continues(r, LotoAction.TouchMachine);
        }

        private static void ResetClears()
        {
            var r = new Referee();
            r.Register(LotoAction.LeverOff);
            r.Register(LotoAction.TouchMachine);
            True(r.Failed, "failed before reset");

            r.Reset();
            True(!r.Failed, "not failed after reset");
            True(!r.Completed, "not completed after reset");
            Eq(1, r.CurrentStep, "back to step 1");
            Eq(0, r.History.Count, "history is empty");
            Continues(r, LotoAction.LeverOff, LotoAction.LockOn, LotoAction.Verify, LotoAction.TouchMachine);

            r.Reset();
            True(!r.Completed, "reset also clears a completed attempt");
            Eq(1, r.CurrentStep, "back to step 1 after completed");
            // The lever is on again after Reset.
            Eq("TOUCH_BEFORE_OFF", r.Register(LotoAction.TouchMachine).ErrorCode, "lever is on after reset");
        }

        private static void RepeatActions()
        {
            var r = new Referee();
            r.Register(LotoAction.LeverOff);
            var s = r.Register(LotoAction.LeverOff);
            Ok(s); Eq(2, s.ExpectedStep, "repeat LeverOff keeps step 2");

            r.Register(LotoAction.LockOn);
            s = r.Register(LotoAction.LockOn);
            Ok(s); Eq(3, s.ExpectedStep, "repeat LockOn keeps step 3");

            r.Register(LotoAction.Verify);
            s = r.Register(LotoAction.Verify);
            Ok(s); Eq(4, s.ExpectedStep, "repeat Verify keeps step 4");

            var r2 = new Referee();
            s = r2.Register(LotoAction.LeverOn);
            Ok(s); Eq(1, s.ExpectedStep, "LeverOn with lever already on");
            s = r2.Register(LotoAction.LockOff);
            Ok(s); Eq(1, s.ExpectedStep, "LockOff with no lock");
            True(!r2.Failed, "repeats never fail");
        }

        private static void LockOffAfterVerify()
        {
            var r = new Referee();
            r.Register(LotoAction.LeverOff);
            r.Register(LotoAction.LockOn);
            r.Register(LotoAction.Verify);

            var s = r.Register(LotoAction.LockOff);
            Ok(s);
            Eq(2, s.ExpectedStep, "back to step 2");
            Eq(2, r.CurrentStep, "CurrentStep is 2");

            // Putting the lock back does not restore the old verification.
            r.Register(LotoAction.LockOn);
            Eq(3, r.CurrentStep, "must verify again");
            s = r.Register(LotoAction.TouchMachine);
            Stops(r, s, "TOUCH_WITHOUT_VERIFY", 3);

            // And touching right after removing the lock is the no-lock error.
            var r2 = new Referee();
            r2.Register(LotoAction.LeverOff);
            r2.Register(LotoAction.LockOn);
            r2.Register(LotoAction.Verify);
            r2.Register(LotoAction.LockOff);
            Stops(r2, r2.Register(LotoAction.TouchMachine), "TOUCH_WITHOUT_LOCK", 2);
        }

        private static void LeverOnWithoutLock()
        {
            var r = new Referee();
            r.Register(LotoAction.LeverOff);
            var s = r.Register(LotoAction.LeverOn);
            Ok(s); Eq(1, s.ExpectedStep, "back to step 1");
            Stops(r, r.Register(LotoAction.TouchMachine), "TOUCH_BEFORE_OFF", 1);
        }

        private static void AfterCompleted()
        {
            var r = new Referee();
            Continues(r, LotoAction.LeverOff, LotoAction.LockOn, LotoAction.Verify, LotoAction.TouchMachine);
            int count = r.History.Count;

            var s = r.Register(LotoAction.LockOff);
            Ok(s);
            True(s.Completed && r.Completed, "still completed");
            Eq(5, r.CurrentStep, "still step 5");
            Eq(count, r.History.Count, "ignored action is not recorded");
        }

        private static void HistoryRecords()
        {
            var r = new Referee();
            r.Register(LotoAction.LockOn);        // soft error, still recorded
            r.Register(LotoAction.LeverOff);
            r.Register(LotoAction.TouchMachine);  // stopping error, recorded

            var expected = new List<LotoAction> { LotoAction.LockOn, LotoAction.LeverOff, LotoAction.TouchMachine };
            Eq(expected.Count, r.History.Count, "history length");
            for (int i = 0; i < expected.Count && i < r.History.Count; i++)
                Eq(expected[i].ToString(), r.History[i].ToString(), "history[" + i + "]");
        }

        private static void ErrorTextsCoverCodes()
        {
            string[] codes =
            {
                "TOUCH_BEFORE_OFF", "TOUCH_WITHOUT_LOCK", "TOUCH_WITHOUT_VERIFY",
                "LOCK_BEFORE_OFF", "VERIFY_WITHOUT_LOCK", "LEVER_BLOCKED"
            };
            string fallbackTitle = ErrorTexts.Title("NOT_A_CODE");
            string fallbackExplain = ErrorTexts.Explain("NOT_A_CODE");
            True(!string.IsNullOrEmpty(fallbackTitle), "unknown code has a fallback title");
            True(!string.IsNullOrEmpty(fallbackExplain), "unknown code has a fallback explanation");

            var seen = new HashSet<string>();
            foreach (string c in codes)
            {
                string t = ErrorTexts.Title(c);
                string e = ErrorTexts.Explain(c);
                True(!string.IsNullOrEmpty(t) && t != fallbackTitle, c + " has its own title");
                True(!string.IsNullOrEmpty(e) && e != fallbackExplain, c + " has its own explanation");
                True(seen.Add(e), c + " explanation is unique");
            }

            Eq("", ErrorTexts.Title(null), "null code gives empty title");
            Eq("", ErrorTexts.Explain(null), "null code gives empty explanation");
        }

        // ---------- helpers ----------

        private static void Stops(Referee r, StepResult s, string code, int step)
        {
            True(!s.Ok, "result is not Ok");
            Eq(code, s.ErrorCode, "error code");
            True(r.Failed, "referee is Failed");
            True(!r.Completed && !s.Completed, "not completed");
            Eq(step, s.ExpectedStep, "expected step");
        }

        private static void SoftError(Referee r, StepResult s, string code, int step)
        {
            True(!s.Ok, "result is not Ok");
            Eq(code, s.ErrorCode, "error code");
            True(!r.Failed, "referee is NOT Failed");
            Eq(step, s.ExpectedStep, "expected step unchanged");
        }

        private static void Continues(Referee r, params LotoAction[] actions)
        {
            foreach (LotoAction a in actions)
                Ok(r.Register(a));
            True(r.Completed, "attempt can still be completed");
        }

        private static void Ok(StepResult s)
        {
            True(s.Ok, "result is Ok");
            True(s.ErrorCode == null, "ErrorCode is null when Ok");
        }

        private static void Run(string name, Action test)
        {
            _current = name;
            int before = _failures;
            try
            {
                test();
            }
            catch (Exception ex)
            {
                _failures++;
                Console.WriteLine("    exception: " + ex.Message);
            }
            Console.WriteLine((_failures == before ? "PASS  " : "FAIL  ") + name);
        }

        private static void True(bool condition, string what)
        {
            _checks++;
            if (condition) return;
            _failures++;
            Console.WriteLine("    [" + _current + "] expected: " + what);
        }

        private static void Eq(object expected, object actual, string what)
        {
            _checks++;
            if (Equals(expected, actual)) return;
            _failures++;
            Console.WriteLine("    [" + _current + "] " + what + ": expected <" + expected + "> but got <" + actual + ">");
        }
    }
}
