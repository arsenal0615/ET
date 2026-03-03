namespace ET.Server
{
    /// <summary>
    /// 商店单个槽位数据。TemplateId = -1 表示空位。
    /// 非 Entity，值语义数据类。
    /// </summary>
    [EnableClass]
    public class ShopOffer
    {
        public int TemplateId; // -1 = 空位
    }
}
