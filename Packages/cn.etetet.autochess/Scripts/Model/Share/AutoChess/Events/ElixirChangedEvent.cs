namespace ET
{
    /// <summary>
    /// 玩家圣水值发生变化时发布（ET 内部事件，非网络协议）。
    /// </summary>
    public struct ElixirChangedEvent
    {
        public long PlayerId;
        public int OldValue;
        public int NewValue;
    }
}
