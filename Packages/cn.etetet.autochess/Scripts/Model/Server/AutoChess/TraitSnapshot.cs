using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 羁绊快照：冻结某一时刻的羁绊计算结果，供战斗阶段只读使用。
    /// </summary>
    [EnableClass]
    public class TraitSnapshot
    {
        /// <summary>全局激活的羁绊列表。</summary>
        public List<SynergyEntry> ActiveSynergies = new();

        /// <summary>每个单位（InstId → 该单位触发的羁绊列表）。</summary>
        public Dictionary<int, List<SynergyEntry>> UnitSynergyMap = new();

        /// <summary>每个单位（InstId → 战斗属性修改值）。</summary>
        public Dictionary<int, UnitBattleModifiers> UnitModifiers = new();
    }
}
