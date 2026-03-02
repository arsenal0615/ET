using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 对局管理组件，挂在 Scene 上，管理所有活跃的 MatchRoom。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MatchComponent : Entity, IAwake, IDestroy
    {
    }
}
