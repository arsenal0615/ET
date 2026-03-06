namespace ET.Client
{
    [MessageHandler(SceneType.StateSync)]
    public class M2C_AutoChessRoundResultHandler : MessageHandler<Scene, M2C_AutoChessRoundResult>
    {
        protected override async ETTask Run(Scene root, M2C_AutoChessRoundResult message)
        {
            Scene currentScene = root.CurrentScene();
            AutoChessClientComponent comp = currentScene.GetComponent<AutoChessClientComponent>();
            if (comp == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            comp.ApplyRoundResult(message);

            await ETTask.CompletedTask;
        }
    }
}
