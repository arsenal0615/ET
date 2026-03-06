namespace ET.Client
{
    [MessageHandler(SceneType.StateSync)]
    public class M2C_AutoChessRoundStateHandler : MessageHandler<Scene, M2C_AutoChessRoundState>
    {
        protected override async ETTask Run(Scene root, M2C_AutoChessRoundState message)
        {
            Scene currentScene = root.CurrentScene();
            AutoChessClientComponent comp = currentScene.GetComponent<AutoChessClientComponent>();
            if (comp == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            comp.ApplyRoundState(message);

            await ETTask.CompletedTask;
        }
    }
}
