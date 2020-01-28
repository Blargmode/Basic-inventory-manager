#region pre-script
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;
#endregion
namespace IngameScript
{
	#region in-game
	class AssemblerManager
	{
		//Dictionary<MyDefinitionId, VRage.MyFixedPoint> AssembledBalance; //Stores what items exist in the system. Cor calculating what needs to be assembled or dissasembled
		Dictionary<MyDefinitionId, VRage.MyFixedPoint> AssembleConfig; //Stores what items the user wants auto produced.
		Dictionary<MyDefinitionId, VRage.MyFixedPoint> DisassembleConfig; //Stores what items the user wants auto broken.

		IMyProgrammableBlock Me;
		List<IMyAssembler> Assemblers;

		Dictionary<string, string> BlueprintNames; //For the assembler. Only contains those which are different to the item SubtypeId.

		string MinHeader = "BIM Minimum components";
		string MaxHeader = "BIM Maximum components";

		public AssemblerManager(IMyProgrammableBlock me, List<IMyAssembler> assemblers)
		{
			Me = me;
			Assemblers = assemblers;

			BlueprintNames = new Dictionary<string, string>()
			{
				{"NATO_5p56x45mm", "NATO_5p56x45mmMagazine"},
				{"NATO_25x184mm", "NATO_25x184mmMagazine"},
				{"Computer", "ComputerComponent"},
				{"Construction", "ConstructionComponent"},
				{"Detector", "DetectorComponent"},
				{"Explosives", "ExplosivesComponent"},
				{"Girder", "GirderComponent"},
				{"GravityGenerator", "GravityGeneratorComponent"},
				{"Medical", "MedicalComponent"},
				{"Motor", "MotorComponent"},
				{"RadioCommunication", "RadioCommunicationComponent"},
				{"Reactor", "ReactorComponent"},
				{"Thrust", "ThrustComponent"},
				{"AngleGrinderItem", "AngleGrinder"},
				{"AngleGrinder2Item", "AngleGrinder2"},
				{"AngleGrinder3Item", "AngleGrinder3"},
				{"AngleGrinder4Item", "AngleGrinder4"},
				{"HandDrillItem", "HandDrill"},
				{"HandDrill2Item", "HandDrill2"},
				{"HandDrill3Item", "HandDrill3"},
				{"HandDrill4Item", "HandDrill4"},
				{"AutomaticRifleItem", "AutomaticRifle"},
				{"PreciseAutomaticRifleItem", "PreciseAutomaticRifle"},
				{"RapidFireAutomaticRifleItem", "RapidFireAutomaticRifle"},
				{"UltimateAutomaticRifleItem", "UltimateAutomaticRifle"},
				{"WelderItem", "Welder"},
				{"Welder2Item", "Welder2"},
				{"Welder3Item", "Welder3"},
				{"Welder4Item", "Welder4"}
			};
			
		}


		public void Update(Dictionary<MyDefinitionId, VRage.MyFixedPoint> assemblerBalance)
		{
			if(AssembleConfig == null)
			{
				ProcessAssemblerConfig(assemblerBalance);
			}
			
			foreach (KeyValuePair<MyDefinitionId, VRage.MyFixedPoint> item in AssembleConfig)
			{
				if (assemblerBalance.ContainsKey(item.Key))
				{
					if(assemblerBalance[item.Key] < AssembleConfig[item.Key])
					{
						TryQueue(item.Key, AssembleConfig[item.Key] - assemblerBalance[item.Key], MyAssemblerMode.Assembly);
					}
				}
				else
				{
					TryQueue(item.Key, AssembleConfig[item.Key], MyAssemblerMode.Assembly);
				}
			}

			foreach (KeyValuePair<MyDefinitionId, VRage.MyFixedPoint> item in assemblerBalance)
			{
				if (DisassembleConfig.ContainsKey(item.Key))
				{
					if (DisassembleConfig[item.Key] > -1 && assemblerBalance[item.Key] > DisassembleConfig[item.Key])
					{
						TryQueue(item.Key, assemblerBalance[item.Key] - DisassembleConfig[item.Key], MyAssemblerMode.Disassembly);
					}
				}
			}
		}
		

		bool TryQueue(MyDefinitionId item, VRage.MyFixedPoint amount, MyAssemblerMode mode)
		{
			foreach (var assembler in Assemblers)
			{
				if (assembler.CanUseBlueprint(item) && assembler.Mode == mode && assembler.IsQueueEmpty)
				{
					assembler.AddQueueItem(item, amount);
					return true;
				}
			}
			return false;
		}


