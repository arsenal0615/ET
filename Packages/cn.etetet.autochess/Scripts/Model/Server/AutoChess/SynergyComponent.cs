using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 挂在 MatchPlayer 上，持有实时羁绊统计、快照和跨回合 Goblin 状态。
    /// </summary>
    [ComponentOf(typeof(MatchPlayer))]
    public class SynergyComponent : Entity, IAwake, IDestroy
    {
        /// <summary>实时羁绊统计列表，每次准备阶段重新计算。</summary>
        public List<SynergyEntry> LiveSynergies;

        /// <summary>战斗开始时冻结的快照，战斗阶段只读。</summary>
        public TraitSnapshot Snapshot;

        /// <summary>上一次 Goblin 羁绊激活等级，用于跨回合累积逻辑。</summary>
        public int LastGoblinLevel;
    }
}
