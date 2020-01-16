﻿#region pre-script
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
namespace IngameScript
{
	public partial class Program : MyGridProgram
	{
		#endregion
		#region in-game
		
		//TODO\\__________________________________

		//MAYBE: More than one type per container

		//BUG: Removed block causes crash
		

		bool initialized = false;

		IEnumerator<bool> RunStateMachine;

		[Flags]
		enum RunningError
		{
			IngotExportFailed = 0,
			ComponentExportFailed = 1,
			OreExportFailed = 2,
			ToolExportFailed = 4,
		};

		struct TypeFillData
		{
			//amount, total, percent
			public Vector3 Ingots;
			public Vector3 Components;
			public Vector3 Ores;
			public Vector3 Tools;
			public Vector3 Empty;
		}

		class ExternalExportDefinition
		{
			public IMyShipConnector connector;
			public List<IMyTerminalBlock> exportFrom;
		}

		TypeFillData FillLevel;

		RunningError Errors = 0;
		RunningError ErrorsShow = 0; //Used for displaying purposes, Only updated once everything has been worked through
		Dictionary<RunningError, string> RunningErrorMessages;

		List<string>  SetupMessages = new List<string>();
		HashSet<string> BlocksMissingInventory = new HashSet<string>();
		List<string> BlocksMissingInventoryShow = new List<string>();

		//Any inventory
		List<IMyTerminalBlock> Ingots;
		List<IMyTerminalBlock> Components;
		List<IMyTerminalBlock> Ores;
		List<IMyTerminalBlock> Tools;
		List<IMyTerminalBlock> Empty;

		//Connectors - Exports into connected connector.
		List<ExternalExportDefinition> ExternalExporter;

		IMyTextSurface surface; //Used for calculating text width;

		//Strings for printing Echo, gets padding by EqualTextPadding()
		readonly string[] defaultCategoryStrings = new string[]
		{
			"Ingots:",
			"Components:",
			"Ores:",
			"Tools:",
			"Empty:"
		};

		//These get modyfied in Init()
		string[] categoryStrings;

		public Program()
		{
			Runtime.UpdateFrequency = UpdateFrequency.Update10;

			RunningErrorMessages = new Dictionary<RunningError, string>()
			{
				{ RunningError.IngotExportFailed, "Not enough container space for ingots." },
				{ RunningError.ComponentExportFailed, "Not enough container space for components." },
				{ RunningError.OreExportFailed, "Not enough container space for ores." },
				{ RunningError.ToolExportFailed, "Not enough container space for tools/misc." }
			};

			surface = (Me as IMyTextSurfaceProvider).GetSurface(0);
		}

