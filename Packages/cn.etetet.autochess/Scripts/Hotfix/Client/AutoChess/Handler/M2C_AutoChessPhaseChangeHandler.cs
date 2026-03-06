namespace ET.Client
{
    [MessageHandler(SceneType.StateSync)]
    public class M2C_AutoChessPhaseChangeHandler : MessageHandler<Scene, M2C_AutoChessPhaseChange>
    {
        protected override async ETTask Run(Scene root, M2C_AutoChessPhaseChange message)
        {
            Scene currentScene = root.CurrentScene();
            AutoChessClientComponent comp = currentScene.GetComponent<AutoChessClientComponent>();
            if (comp == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            comp.ApplyPhaseChange(message.Round, message.NewPhase, message.PhaseEndTime);

            await ETTask.CompletedTask;
        }
    }
}
