using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using VRageMath;

namespace Scripts.KSWH
{
    [Sandbox.Common.MySessionComponentDescriptor(Sandbox.Common.MyUpdateOrder.BeforeSimulation)]
    class MissionComponent : Sandbox.Common.MySessionComponentBase
    {
        //enum MissionState
        //{
        //    NotStarted,
        //    InProgress,
        //    Success,
        //    Failed
        //}

        class Trigger
        {
            public Func<int,bool> UpdateMethod;
        }

        class MissionData
        {
            public List<EnemyShip> EnemyShips = new List<EnemyShip>();
            public MyCubeBlockDefinition LargeTrustS;
            public MyCubeBlockDefinition LargeTrustL;

            public List<Trigger> Triggers = new List<Trigger>();
            public Sandbox.ModAPI.Ingame.IMyTerminalBlock ShipComp;
            public string LastMsg;
        }

        private MissionData m_data;
        private bool m_loaded;
        private int count;
        public static MyLogger Logger;
        public override void UpdateBeforeSimulation()
        {
            //try
            //{
                if (MyAPIGateway.Session == null)
                    return;
                if (!m_loaded)
                    Init();
                if (!m_loaded)
                    return;
                count++;
                for (int i = 0; i < m_data.Triggers.Count; i++)
                {
                    var trigger = m_data.Triggers[i];
                    if (trigger.UpdateMethod(count))
                    {
                        m_data.Triggers.Remove(trigger);
                        i--;
                    }
                }
                foreach (var enemy in m_data.EnemyShips)
                    enemy.Update(count);
            //}
            //catch (Exception e)
            //{
            //    Logger.WriteLine(e.Message);
            //    Logger.WriteLine(e.StackTrace);
            //}
        }

        private static Dictionary<string,int> m_enemies = new Dictionary<string,int>(){ {"Drone1", 900},{ "Drone2", 1200} , {"Drone3", 1200}, {"Drone4",1000},{"Drone5",1000}, {"Boss1",1000}};
        private static List<string> m_friendlyGrids = new List<string>() { "Arrow1", "Meteor" };
        private HashSet<IMyEntity> m_entitiesCache = new HashSet<IMyEntity>();
        private List<IMySlimBlock> m_blocksCache = new List<IMySlimBlock>();
        private bool m_loadFailed;

        private void Init()
        {
            try
            {
                Logger = new MyLogger("Log");
                m_data = new MissionData();

                InitEnemies();

                if(MyAPIGateway.Multiplayer.IsServer)
                    InitOwnership();

                if (MyAPIGateway.Multiplayer.IsServer)
                    InitMissionBlock();

                if (MyAPIGateway.Multiplayer.IsServer)
                    SetupMeteor();

                MyDefinitionId id = new MyDefinitionId(typeof(MyObjectBuilder_Thrust), "SmallBlockLargeThrust");
                m_data.LargeTrustS = MyDefinitionManager.Static.GetCubeBlockDefinition(id);
                id = new MyDefinitionId(typeof(MyObjectBuilder_Thrust), "LargeBlockLargeThrust");
                m_data.LargeTrustL = MyDefinitionManager.Static.GetCubeBlockDefinition(id);

                CheckMissionData();

                m_data.LargeTrustL.Public = false;
                m_data.LargeTrustS.Public = false;

                var trig = new Trigger();
                trig.UpdateMethod = (count) =>
                    {
                        if (m_data.LastMsg != null && (count / 60) % 30 == 0)
                            MyAPIGateway.Utilities.ShowMessage("", m_data.LastMsg);
                        return false;
                    };

                m_loaded = true;
            }
            catch (Exception e)
            {
                if (Logger != null)
                {
                    Logger.WriteLine(e.Message);
                    Logger.WriteLine(e.StackTrace);
                    MyAPIGateway.Utilities.ShowNotification("Mission load failed, restart or re-download the files.", 30000, Sandbox.Common.MyFontEnum.Red);
                }
                m_loadFailed = true;
            }
        }