		void Init()
		{
			SetupMessages = new List<string>();

			Ingots = new List<IMyTerminalBlock>();
			Components = new List<IMyTerminalBlock>();
			Ores = new List<IMyTerminalBlock>();
			Tools = new List<IMyTerminalBlock>();
			Empty = new List<IMyTerminalBlock>();

			ExternalExporter = new List<ExternalExportDefinition>();

			var allBlocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType(allBlocks, x => x.IsSameConstructAs(Me));

			foreach (var block in allBlocks)
			{
				if(block.InventoryCount > 0)
				{
					int index = block.CustomName.IndexOf(TAG);
					if(index >= 0)
					{
						string part = block.CustomName.Substring(index + TAG.Length);
						part = part.Split(' ')[0];

						switch (part.ToLower())
						{
							case "ingot":
							case "ingots":
								if (block is IMyCargoContainer)
								{
									Ingots.Add(block);
								}
								else
								{
									SetupMessages.Add("Ingot storage can only be in containters. (@'" + block.CustomName + "'')"); ;
								}
								break;

							case "component":
							case "components":
								if (block is IMyCargoContainer)
								{
									Components.Add(block);
								}
								else
								{
									SetupMessages.Add("Component storage can only be in containters. (@'" + block.CustomName + "')");
								}
								break;

							case "ore":
							case "ores":
								if (block is IMyCargoContainer)
								{
									Ores.Add(block);
								}
								else
								{
									SetupMessages.Add("Ore storage can only be in containters. (@'" + block.CustomName + "')");
								}
								break;

							case "tool":
							case "tools":
							case "misc":
								if (block is IMyCargoContainer)
								{
									Tools.Add(block);
								}
								break;

							case "empty":
								Empty.Add(block);
								break;

							
							case "export-ingot":
							case "export-ingots":
								if (block is IMyShipConnector)
								{
									Empty.Add(block);
									ExternalExporter.Add(new ExternalExportDefinition() { connector = block as IMyShipConnector, exportFrom = Ingots });
								}
								else
								{
									SetupMessages.Add("Export can only be set on a connector. (@'" + block.CustomName + "')");
								}
								break;

							case "export-component":
							case "export-components":
								if (block is IMyShipConnector)
								{
									Empty.Add(block);
									ExternalExporter.Add(new ExternalExportDefinition() { connector = block as IMyShipConnector, exportFrom = Components });
								}
								else
								{
									SetupMessages.Add("Export can only be set on a connector. (@'" + block.CustomName + "')");
								}
								break;

							case "export-ore":
							case "export-ores":
								if (block is IMyShipConnector)
								{
									Empty.Add(block);
									ExternalExporter.Add(new ExternalExportDefinition() { connector = block as IMyShipConnector, exportFrom = Ores });
								}
								else
								{
									SetupMessages.Add("Export can only be set on a connector. (@'" + block.CustomName + "')");
								}
								break;

							case "export-tool":
							case "export-tools":
								if (block is IMyShipConnector)
								{
									Empty.Add(block);
									ExternalExporter.Add(new ExternalExportDefinition() { connector = block as IMyShipConnector, exportFrom = Tools });
								}
								else
								{
									SetupMessages.Add("Export can only be set on a connector. (@'" + block.CustomName + "')");
								}
								break;

							case "export-empty":
								if (block is IMyShipConnector)
								{
									Empty.Add(block);
									ExternalExporter.Add(new ExternalExportDefinition() { connector = block as IMyShipConnector, exportFrom = Empty });
								}
								else
								{
									SetupMessages.Add("Export can only be set on a connector. (@'" + block.CustomName + "')");
								}
								break;

							default:
								SetupMessages.Add("Option '" + part + "' wasn't understood. (@'" + block.CustomName + "')");
								break;
						}

					}
				}
			}

			categoryStrings = EqualTextPadding(defaultCategoryStrings, (Me as IMyTextSurfaceProvider).GetSurface(0), ' ', 10);
			categoryStrings[0] += "(" + Ingots.Count + ")";
			categoryStrings[1] += "(" + Components.Count + ")";
			categoryStrings[2] += "(" + Ores.Count + ")";
			categoryStrings[3] += "(" + Tools.Count + ")";
			categoryStrings[4] += "(" + Empty.Count + ")";

			initialized = true;
			RunStateMachine = Run();
		}
		
		public void Main(string argument)
		{
			Echo("Blarg's Basic Inv. Manager");

			if (initialized == false)
			{
				Init();
				if (initialized == false) return;
			}
			
			if (RunStateMachine != null)
			{
				if (!RunStateMachine.MoveNext() || !RunStateMachine.Current)
				{
					RunStateMachine.Dispose();
					RunStateMachine = null;
				}
			}
			DetailedInfo();
		}

		private IEnumerator<bool> Run()
		{
			while (true)
			{
				Errors = 0;
				BlocksMissingInventory = new HashSet<string>();

				//Step 0: Export to other grids

				foreach (var item in ExternalExporter)
				{
					ExternalExport(item.connector, item.exportFrom);
					yield return true;
				}

				//Step 1: Export unwanted.

				foreach (var block in Ingots)
				{
					ExportInventory(block, skipIngots: true);
					yield return true;
				}

				foreach (var block in Ores)
				{
					ExportInventory(block, skipOres: true);
					yield return true;
				}

				foreach (var block in Components)
				{
					ExportInventory(block, skipComponents: true);
					yield return true;
				}

				foreach (var block in Tools)
				{
					ExportInventory(block, skipTools: true);
					yield return true;
				}

				foreach (var block in Empty)
				{
					ExportInventory(block);
					yield return true;
				}

				//Step 2. Organise within (Alphabetical etc)

				TypeFillData fill = new TypeFillData();

				foreach (var block in Ingots)
				{
					fill.Ingots += SortInventory(block);
					yield return true;
				}

				foreach (var block in Ores)
				{
					fill.Ores += SortInventory(block);
					yield return true;
				}

				foreach (var block in Components)
				{
					fill.Components += SortInventory(block);
					yield return true;
				}

				foreach (var block in Tools)
				{
					fill.Tools += SortInventory(block);
					yield return true;
				}

				foreach (var block in Empty)
				{
					fill.Empty += SortInventory(block);
					yield return true;
				}

				fill.Ingots.Z = fill.Ingots.X / fill.Ingots.Y * 100; //Percent = Amount / Total
				fill.Components.Z = fill.Components.X / fill.Components.Y * 100; //Percent = Amount / Total
				fill.Ores.Z = fill.Ores.X / fill.Ores.Y * 100; //Percent = Amount / Total
				fill.Tools.Z = fill.Tools.X / fill.Tools.Y * 100; //Percent = Amount / Total

				ErrorsShow = Errors;
				BlocksMissingInventoryShow = BlocksMissingInventory.ToList();
				FillLevel = fill;
				yield return true;
			}
		}

