using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI.Group;

namespace TradingSpot
{
    public class TradingSpot : Building
    {
        private int count = 0;

        public TradingSpot()
        {
            checked
            {
                if (Find.VisibleMap != null)
                {
                    bool flag = false;
                    int index = -1;
                    for (int i = 0; i < Find.VisibleMap.listerBuildings.allBuildingsColonist.Count; i++)
                    {
                        if (Find.VisibleMap.listerBuildings.allBuildingsColonist[i].Label == "trading spot")
                        {
                            flag = true;
                            index = i;
                        }
                    }
                    if (flag)
                    {
                        Find.VisibleMap.listerBuildings.allBuildingsColonist[index].Destroy(0);
                        Messages.Message("Only one trading spot allowed per map.  Removing old spot.", MessageSound.Negative);
                    }
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (this.count++ > 1)
            {
                this.count = 0;
                List<Lord> lords = base.Map.lordManager.lords;
                IntVec3 position = base.Position;
                checked
                {
                    for (int i = 0; i < lords.Count; i++)
                    {
                        Lord lord = lords[i];
                        if (lord.LordJob is LordJob_TradeWithColony)
                        {
                            FieldInfo field = lord.LordJob.GetType().GetField("chillSpot", BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            IntVec3 intVec = (IntVec3)field.GetValue(lord.LordJob);
                            if (intVec.x != position.x || intVec.y != position.y || intVec.z != position.z)
                            {
                                field.SetValue(lord.LordJob, position);
                            }
                            LordToil curLordToil = lord.CurLordToil;
                            if (curLordToil is LordToil_Travel)
                            {
                                LordToil_Travel lordToil_Travel = (LordToil_Travel)curLordToil;
                                if (lordToil_Travel.FlagLoc != position)
                                {
                                    lordToil_Travel.SetDestination(position);
                                    lordToil_Travel.UpdateAllDuties();
                                }
                            }
                            else if (curLordToil is LordToil_DefendPoint)
                            {
                                LordToil_DefendPoint lordToil_DefendPoint = (LordToil_DefendPoint)curLordToil;
                                if (lordToil_DefendPoint.FlagLoc != position)
                                {
                                    lordToil_DefendPoint.SetDefendPoint(position);
                                    lordToil_DefendPoint.UpdateAllDuties();
                                }
                            }
                            foreach (Pawn current in lord.ownedPawns)
                            {
                                if (current.RaceProps.Animal)
                                {
                                    if (current.needs != null && current.needs.food != null && current.needs.food.CurLevel <= current.needs.food.MaxLevel)
                                    {
                                        current.needs.food.CurLevel = current.needs.food.MaxLevel;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}