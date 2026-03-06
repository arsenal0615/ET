namespace ET.Client
{
    [MessageHandler(SceneType.StateSync)]
    public class M2C_AutoChessMatchResultHandler : MessageHandler<Scene, M2C_AutoChessMatchResult>
    {
        protected override async ETTask Run(Scene root, M2C_AutoChessMatchResult message)
        {
            Scene currentScene = root.CurrentScene();
            AutoChessClientComponent comp = currentScene.GetComponent<AutoChessClientComponent>();
            if (comp == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            comp.ApplyMatchResult(message.Rankings);

            await ETTask.CompletedTask;
        }
    }
}