		void ExternalExport(IMyShipConnector connector, List<IMyTerminalBlock> exportFrom)
		{
			if (connector.Status != MyShipConnectorStatus.Connected) return;
			
			var otherInv = connector.OtherConnector.GetInventory(0);

			if (otherInv == null) return;

			foreach (var block in exportFrom)
			{
				var inv = block.GetInventory(0);
				if (inv == null) continue;
				for (int i = inv.ItemCount - 1; i >= 0; i--)
				{
					if (!otherInv.TransferItemFrom(inv, i)) break;
				}
			}
		}

		Vector3 SortInventory(IMyTerminalBlock block)
		{
			IMyInventory inv;
			if (block.InventoryCount == 2)
			{
				inv = block.GetInventory(1);
			}
			else
			{
				inv = block.GetInventory(0);
			}

			//Abort if inventory is missing
			if(inv == null)
			{
				BlocksMissingInventory.Add(block.CustomName);
				return Vector3.Zero;
			}

			var items = new List<MyInventoryItem>();
			inv.GetItems(items);
			var itemsSorted = items.Distinct().OrderBy(q => q.Type.SubtypeId).ToList(); //Sort alphabetically and remove dupes.

			//Checkas all items in slots >= current slot, for each slot. Sorts and removes dupes.
			for (int i = 0; i < itemsSorted.Count; i++)
			{
				for (int j = i; j < inv.ItemCount; j++)
				{
					var item = inv.GetItemAt(j);

					if (itemsSorted[i] == item)
					{
						//Item fouond

						if (i == j) continue; //Already in the right sport, continue and look for dupes.

						//Move home.
						if(inv.GetItemAt(i) == items[j])
						{
							//Target slot contains same item, merge
							inv.TransferItemFrom(inv, j, inv.ItemCount, true);
							j--; //Check same index again
						}
						else
						{
							//Target slot contains something else, don't merge
							inv.TransferItemFrom(inv, j, inv.ItemCount);
						}
					}
				}
			}
			return new Vector3((float)inv.CurrentVolume, (float)inv.MaxVolume, 0);
		}

		void ExportInventory(IMyTerminalBlock block, bool skipIngots = false, bool skipComponents = false, bool skipOres = false, bool skipTools = false)
		{
			IMyInventory inv;
			if(block.InventoryCount == 2)
			{
				inv = block.GetInventory(1);
			}
			else
			{
				inv = block.GetInventory(0);
			}

			//Abort if inventory is missing
			if (inv == null)
			{
				BlocksMissingInventory.Add(block.CustomName);
				return;
			}

			var items = new List<MyInventoryItem>();
			inv.GetItems(items);
			foreach (var item in items)
			{
				var info = item.Type.GetItemInfo();
				
				if (info.IsIngot)
				{
					//Is ingot
					if (skipIngots) continue;
					if(!ExportItem(item, inv, Ingots)){
						Errors |= RunningError.IngotExportFailed;
					}
					else
					{
						Echo("Moved ingot " + item.Type.SubtypeId);
					}
				}
				else if (info.IsOre)
				{
					//Is ore
					if(skipOres) continue;
					if (!ExportItem(item, inv, Ores))
					{
						Errors |= RunningError.OreExportFailed;
					}
					else
					{
						Echo("Moved ore " + item.Type.SubtypeId);
					}
				}
				else
				{
					//if (info.MaxStackAmount == 1) Bugged in the current version. (1.190.101)
					if(item.Type.TypeId.EndsWith("Object")) //Workaround
					{
						//Is Tool/misc
						if (skipTools) continue;
						if (!ExportItem(item, inv, Tools))
						{
							Errors |= RunningError.ToolExportFailed;
						}
						else
						{
							Echo("Moved tool " + item.Type.SubtypeId);
						}
					}
					else
					{
						//Is component
						if (skipComponents) continue;
						if (!ExportItem(item, inv, Components))
						{
							Errors |= RunningError.ComponentExportFailed;
						}
						else
						{
							Echo("Moved comp " + item.Type.SubtypeId);
						}
					}
				}
			}
		}

