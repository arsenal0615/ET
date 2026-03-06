namespace ET.Client
{
    [MessageHandler(SceneType.StateSync)]
    public class M2C_AutoChessOpResultHandler : MessageHandler<Scene, M2C_AutoChessOpResult>
    {
        protected override async ETTask Run(Scene root, M2C_AutoChessOpResult message)
        {
            Scene currentScene = root.CurrentScene();
            AutoChessClientComponent comp = currentScene.GetComponent<AutoChessClientComponent>();
            if (comp == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            comp.ApplyOpResult(message.ErrorCode, message.Elixir, message.PopUsed);

            await ETTask.CompletedTask;
        }
    }
}
