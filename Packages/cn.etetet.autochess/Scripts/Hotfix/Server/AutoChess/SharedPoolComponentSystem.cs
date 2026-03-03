namespace ET.Server
{
    [EntitySystemOf(typeof(SharedPoolComponent))]
    [FriendOf(typeof(SharedPoolComponent))]
    public static partial class SharedPoolComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SharedPoolComponent self)
        {
            self.Remaining = new int[AutoChessDefine.TotalUnitTemplates];
            for (int i = 0; i < AutoChessDefine.TotalUnitTemplates; i++)
                self.Remaining[i] = AutoChessDefine.CopiesPerUnit; // 8
        }

        [EntitySystem]
        private static void Destroy(this SharedPoolComponent self)
        {
            self.Remaining = null;
        }
    }
}
