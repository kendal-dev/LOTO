namespace Loto.Core
{
    // Result of registering one action in the Referee.
    public sealed class StepResult
    {
        public bool Ok;
        public string ErrorCode;   // null when Ok
        public bool Completed;
        public int ExpectedStep;   // 1..4: the step the student has to do next
    }
}
