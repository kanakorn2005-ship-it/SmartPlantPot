namespace SmartPlantPot
{
    /// <summary>
    /// Every user-facing string in Thai and English. Switch with SmartPlantPotApp.thaiLanguage.
    /// </summary>
    public static class L
    {
        public static bool Thai = true;

        public static string T(string thai, string english)
        {
            return Thai ? ThaiShaper.Fix(thai) : english;
        }

        // ---------- metrics & status ----------
        public static string MetricName(PlantMetric m)
        {
            switch (m)
            {
                case PlantMetric.Moisture: return T("ความชื้นในดิน", "Soil moisture");
                case PlantMetric.Temperature: return T("อุณหภูมิ", "Temperature");
                default: return T("แสงสว่าง", "Light");
            }
        }

        public static string StatusName(MetricStatus s)
        {
            switch (s)
            {
                case MetricStatus.Low: return T("ต่ำ", "Low");
                case MetricStatus.High: return T("สูง", "High");
                default: return T("ปกติ", "Normal");
            }
        }

        /// <summary>Short advice shown on the info card, toasts and the mascot bubble.</summary>
        public static string Advice(PlantMetric m, MetricStatus s)
        {
            if (s == MetricStatus.Normal) return T("ปกติ", "All good");
            switch (m)
            {
                case PlantMetric.Moisture:
                    return s == MetricStatus.Low
                        ? T("ควรรดน้ำ", "Needs water")
                        : T("ดินแฉะเกินไป งดรดน้ำก่อน", "Soil too wet - hold off watering");
                case PlantMetric.Temperature:
                    return s == MetricStatus.Low
                        ? T("อากาศเย็นเกินไป ย้ายไปที่อุ่นขึ้น", "Too cold - move somewhere warmer")
                        : T("ร้อนเกินไป ย้ายเข้าที่ร่ม", "Too hot - move into the shade");
                default:
                    return s == MetricStatus.Low
                        ? T("แสงน้อยเกินไป ย้ายไปใกล้หน้าต่าง", "Too dark - move nearer a window")
                        : T("แดดจัดเกินไป ย้ายออกจากแดด", "Too much sun - move out of direct light");
            }
        }

        public static string AlertTitle(string plant, PlantMetric m, MetricStatus s)
        {
            return Thai
                ? ThaiShaper.Fix(plant + " · " + MetricName(m) + StatusName(s))
                : plant + " · " + MetricName(m) + " " + StatusName(s).ToLowerInvariant();
        }

        public static string RecoveredTitle(string plant, PlantMetric m)
        {
            return Thai ? ThaiShaper.Fix(plant + " · " + MetricName(m) + "กลับสู่ปกติ") : plant + " · " + MetricName(m) + " back to normal";
        }

        // ---------- care ----------
        public static string Water { get { return T("รดน้ำ", "Water"); } }
        public static string Prune { get { return T("แต่งกิ่ง", "Prune"); } }
        public static string Watered(string plant) { return T("รดน้ำ" + plant + "แล้ว", "Watered the " + plant); }
        public static string Pruned(string plant) { return T("แต่งกิ่ง" + plant + "เรียบร้อย", "Pruned the " + plant); }
        public static string TooWetToWater { get { return T("ดินยังชื้นอยู่ ยังไม่ต้องรดน้ำ", "Soil is still wet - no need to water"); } }
        public static string CareTitle { get { return T("ดูแลต้นไม้", "Plant care"); } }

        // ---------- screens ----------
        public static string AppSubtitle { get { return T("Leafy Corner · ดิจิทัลทวินกระถางต้นไม้", "Leafy Corner · Smart pot digital twin"); } }
        public static string Updated(string time) { return T("อัปเดต " + time, "Updated " + time); }
        public static string IdealRange(string range) { return T("ช่วงที่เหมาะ " + range, "Ideal " + range); }
        public static string HintTapPlant { get { return T("แตะที่ต้นไม้เพื่อดูความชื้น แสง และอุณหภูมิ", "Tap a plant to see moisture, light and temperature"); } }
        public static string AllHappy { get { return T("ต้นไม้ทุกต้นสบายดี", "All plants are happy"); } }
        public static string NeedsAttention(int count) { return T("มีต้นไม้ " + count + " ต้นต้องการการดูแล", count + " plant(s) need attention"); }
        public static string Notifications { get { return T("การแจ้งเตือน", "Notifications"); } }
        public static string NoNotifications { get { return T("ยังไม่มีการแจ้งเตือน", "No notifications yet"); } }
        public static string ClearAll { get { return T("ล้างทั้งหมด", "Clear all"); } }
        public static string Reminder { get { return T("(เตือนซ้ำ)", "(reminder)"); } }
        public static string SimulatedData { get { return T("ข้อมูลจำลอง", "Simulated"); } }
        public static string Online { get { return T("เชื่อมต่อแล้ว", "Live"); } }
        public static string Offline { get { return T("ออฟไลน์", "Offline"); } }
    }
}
