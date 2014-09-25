using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

using VRageMath;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApplication3 {
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_ShipConnector))]
	
	public class FilterEjector : MyGameLogicComponent {
    	private DateTime nextUpdate;
    	private int rate;
    	private string objectName;
    	private bool objectValid;
    	private MyObjectBuilder_FloatingObject floatingBuilder;
    	private string oldName;
    	private IMyEntity tempEnt;
    	private short objType;
    	private float volume;
    	private Vector3I vecFront;
    	private IMyFunctionalBlock Ejector;
    	
    	List<Sandbox.ModAPI.Ingame.IMySlimBlock> storageBlocks;
  	
	    public override void Init(Sandbox.Common.ObjectBuilders.MyObjectBuilder_EntityBase objectBuilder) {
	        Entity.NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
	        rate = 500;
	        objectName = "";
	        nextUpdate = MyAPIGateway.Session.GameDateTime.AddDays(-1);
            oldName = "";
            Ejector = Entity as IMyFunctionalBlock;
            
            floatingBuilder = new MyObjectBuilder_FloatingObject();
			floatingBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene;
			objType = 0;
			storageBlocks = new List<Sandbox.ModAPI.Ingame.IMySlimBlock>();
			volume = 0.075f;
			vecFront = new Vector3I(0,0,0);
    	}
        
    	public void SetupObject() {
    		objectValid = true;
    		
    	
    		switch (objType) {
    			case 0:
		        	floatingBuilder.Item = new MyObjectBuilder_InventoryItem() { Content = new MyObjectBuilder_Component() { SubtypeName = objectName } };
		    		foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions()) {
		    			if(def.Id.TypeId.ToString() == "MyObjectBuilder_Component" && def.Id.SubtypeName == objectName) {
		    				MyAPIGateway.Utilities.ShowNotification(def.Id.TypeId.ToString() + " " + MyDefinitionManager.Static.GetComponentDefinition(def.Id).Volume.ToString(),  2000, MyFontEnum.Blue);
		    				floatingBuilder.Item.Amount = (int) Math.Floor(volume / MyDefinitionManager.Static.GetComponentDefinition(def.Id).Volume);
		    			}
		    		}
		        	break;
    			case 1:
		        	floatingBuilder.Item = new MyObjectBuilder_InventoryItem() {Content = new MyObjectBuilder_Ore() { SubtypeName = objectName } };
		    		foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions()) {
		    			if(def.Id.TypeId.ToString() == "MyObjectBuilder_Ore" && def.Id.SubtypeName == objectName) {
		    				MyAPIGateway.Utilities.ShowNotification(def.Id.TypeId.ToString() + " " + MyDefinitionManager.Static.GetPhysicalItemDefinition(def.Id).Volume.ToString(),  2000, MyFontEnum.Blue);
		    				floatingBuilder.Item.Amount = (int) Math.Floor(volume / MyDefinitionManager.Static.GetPhysicalItemDefinition(def.Id).Volume);
		    			}
		    		}
		        	break;
    			case 2:
		        	floatingBuilder.Item = new MyObjectBuilder_InventoryItem() { Content = new MyObjectBuilder_Ingot() { SubtypeName = objectName } };
		    		foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions()) {
		    			if(def.Id.TypeId.ToString() == "MyObjectBuilder_Ingot" && def.Id.SubtypeName == objectName) {
		    				MyAPIGateway.Utilities.ShowNotification(def.Id.TypeId.ToString() + " " + MyDefinitionManager.Static.GetPhysicalItemDefinition(def.Id).Volume.ToString(),  2000, MyFontEnum.Blue);
		    				floatingBuilder.Item.Amount = (int) Math.Floor(volume / MyDefinitionManager.Static.GetPhysicalItemDefinition(def.Id).Volume);
		    			}
		    		}
		        	break;
    			case 3:
		        	floatingBuilder.Item = new MyObjectBuilder_InventoryItem() { Content = new MyObjectBuilder_PhysicalGunObject() { SubtypeName = objectName } };
		    		foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions()) {
		    			if(def.Id.TypeId.ToString() == "MyObjectBuilder_PhysicalGunObject" && def.Id.SubtypeName == objectName) {
		    				MyAPIGateway.Utilities.ShowNotification(def.Id.TypeId.ToString() + " " + MyDefinitionManager.Static.GetPhysicalItemDefinition(def.Id).Volume.ToString(),  2000, MyFontEnum.Blue);
		    				floatingBuilder.Item.Amount = (int) Math.Floor(volume / MyDefinitionManager.Static.GetPhysicalItemDefinition(def.Id).Volume);
		    			}
		    		}
		        	break;
    		}
    		
    		if (floatingBuilder.Item.Amount < 1) floatingBuilder.Item.Amount = 1;
       	}
	    
		public override void UpdateBeforeSimulation10() { 
    		if (objectValid && nextUpdate < MyAPIGateway.Session.GameDateTime && !Entity.Closed && Ejector.Enabled && Ejector.IsFunctional && floatingBuilder != null && Ejector != null) {
    			float pos = 1.3f;
    			if (Ejector.BlockDefinition.SubtypeName == "ConnectorMedium") pos = 0.5f;	
    			
	           	floatingBuilder.PositionAndOrientation = new MyPositionAndOrientation() {
					Position = Entity.LocalMatrix.Translation + Entity.WorldMatrix.Forward * pos,
	                Forward = Entity.LocalMatrix.Forward,
	                Up = Entity.LocalMatrix.Up,
				};

    			vecFront = Ejector.Position + new Vector3I((int) Math.Round(Ejector.LocalMatrix.Forward.X), (int) Math.Round(Ejector.LocalMatrix.Forward.Y), (int) Math.Round(Ejector.LocalMatrix.Forward.Z));
    			
    			if (!Ejector.CubeGrid.CubeExists(vecFront)) {
    				foreach (var ent in storageBlocks) {
    					var fb = ent.FatBlock;
    					if (fb != null) {
							MyAPIGateway.Utilities.ShowNotification("FB " + fb.BlockDefinition.SubtypeName);
							if (fb.BlockDefinition.SubtypeName == "LargeBlockSmallContainer") {
								MyAPIGateway.Utilities.ShowNotification("YAY !");
								var cg = fb as Sandbox.ModAPI.Ingame.IMyFunctionalBlock;
							}
							/*
								var cg = (MyObjectBuilder_CargoContainer) fb;
			    				if (cg != null) {
			    					MyAPIGateway.Utilities.ShowNotification("YESSS " + cg.Inventory.Items.Count.ToString());
		    					}
							}
							*/
    					}
    				}
    			}
    			
    			/*
				var input = (MyObjectBuilder_CargoContainer) input.GetObjectBuilderCubeBlock(); 
				var inputInventory = inputFace.Inventory; 
				var input = (MyObjectBuilder_CargoContainer) output.GetObjectBuilderCubeBlock(); 
				var outputInventory = inputFace.Inventory; 
				if(inputInventory.Items.Count > 0) { 
					var item = inputInventory.Items[0]; 
					inputInventory.Items.Remove(item); // removeAt(0) does nothing,too. 
					outputInventory.Items.Add(item); // does nothing 
				};
				*/

				tempEnt = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(floatingBuilder);
	            nextUpdate = MyAPIGateway.Session.GameDateTime.AddMilliseconds(rate);
    		}
		}
	
		public override void UpdateBeforeSimulation100() {
    		if (Ejector != null) {
	    		if (Ejector.CustomName != oldName) {
    				if (Ejector.CustomName.Contains("FilterComp ")) 	{ objectName = Ejector.CustomName.Substring(Ejector.CustomName.IndexOf("FilterComp ", StringComparison.InvariantCulture)+11); objType = 0; }
    				if (Ejector.CustomName.Contains("FilterOre ")) 		{ objectName = Ejector.CustomName.Substring(Ejector.CustomName.IndexOf("FilterOre ", StringComparison.InvariantCulture)+10); objType = 1; }
	    			if (Ejector.CustomName.Contains("FilterIngot ")) 	{ objectName = Ejector.CustomName.Substring(Ejector.CustomName.IndexOf("FilterIngot ", StringComparison.InvariantCulture)+12); objType = 2; }
	    			if (Ejector.CustomName.Contains("FilterTool ")) 	{ objectName = Ejector.CustomName.Substring(Ejector.CustomName.IndexOf("FilterTool ", StringComparison.InvariantCulture)+11); objType = 3; }
					if (Ejector.CustomName.Contains("Delay "))			{ 
	    				int val;
	    				if (int.TryParse(Ejector.CustomName.Substring(Ejector.CustomName.IndexOf("Delay ", StringComparison.InvariantCulture)+6), out val)) {
		    				rate = MathHelper.Clamp(val,250,3600000);
							MyAPIGateway.Utilities.ShowNotification("Delay between ejects changed to " + rate + "ms",  2500, MyFontEnum.Blue);
						}
	    			}
	    			if (objectName.Contains(" ")) objectName = objectName.Substring(0,objectName.IndexOf(" ", StringComparison.InvariantCulture));
					SetupObject();
					oldName = Ejector.CustomName;
		   		}
	   			storageBlocks.Clear();
    			(Entity as Sandbox.ModAPI.Ingame.IMyCubeBlock).CubeGrid.GetBlocks(storageBlocks, x => x.FatBlock != null);
    		}  		
		}
		
		
		
    	public override void Close() {}
	    public override void MarkForClose() {}
	    public override void UpdateAfterSimulation() {}
	    public override void UpdateAfterSimulation10() {}
	    public override void UpdateAfterSimulation100() {}
		public override void UpdateBeforeSimulation() {}
	    public override void UpdateOnceBeforeFrame() {}
	}
}
