using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI.Group;
using RimWorld.Planet;
using HarmonyLib;

namespace TradingSpot
{
    [HarmonyPatch(typeof(LordMaker), "MakeNewLord")]
    static class Patch_LordMaker_MakeNewLord
    {
        [HarmonyPriority(Priority.Last)]
        static void Finalizer(ref Lord __result, Faction faction, LordJob lordJob, Map map, IEnumerable<Pawn> startingPawns)
        {
            if (faction == Faction.OfPlayer || __result == null || lordJob == null)
            {
                return;
            }
            UpdateLordJob(ref __result, lordJob, map);
        }

        public static void UpdateLordJob(ref Lord lord, LordJob lordJob, Map map)
        {
            FieldInfo field = null;
            if (lordJob is LordJob_VisitColony ||
                lordJob is LordJob_TradeWithColony)
            {
                if (ModFinder.HasHospitality ||
                    (!Settings.VisitorsGoToTradeSpot && lordJob is LordJob_TradeWithColony))
                {
                    return;
                }

                if (field == null)
                    field = lord.LordJob.GetType().GetField("chillSpot", BindingFlags.Instance | BindingFlags.NonPublic);

                if (WorldComp.WC.TradingSpots.TryGetValue(map, out TradingSpot ts))
                {
                    field.SetValue(lord.LordJob, ts.Position);

                    var toil = lord.CurLordToil;
                    if (toil is LordToil_Travel t)
                    {
                        t.SetDestination(ts.Position);
                        t.UpdateAllDuties();
                    }
                    else if (toil is LordToil_DefendPoint dt)
                    {
                        dt.SetDefendPoint(ts.Position);
                        dt.UpdateAllDuties();
                    }
                }
            }
        }
    }

    static class ModFinder
    {
        private static bool? hasHospitality;
        public static bool HasHospitality 
        { 
            get
            {
                if (hasHospitality == null) {
                    foreach (ModMetaData d in ModsConfig.ActiveModsInLoadOrder)
                    {
                        hasHospitality = false;
                        if (d.Name.EqualsIgnoreCase("hospitality"))
                        {
                            hasHospitality = true;
                            break;
                        }
                    }
                }
                return hasHospitality.Value;
            }
        }
    }

    public class TradingSpot : Building
    {
        private int count = 0;
        public TradingSpot()
        {

        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            var l = WorldComp.WC.TradingSpots;
            if (l.TryGetValue(map, out TradingSpot ts) && ts?.Destroyed == false)
            {
                Messages.Message("TradingSpot.AlreadyOnMap".Translate(), MessageTypeDefOf.NegativeEvent);
            }
            l[map] = this;
        }

        public override void Tick()
        {
            base.Tick();
            if (count % 60 == 0)
            {
                if (ModFinder.HasHospitality)
                    return;
                UpdateLords();
                count = 0;
            }
            ++count;
        }

        private void UpdateLords()
        {
            var l = base.Map.lordManager.lords;
            for (int i = 0; i < l?.Count; ++i)
            {
                var lord = l[i];
                if (lord != null && lord.LordJob != null)
                    Patch_LordMaker_MakeNewLord.UpdateLordJob(ref lord, lord.LordJob, this.Map);
            }
        }
    }

    public class WorldComp : WorldComponent
    {
        public static WorldComp WC;
        public readonly Dictionary<Map, TradingSpot> TradingSpots = new Dictionary<Map, TradingSpot>();
        public WorldComp(World world) : base(world)
        {
            WC = this;
            TradingSpots.Clear();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            var mod = LoadedModManager.GetMod(typeof(SettingsController));
            var s = mod.GetSettings<Settings>();
            s.ApplyWorkSetting();
        }
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                TradingSpots.Clear();
            }
        }
    }
}