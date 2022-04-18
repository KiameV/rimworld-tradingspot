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
            if (lordJob is LordJob_VisitColony ||
                lordJob is LordJob_TradeWithColony)
            {
                if (!Settings.VisitorsGoToTradeSpot && lordJob is LordJob_TradeWithColony)
                {
                    return;
                }
                foreach (var ts in WorldComp.WC.TradingSpots)
                {
                    if (ts.Map == map)
                    {
                        FieldInfo field = lord.LordJob.GetType().GetField("chillSpot", BindingFlags.Instance | BindingFlags.NonPublic);
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
            for (int i = 0; i < l.Count; ++i)
            {
                if (map == l[i].Map)
                {
                    if (this != l[i])
                    {
                        l[i].Destroy(DestroyMode.Vanish);
                        l[i] = this;
                        Messages.Message("TradingSpot.AlreadyOnMap".Translate(), MessageTypeDefOf.NegativeEvent);
                    }
                    return;
                }
            }
            l.Add(this);
        }

        public override void Tick()
        {
            base.Tick();
            if (this.count == 0)
            {
                this.count = 1;
                UpdateLords();
            }
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
        public readonly List<TradingSpot> TradingSpots = new List<TradingSpot>();
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