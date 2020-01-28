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
	class DisplayManager
	{

		IMyTextSurface pbSurface; //Used for string length calulations
		List<IMyTextSurface> surfaces;
		Program P;

		string[] CategoryStrings;

		//Strings for printing Echo, gets padding by EqualTextPadding()
		readonly string[] DefaultCategoryStrings = new string[]
		{
			"Ingots:",
			"Components:",
			"Ores:",
			"Tools:",
			"Empty:"
		};

		public DisplayManager(Program p)
		{
			p.Echo("a");
			P = p;
			ResetDisplayList();
			
			pbSurface = (P.Me as IMyTextSurfaceProvider).GetSurface(0);
		}

		public void Update()
		{
			StringBuilder text = new StringBuilder();


			if (P.SetupMessages.Count > 0)
			{
				text.AppendLine("\n== Setup messages ==");
				for (int i = 0; i < P.SetupMessages.Count; i++)
				{
					P.Echo(i + ". " + P.SetupMessages[i]);
				}
			}
			if (P.ErrorsShow != 0)
			{
				text.AppendLine("\n== Runtime messages ==");
				foreach (var entry in P.RunningErrorMessages)
				{
					if ((P.ErrorsShow & entry.Key) != 0)
					{
						text.AppendLine(entry.Value);
					}
				}
			}
			if (P.BlocksMissingInventoryShow.Count > 0)
			{
				text.AppendLine("\n== Inaccessible inventory ==\nBlock missing?");
				foreach (var name in P.BlocksMissingInventoryShow)
				{
					text.AppendLine("• " + name);
				}
			}

			if (pbSurface != null)
			{
				//Set up start of string if it hasn't been done yet.
				if(CategoryStrings == null || CategoryStrings.Length == 0)
				{
					CategoryStrings = EqualTextPadding(DefaultCategoryStrings, pbSurface, ' ', 10);
					CategoryStrings[0] += "(" + P.Ingots.Count + ")";
					CategoryStrings[1] += "(" + P.Components.Count + ")";
					CategoryStrings[2] += "(" + P.Ores.Count + ")";
					CategoryStrings[3] += "(" + P.Tools.Count + ")";
					CategoryStrings[4] += "(" + P.Empty.Count + ")";
				}
				
				var categories = EqualTextPadding(CategoryStrings, pbSurface, ' ', 5);
				text.AppendLine("\n== Monitored inventroies ==");
				text.AppendLine($"{categories[0]}{(P.FillLevel.Ingots.Y == 0 ? "--" : P.FillLevel.Ingots.Z.ToString("n0"))}%");
				text.AppendLine($"{categories[1]}{(P.FillLevel.Components.Y == 0 ? "--" : P.FillLevel.Components.Z.ToString("n0"))}%");
				text.AppendLine($"{categories[2]}{(P.FillLevel.Ores.Y == 0 ? "--" : P.FillLevel.Ores.Z.ToString("n0"))}%");
				text.AppendLine($"{categories[3]}{(P.FillLevel.Tools.Y == 0 ? "--" : P.FillLevel.Tools.Z.ToString("n0"))}%");
				text.AppendLine($"{categories[4]}{(P.FillLevel.Empty.Y == 0 ? "--" : P.FillLevel.Empty.Z.ToString("n0"))}%");
			}


			//Print to screens
			foreach (var surface in surfaces)
			{
				if(surface != null)
				{
					surface.WriteText(text);
				}
			}
			
			//Add usage instructions before printing to terminal
			text.AppendLine("\n== Usage ==\nAdd " + Program.TAG + "xxx to a block's name\nwhere xxx is one of the invnetory\ntypes from above. Recompile.");
			P.Echo(text.ToString());
		}

		public bool AddDisplay(IMyTerminalBlock block, int displayIndex)
		{
			var surfaceProvider = block as IMyTextSurfaceProvider;
			if(surfaceProvider != null && surfaceProvider.SurfaceCount > 0)
			{
				if (displayIndex > surfaceProvider.SurfaceCount - 1) displayIndex = surfaceProvider.SurfaceCount - 1;

				surfaces.Add(surfaceProvider.GetSurface(displayIndex));

				surfaces[surfaces.Count - 1].ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;

				return true;
			}

			return false;
		}

		public void ResetDisplayList()
		{
			surfaces = new List<IMyTextSurface>();
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
	}
	#endregion
}