		bool ExportItem(MyInventoryItem item, IMyInventory fromInventory, List<IMyTerminalBlock> destinationBlocks)
		{
			VRage.MyFixedPoint amount = item.Amount;

			foreach (var block in destinationBlocks)
			{
				var inv = block.GetInventory(0);

				if (fromInventory == inv) continue;

				if(!inv.IsFull && fromInventory.CanTransferItemTo(inv, item.Type))
				{
					var info = item.Type.GetItemInfo();

					double freeVolume = (double)(inv.MaxVolume - inv.CurrentVolume);

					if (info.Volume <= freeVolume)
					{
						//Move all
						fromInventory.TransferItemTo(inv, item);
						return true;
					}
					else
					{
						//Calculate how much to move
						double part = freeVolume / info.Volume;
						amount *= (VRage.MyFixedPoint)part;
						fromInventory.TransferItemTo(inv, item, amount);
						if (amount == 0) return true;
					}
				}
			}
			return false;
		}

		void DetailedInfo()
		{
			//Print errors
			if (SetupMessages.Count > 0)
			{
				Echo("\n== Setup messages ==");
				for (int i = 0; i < SetupMessages.Count; i++)
				{
					Echo(i + ". " + SetupMessages[i]);
				}
			}
			if (ErrorsShow != 0)
			{
				Echo("\n== Runtime messages ==");
				foreach (var entry in RunningErrorMessages)
				{
					if ((ErrorsShow & entry.Key) != 0)
					{
						Echo(entry.Value);
					}
				}
			}
			if (BlocksMissingInventoryShow.Count > 0)
			{
				Echo("\n== Inaccessible inventory ==\nBlock missing?");
				foreach (var name in BlocksMissingInventoryShow)
				{
					Echo("• " + name);
				}
			}

			var categories = EqualTextPadding(categoryStrings, surface, ' ', 5);
			Echo("\n== Monitored inventroies ==");
			Echo($"{categories[0]}{(FillLevel.Ingots.Y == 0 ? "--" : FillLevel.Ingots.Z.ToString("n0"))}%");
			Echo($"{categories[1]}{(FillLevel.Components.Y == 0 ? "--" : FillLevel.Components.Z.ToString("n0"))}%");
			Echo($"{categories[2]}{(FillLevel.Ores.Y == 0 ? "--" : FillLevel.Ores.Z.ToString("n0"))}%");
			Echo($"{categories[3]}{(FillLevel.Tools.Y == 0 ? "--" : FillLevel.Tools.Z.ToString("n0"))}%");
			Echo($"{categories[4]}{(FillLevel.Empty.Y == 0 ? "--" : FillLevel.Empty.Z.ToString("n0"))}%");

			Echo("\n== Usage ==\nAdd " + TAG + "xxx to a block's name\nwhere xxx is one of the invnetory\ntypes from above. Recompile.");
		}


		// Calculate and add padding for nicely alinged lists
		// - One: 1
		// - Two: 2
		// - Threee: 3
		// Becomes:
		// - One:    1
		// - Two:    2
		// - Threee: 3
		// Roughly.
		string[] EqualTextPadding(string[] strings, IMyTextSurface dummySurface, char padding = ' ', int extraPadding = 0)
		{
			//Calculate pixel lengths and find longest string 
			string[] output = new string[strings.Length];
			float[] lengths = new float[strings.Length];
			int longestStringIndex = 0;
			float longestStringLength = float.MinValue;
			for (int i = 0; i < strings.Length; i++)
			{
				lengths[i] = dummySurface.MeasureStringInPixels(new StringBuilder(strings[i]), "Debug", 1).X;
				if (lengths[i] > longestStringLength)
				{
					longestStringIndex = i;
					longestStringLength = lengths[i];
				}
			}

			float spaceLength = dummySurface.MeasureStringInPixels(new StringBuilder(padding.ToString()), "Debug", 1).X;
			
			for (int i = 0; i < strings.Length; i++)
			{
				output[i] = strings[i] + new string(padding, (int)((lengths[longestStringIndex] - lengths[i]) / spaceLength) + extraPadding);
			}

			return output;
		}




		//to this comment.
		#endregion
		#region post-script
	}
}
#endregion