        private void InitMissionBlock()
        {
            var playerShip = MyAPIGateway.Entities.GetEntity((x) => x is IMyCubeGrid && x.DisplayName == "Arrow1") as IMyCubeGrid;
            m_entitiesCache.Clear();
            playerShip.GetBlocks(m_blocksCache, (x) => x.FatBlock is Sandbox.ModAPI.Ingame.IMyTerminalBlock && (x.FatBlock as Sandbox.ModAPI.Ingame.IMyTerminalBlock).CustomName == "Ship computer");
            m_data.ShipComp = m_blocksCache.FirstOrDefault().FatBlock as Sandbox.ModAPI.Ingame.IMyTerminalBlock;
            m_blocksCache.Clear();
        }

        //This is the "meteor" that hits the ship after start, we use warheads to blow off the large thrust at back
        private void SetupMeteor()
        {
            IMyCubeBlock meteor = null;
            ITerminalAction action = null;
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                //Get the grid with warheads
                var grid = MyAPIGateway.Entities.GetEntity((x) => x is IMyCubeGrid && x.DisplayName == "Meteor") as IMyCubeGrid;
                //get any warhead on grid
                grid.GetBlocks(m_blocksCache,
                    (x) => x.FatBlock is Sandbox.ModAPI.Ingame.IMyTerminalBlock && x.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Warhead));// as Sandbox.ModAPI.Ingame.IMyTerminalBlock).CustomName.Contains("Warhead"));
                meteor = (m_blocksCache.FirstOrDefault() as IMySlimBlock).FatBlock;
                m_blocksCache.Clear();
                var actionList = new List<ITerminalAction>();
                MyAPIGateway.TerminalActionsHelper.GetActions(meteor.GetType(), actionList, (x) => x.Id == "Detonate");
                action = actionList.FirstOrDefault();
                actionList.Clear();
                //unlock safety
                MyAPIGateway.TerminalActionsHelper.GetActions(meteor.GetType(), actionList, (x) => x.Id == "Safety");
                actionList.FirstOrDefault().Apply(meteor);
                Logger.WriteLine((meteor as Sandbox.ModAPI.Ingame.IMyTerminalBlock).CustomName);
                //Countdown, the "meteor" will hit the ship 7 seconds after load
            }
            var trigger = new Trigger();
            trigger.UpdateMethod = (count) =>
            {
                if (count / 60 > 7)
                {
                    //detonate the warhead
                    if (MyAPIGateway.Multiplayer.IsServer)
                        action.Apply(meteor);
                    //proceed to first submission
                    CreateFirstSubmission(count);
                    return true;
                }
                return false;
            };

