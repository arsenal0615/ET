using System.Collections.Generic;

namespace ET
{
    public enum SkillExecutionStatus
    {
        NotTriggered = 0,
        NoTargets = 1,
        Executed = 2,
    }

    [EnableClass]
    public class SkillExecutionResult
    {
        public SkillExecutionStatus Status;
        public int CasterInstId;
        public int SkillId;
        public List<SkillEffectResult> EffectResults;
    }
}
