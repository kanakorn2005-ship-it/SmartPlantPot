namespace SmartPlantPot
{
    /// <summary>Strings for the 3D shop game (points, seeds, selling, settings).</summary>
    public static partial class L
    {
        public static string EmptyPot { get { return T("กระถางว่าง", "Empty pot"); } }
        public static string Points { get { return T("แต้ม", "pts"); } }
        public static string SeedShop { get { return T("ร้านเมล็ดพันธุ์", "Seed shop"); } }
        public static string Buy { get { return T("ซื้อ", "Buy"); } }
        public static string Plant { get { return T("ปลูก", "Plant"); } }
        public static string Fertilize { get { return T("ให้ปุ๋ย", "Fertilize"); } }
        public static string ChooseSeed { get { return T("เลือกเมล็ดที่จะปลูก", "Choose a seed to plant"); } }
        public static string NoSeedsYet { get { return T("ยังไม่มีเมล็ด ไปซื้อที่ร้านก่อนนะ", "No seeds yet - buy some in the shop"); } }
        public static string GoToShop { get { return T("ไปร้านเมล็ด", "Open seed shop"); } }
        public static string Owned(int n) { return T("มี " + n, "Owned " + n); }
        public static string Quality { get { return T("คุณภาพ", "Quality"); } }
        public static string Growth { get { return T("การเติบโต", "Growth"); } }
        public static string SellHarvest(int pts) { return T("ขายผลผลิตวันนี้ +" + pts, "Sell today's harvest +" + pts); }
        public static string SoldToday { get { return T("ขายผลผลิตแล้ววันนี้", "Harvest sold today"); } }
        public static string SellPlant(int pts) { return T("ขายทั้งต้น " + pts + " แต้ม", "Sell plant for " + pts); }
        public static string Cancel { get { return T("ยกเลิก", "Cancel"); } }
        public static string Confirm { get { return T("ยืนยัน", "Confirm"); } }
        public static string Settings { get { return T("ตั้งค่า & Mock", "Settings & Mock"); } }
        public static string Save { get { return T("บันทึก", "Save"); } }
    }
}
