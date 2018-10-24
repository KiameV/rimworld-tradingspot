using UnityEngine;
using Verse;

namespace TradingSpot
{
    public class SettingsController : Mod
    {
        public SettingsController(ModContentPack content) : base(content)
        {
            base.GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "TradingSpot";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }

    public class Settings : ModSettings
    {
        public static bool VisitorsGoToTradeSpot = true;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref VisitorsGoToTradeSpot, "TradingSpot.VisitorsGotoTradeSpot", true, false);
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard l = new Listing_Standard();
            l.Begin(new Rect(0, 80, 300, 100));
            l.CheckboxLabeled("TradingSpot.VisitorsGoToTradeSpot".Translate(), ref VisitorsGoToTradeSpot);
            l.End();
        }
    }
}
