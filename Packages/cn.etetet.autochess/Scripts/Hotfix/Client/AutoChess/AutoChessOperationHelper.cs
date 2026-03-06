namespace ET.Client
{
    public static class AutoChessOperationHelper
    {
        /// <summary>
        /// 购买商店槽位中的单位
        /// </summary>
        public static void SendBuy(Scene scene, int slotIndex)
        {
            C2M_AutoChessBuy msg = C2M_AutoChessBuy.Create();
            msg.SlotIndex = slotIndex;
            scene.GetComponent<ClientSenderComponent>().Send(msg);
        }

        /// <summary>
        /// 出售指定单位
        /// </summary>
        public static void SendSell(Scene scene, int instId)
        {
            C2M_AutoChessSell msg = C2M_AutoChessSell.Create();
            msg.InstId = instId;
            scene.GetComponent<ClientSenderComponent>().Send(msg);
        }

        /// <summary>
        /// 放置单位到棋盘指定位置
        /// </summary>
        public static void SendPlace(Scene scene, int instId, int col, int row)
        {
            C2M_AutoChessPlace msg = C2M_AutoChessPlace.Create();
            msg.InstId = instId;
            msg.Col = col;
            msg.Row = row;
            scene.GetComponent<ClientSenderComponent>().Send(msg);
        }

        /// <summary>
        /// 交换两个单位的位置
        /// </summary>
        public static void SendSwap(Scene scene, int instId1, int instId2)
        {
            C2M_AutoChessSwap msg = C2M_AutoChessSwap.Create();
            msg.InstId1 = instId1;
            msg.InstId2 = instId2;
            scene.GetComponent<ClientSenderComponent>().Send(msg);
        }

        /// <summary>
        /// 请求进入自走棋对局，返回初始游戏状态
        /// </summary>
        public static async ETTask<M2C_AutoChessEnterGame> SendEnterGame(Scene scene)
        {
            C2M_AutoChessEnterGame request = C2M_AutoChessEnterGame.Create();
            M2C_AutoChessEnterGame response = (M2C_AutoChessEnterGame)await scene.GetComponent<ClientSenderComponent>().Call(request);
            return response;
        }
    }
}
