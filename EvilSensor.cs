﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

namespace TestScript1
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SensorBlock))]
    public class EvilSensor : MyGameLogicComponent
    {
        static String[] OreNames;

        IMySensorBlock Sensor;

        public override void Close()
        {
            Sensor.StateChanged -= sensor_StateChanged;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (OreNames == null)
            {
                MyDefinitionManager.Static.GetOreTypeNames(out OreNames);
            }

            Sensor = Entity as IMySensorBlock;
            Sensor.StateChanged += sensor_StateChanged;
        }

        void sensor_StateChanged(bool obj)
        {
            if(!obj) return;

            string ore = null;

            foreach(var o in OreNames)
            {
                if (Sensor.CustomName.StartsWith(o, StringComparison.InvariantCultureIgnoreCase))
                {
                    ore = o;
                    break;
                }
            }

            if (ore == null)
                return;

            // We want to spawn ore and throw it at entity which entered sensor
            MyObjectBuilder_FloatingObject floatingBuilder = new MyObjectBuilder_FloatingObject();
            floatingBuilder.Item = new MyObjectBuilder_InventoryItem() { Amount = 100, Content = new MyObjectBuilder_Ore() { SubtypeName = ore } };
            floatingBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene; // Very important
            floatingBuilder.PositionAndOrientation = new MyPositionAndOrientation()
            {
                Position = Sensor.WorldMatrix.Translation + Sensor.WorldMatrix.Forward * 1.5f, // Spawn ore 1.5m in front of the sensor
                Forward = Sensor.WorldMatrix.Forward,
                Up = Sensor.WorldMatrix.Up,
            };

            var floatingObject = Sandbox.ModAPI.MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(floatingBuilder);

            // Now it only creates ore, we will throw it later
        }

        public override void MarkForClose()
        {
        }

        public override void UpdateAfterSimulation()
        {
        }

        public override void UpdateAfterSimulation10()
        {
        }

        public override void UpdateAfterSimulation100()
        {
        }

        public override void UpdateBeforeSimulation()
        {
        }

        public override void UpdateBeforeSimulation10()
        {
        }

        public override void UpdateBeforeSimulation100()
        {
        }

        public override void UpdateOnceBeforeFrame()
        {
        }
    }
}
