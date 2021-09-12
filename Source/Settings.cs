using RimWorld;
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
            Listing_Standard l = new Listing_Standard();
            l.Begin(new Rect(inRect.x, inRect.y, 300, 100));
            l.CheckboxLabeled("TradingSpot.VisitorsGoToTradeSpot".Translate(), ref Settings.VisitorsGoToTradeSpot);
            l.CheckboxLabeled("TradingSpot.RequiresWorkToPlace".Translate(), ref Settings.RequiresWorkToPlace);
            l.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }
    }

    public class Settings : ModSettings
    {
        public static bool VisitorsGoToTradeSpot = true;
        public static bool RequiresWorkToPlace = true;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref VisitorsGoToTradeSpot, "TradingSpot.VisitorsGotoTradeSpot", true, false);
            Scribe_Values.Look<bool>(ref RequiresWorkToPlace, "TradingSpot.RequiresWorkToPlace", true, false);

            if (DefOf.TradingSpot != null)
                this.ApplyWorkSetting();
        }

        public void ApplyWorkSetting()
        {
            foreach (var s in DefOf.TradingSpot.statBases)
            {
                if (s.stat == StatDefOf.WorkToBuild)
                {
                    s.value = (RequiresWorkToPlace) ? 10 : 0;
                    break;

                }
            }
        }
    }

    [RimWorld.DefOf]
    public static class DefOf
    {
        public static ThingDef TradingSpot;
    }
}