            m_data.Triggers.Add(trigger);
        }

        private void CreateFirstSubmission(int start)
        {
            string[] FirstDialog = new string[] 
            {@"Ship hit by asteroid, Large thruster destroyed", 
            @"Fix the ship by placing small thrusters",
            @"Find help in the area, make sure to turn off turrets when approaching unknown structures you want to explore"};

            //Trigger that will show our messages with some space in between them
            var trig = new Trigger();
            trig.UpdateMethod = (count) =>
            {
                if (FirstDialog[0] != null)
                {
                    ShowMissionMsg(FirstDialog[0]);
                    FirstDialog[0] = null;
                }
                if ((count - start) / 60 > 6)
                {
                    if (FirstDialog[1] != null)
                    {
                        ShowMissionMsg(FirstDialog[1]);
                        FirstDialog[1] = null;
                    }
                }
                if ((count - start) / 60 > 16)
                {
                    if (FirstDialog[2] != null)
                    {
                        ShowMissionMsg(FirstDialog[2]);
                        FirstDialog[2] = null;
                    }
                }
                return false;
            };
            m_data.Triggers.Add(trig);

            //Setup trigger that will check if player reached the first satellite and send him towards the next point
            var sat1 = MyAPIGateway.Entities.GetEntity((x) => x is IMyCubeGrid && x.DisplayName == "Satellite1");
            trig = new Trigger();
            trig.UpdateMethod = (count) =>
            {
                if (count % 101 == 0)
                {
                    var sphere = new BoundingSphere(sat1.GetPosition(), 400);
                    if (GetPlayerInSphere(ref sphere) != null)
                    {
                        CreateSecondSubmission(count);
                        return true;
                    }
                }
                return false;
            };
            m_data.Triggers.Add(trig);
        }

        private void CreateSecondSubmission(int start)
        {
            var SecondDialog = new string[] { 
                @"Reach the station and gain large thrust blueprint from Computer inside station",
                @"Station1 access obtained",
                @"Large Thruster blueprint obtained"
            };
            ShowMissionMsg(SecondDialog[0]);

            var station = MyAPIGateway.Entities.GetEntity((x) => x is IMyCubeGrid && x.DisplayName == "Station1") as IMyCubeGrid;
            var computers = new List<IMyCubeBlock>();
            station.GetBlocks(m_blocksCache, (x) => x.FatBlock is Sandbox.ModAPI.Ingame.IMyTerminalBlock && (x.FatBlock as Sandbox.ModAPI.Ingame.IMyTerminalBlock).CustomName.Contains("ControlStation"));
            foreach (var b in m_blocksCache)
                computers.Add(b.FatBlock);
            m_blocksCache.Clear();

            ////Switch Station1 electricity when player reaches 1300m
            //var sphere = new BoundingSphere(station.GetPosition(), 1300);
            //var trig = new Trigger();
            //trig.UpdateMethod = (count) =>
            //{
            //    if (count % 99 == 0)
            //        if (GetPlayerInSphere(ref sphere) != null)
            //        {
            //            (computers[0] as IMyControllableEntity).SwitchReactors();
            //            return true;
            //        }
            //    return false;
            //};
            //m_data.Triggers.Add(trig);

            //Enable large thrust  when sitted in station computer
            var players = new List<IMyPlayer>();
            var trig = new Trigger();
            trig.UpdateMethod = (count) =>
            {
                if (count % 100 == 0)
                {
                    players.Clear();
                    MyAPIGateway.Multiplayer.Players.GetPlayers(players);
                    foreach (var player in players)
                    {
                        foreach(var computer in computers)
                        {
                            if (player.Controller.ControlledEntity == computer)
                            {
                                MyAPIGateway.Utilities.ShowMessage("", SecondDialog[1]);
                                MyAPIGateway.Utilities.ShowMessage("", SecondDialog[2]);
                                m_data.LargeTrustL.Public = true;
                                m_data.LargeTrustS.Public = true;
                                if(MyAPIGateway.Multiplayer.IsServer)
                                    station.ChangeGridOwnership(player.PlayerId, MyOwnershipShareModeEnum.Faction);
                                CreateThirdSubmission(count);
                                return true;
                            }
                        }
                    }
                }
                return false;
            };
            m_data.Triggers.Add(trig);
        }

        private void CreateThirdSubmission(int start)
        {
            var ThirdDialog = new string[] {
            @"Build Large thurster on Arrow1 large ship"
            };

            ShowMissionMsg(ThirdDialog[0]);

            var thrustList = new List<IMyCubeBlock>();
            //check if large thrust was placed
            (m_data.ShipComp.CubeGrid as IMyCubeGrid).OnBlockAdded += (x) =>
                {
                    Logger.WriteLine(string.Format("Block added {0}", x.FatBlock is Sandbox.ModAPI.Ingame.IMyTerminalBlock ? (x.FatBlock as Sandbox.ModAPI.Ingame.IMyTerminalBlock).CustomName : ""));
                    if (x.FatBlock != null && x.FatBlock.BlockDefinition == m_data.LargeTrustL.Id)
                    {
                        Logger.WriteLine("Thrust added");
                        thrustList.Add(x.FatBlock);
                    }
                };

            //check if any large thrust was welded above functional threshold
            var trig = new Trigger();
            trig.UpdateMethod = (count) =>
            {
                if(count % 98 == 0)
                {
                    Logger.WriteLine(string.Format("Thrust count: ", thrustList.Count));
                    foreach(var thrust in thrustList)
                    {
                        if(thrust.IsWorking)
                        {
                            CreateFourthSubmission(count);
                            return true;
                        }
                    }
                }
                return false;
            };
            m_data.Triggers.Add(trig);
        }

        private void CreateFourthSubmission(int start)
        {
            var FourthDialog = new string[] {
            @"Reach the Satellite2 and prepare to leave the sector, deal with any possible threats"
            };
            ShowMissionMsg(FourthDialog[0]);
            MyDefinitionId reactorDef = new MyDefinitionId(typeof(MyObjectBuilder_Reactor), "LargeBlockSmallGenerator");
            var targets = new string[] { "Drone4", "Drone5", "Boss1" };
            var reactors = new List<IMySlimBlock>();
            foreach (var enemy in m_data.EnemyShips)
            {
                if (targets.Contains(enemy.Ship.DisplayName))
                    enemy.Ship.GetBlocks(reactors, (x) => x.FatBlock != null && x.FatBlock.BlockDefinition == reactorDef);
            }
            var trig = new Trigger();
            trig.UpdateMethod = (count) =>
            {
                if (count % 100 == 0)
                {
                    bool hasEnergy = false;
                    foreach (var reactor in reactors)
                        hasEnergy |= reactor.FatBlock.IsWorking;
                    if (!hasEnergy)
                    {
                        MissionSuccess();
                        return true;
                    }
                }
                return false;
            };
        }

        private void MissionSuccess()
        {
            ShowMissionMsg("Mission succesfully finished");
            MyAPIGateway.Utilities.ShowNotification("Mission succesfully finished", 30000, Sandbox.Common.MyFontEnum.Green);
        }

        private void ShowMissionMsg(string msg)
        {
            m_data.ShipComp.SetCustomName(msg);
            MyAPIGateway.Utilities.ShowMessage("", msg);
            m_data.LastMsg = msg;
        }

        private void InitOwnership()
        {
            //Find npc player that was created in world in prior
            var enemy = MyAPIGateway.Players.AllPlayers.Where((x) => x.Value.DisplayName == "NPC 8109").FirstOrDefault();
            Logger.WriteLine("enemy " + enemy.Key);
            MyAPIGateway.Entities.GetEntities(m_entitiesCache, (x) => x is IMyCubeGrid && !m_friendlyGrids.Contains(x.DisplayName));
            foreach (var ent in m_entitiesCache)
            {
                Logger.WriteLine(ent.DisplayName);
                var grid = ent as IMyCubeGrid;
                grid.ChangeGridOwnership(enemy.Key, MyOwnershipShareModeEnum.None);
            }
            m_entitiesCache.Clear();
            var player = MyAPIGateway.Session.Player.PlayerId;
            Logger.WriteLine("player " + player);
            MyAPIGateway.Entities.GetEntities(m_entitiesCache, (x) => x is IMyCubeGrid && m_friendlyGrids.Contains(x.DisplayName));
            foreach (var ent in m_entitiesCache)
            {
                Logger.WriteLine(ent.DisplayName);
                var grid = ent as IMyCubeGrid;
                grid.ChangeGridOwnership(player, MyOwnershipShareModeEnum.Faction);
            }
            m_entitiesCache.Clear();
        }

        private void InitEnemies()
        {
            MyAPIGateway.Entities.GetEntities(m_entitiesCache, (x) => x is IMyCubeGrid && m_enemies.ContainsKey(x.DisplayName));
            foreach (var ent in m_entitiesCache)
                m_data.EnemyShips.Add(new EnemyShip(ent as IMyCubeGrid, m_enemies[ent.DisplayName]));
            m_entitiesCache.Clear();
        }

        public static IMyEntity GetPlayerInSphere(ref BoundingSphere sphere)
        {
            var ents = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
            foreach (var entity in ents)
            {
                var controllable = entity as IMyControllableEntity;
                if (controllable != null && (MyAPIGateway.Multiplayer.Players.GetPlayerControllingEntity(entity) != null || controllable == MyAPIGateway.Session.ControlledObject))
                    return entity;
            }
            return null;
        }

        private void CheckMissionData()
        {
            if (m_data.LargeTrustL == null)
                Logger.WriteLine("LargeTrustL not found");
            if (m_data.LargeTrustS == null)
                Logger.WriteLine("LargeTrustS not found");
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            Logger.Close();
            Logger = null;
        }
    }
}
