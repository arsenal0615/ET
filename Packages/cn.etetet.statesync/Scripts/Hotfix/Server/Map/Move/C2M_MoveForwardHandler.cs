using Unity.Mathematics;

namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_MoveForwardHandler : MessageLocationHandler<Unit, C2M_MoveForward>
    {
        protected override async ETTask Run(Unit unit, C2M_MoveForward message)
        {
            float speed = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Speed);
            if (speed < 0.01f)
            {
                unit.SendStop(2);
                return;
            }

            float3 direction = math.normalizesafe(message.Direction);
            if (math.lengthsq(direction) < 0.01f)
            {
                unit.Stop(1);
                return;
            }

            // 目标点 = 当前位置 + 方向 × 足够远的距离
            // 角色会持续朝此方向移动，直到客户端发送新方向或 Stop
            // FindPathMoveToAsync 内部会自动取消上一次移动
            float moveDistance = speed * 5.0f;
            float3 targetPos = unit.Position + direction * moveDistance;

            unit.FindPathMoveToAsync(targetPos).NoContext();

            await ETTask.CompletedTask;
        }
    }
}