		void ProcessAssemblerConfig(Dictionary<MyDefinitionId, VRage.MyFixedPoint> assemblerBalance)
		{
			DisassembleConfig = new Dictionary<MyDefinitionId, VRage.MyFixedPoint>();
			AssembleConfig = new Dictionary<MyDefinitionId, VRage.MyFixedPoint>();

			MyIni ini = new MyIni();
			
			//1. Parse ini in CustomData of the programmable block

			if (ini.TryParse(Me.CustomData))
			{
				if (ini.ContainsSection(MinHeader))
				{
					ReadIniSection(ini, MinHeader, AssembleConfig);
				}

				if (ini.ContainsSection(MaxHeader))
				{
					ReadIniSection(ini, MaxHeader, DisassembleConfig);
				}
			}

			//2. Write the config back to custom data, adding every known component
			//   in the system so that the user doesn't have to know their names.

			List<MyDefinitionId> combined = AssembleConfig.Keys.Union(assemblerBalance.Keys).ToList(); //Combine lists, removing dupes
			List<MyDefinitionId> components = combined.OrderBy(o => o.SubtypeId.ToString()).ToList(); //Sort alphabetically

			foreach (var definitionId in components)
			{
				WriteIniEntry(ini, MinHeader, definitionId, AssembleConfig);
				WriteIniEntry(ini, MaxHeader, definitionId, DisassembleConfig);
			}

			//Set section headers if they exist. Can't do it earlier becasue there can be no sections without data. 
			if (ini.ContainsSection(MinHeader))
			{
				ini.SetSectionComment(MinHeader, " BIM Auto Assembly\n----------------------------------------------------------------------------------------------\n Requires Assembler set to assemble!\n \n Change these values to automatically craft new components once\n you run low. Recompile the script if you've made changes.\n \n Is the list missing an item? Craft one manually, then recompile the\n script. It only knows about items present in BIM:Components containers.\n----------------------------------------------------------------------------------------------");
			}
			if (ini.ContainsSection(MaxHeader))
			{
				ini.SetSectionComment(MaxHeader, " BIM Auto Disassembly\n----------------------------------------------------------------------------------------------\n Requires Assembler set to disassemble!\n \n Change these values for automatic dissassembling of components when\n you have too many. Recompile the script if you've made changes.\n \n Set value to -1 to ignore.\n----------------------------------------------------------------------------------------------");
			}

			Me.CustomData = ini.ToString();
		}

		//I wish this could be a nested function in LoadAssemblerConfig.
		void WriteIniEntry(MyIni ini, string section, MyDefinitionId definitionId, Dictionary<MyDefinitionId, VRage.MyFixedPoint> config)
		{
			if (config.ContainsKey(definitionId))
			{
				ini.Set(section, definitionId.SubtypeId.ToString(), config[definitionId].ToIntSafe());
			}
			else
			{
				ini.Set(section, definitionId.SubtypeId.ToString(), -1);
			}
		}

		//I wish this could be a nested function in LoadAssemblerConfig.
		void ReadIniSection(MyIni ini, string section, Dictionary<MyDefinitionId, VRage.MyFixedPoint> config)
		{
			List<MyIniKey> keys = new List<MyIniKey>();
			ini.GetKeys(section, keys);

			foreach (var key in keys)
			{
				var val = ini.Get(key.Section, key.Name);
				MyDefinitionId id;
				if (GetDefinitionId(key.Name, out id))
				{
					int temp;
					if (val.TryGetInt32(out temp))
					{
						if (config.ContainsKey(id))
						{
							config[id] = temp;
						}
						else
						{
							config.Add(id, temp);
						}
					}
				}
			}
		}

		bool GetDefinitionId(string type, out MyDefinitionId definitionId)
		{
			//Hiding string magic here.

			//jTurp "I asked Inflex to add in a MyItemType.MakeBlueprint so hopefully in the future it'll be easier" 
			//So this hardcoded lookup table should be replaced if that happens.

			//Translate subtypeID to blueprint if needed
			if (BlueprintNames.ContainsKey(type))
			{
				type = BlueprintNames[type];
			}

			if (MyDefinitionId.TryParse("MyObjectBuilder_BlueprintDefinition/" + type, out definitionId))
			{
				return true;
			}
			return false;
		}

		//Calculates how much of each item the assembler can make we have.
		public void CalculateAssemblableCount(IMyTerminalBlock block, Dictionary<MyDefinitionId, VRage.MyFixedPoint> data)
		{
			//Get all inventories
			IMyInventory[] invs = new IMyInventory[block.InventoryCount];
			for (int i = 0; i < block.InventoryCount; i++)
			{
				IMyInventory inv = block.GetInventory(i);

				//Abort if inventory is missing
				if (inv == null)
				{
					//BlocksMissingInventory.Add(block.CustomName); //TODO: Not accessible in AssemblerManager. 
					continue;
				}

				var items = new List<MyInventoryItem>();
				inv.GetItems(items);


				foreach (var item in items)
				{
					var itemInfo = item.Type.GetItemInfo();

					if (itemInfo.IsComponent || itemInfo.IsTool || itemInfo.IsAmmo)
					{
						MyDefinitionId definitionId;
						if (GetDefinitionId(item.Type.SubtypeId, out definitionId))
						{
							//Echo("Success " + item.Type.SubtypeId + " / " + definitionId.SubtypeId);
							if (data.ContainsKey(definitionId))
							{
								data[definitionId] += item.Amount;
							}
							else
							{
								data.Add(definitionId, item.Amount);
							}
						}
						else
						{
							//Echo("Fail " + item.Type.SubtypeId);
						}
					}
				}
				//throw new Exception("Breakpoint");
			}
		}
	}
	#endregion
}
