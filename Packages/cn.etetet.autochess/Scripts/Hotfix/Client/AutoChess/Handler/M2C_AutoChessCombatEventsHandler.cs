namespace ET.Client
{
    [MessageHandler(SceneType.StateSync)]
    public class M2C_AutoChessCombatEventsHandler : MessageHandler<Scene, M2C_AutoChessCombatEvents>
    {
        protected override async ETTask Run(Scene root, M2C_AutoChessCombatEvents message)
        {
            Scene currentScene = root.CurrentScene();
            AutoChessClientComponent comp = currentScene.GetComponent<AutoChessClientComponent>();
            if (comp == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            comp.ApplyCombatEvents(message.OpponentId, message.Events);

            await ETTask.CompletedTask;
        }
    }
}
