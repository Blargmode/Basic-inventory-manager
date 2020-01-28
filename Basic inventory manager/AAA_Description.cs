using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
	public partial class Program : MyGridProgram
	{
		#region untouched
		/*
		Blargmode's Basic Inventory Manager (BIM)
		Version 0.0.4 (2020-01-28)


		// Forewords \\__________________________________________________
		This script is a work in progress. I don't know if I'll post it to the workshop yet.
		You are free to use it in ships you build, including ones uploaded to the workshop,
		but please don't upload the script on its own to the workshop. I'd like to retain
		that privilege. Thank you.


		// Key features \\__________________________________________________
		- Sorts stuff into 4 categories.
		- Does not touch inventories you haven't specified.
		- Works on an 'export unwanted' principle. 
		- Can export to other grids (errr.. breaking feature 2, more on that later..)
		- You need to recompile if you've made any changes, like tagging or building blocks.
		- Can auto-queue components in the assembler (Check Custom Data of the PB).


		// How it works \\__________________________________________________
		It's designed to work in unison with the games own inventory management. I.e. refineries, 
		oxygen generators, reactors, etc.. pulling what they need. 
		In other words, it doesn't do any of that (remember; basic is in the name).

		There are 5 types of containers in this script:
		- BIM:Ores
		- BIM:Ingots
		- BIM:Components
		- BIM:Tools (things that don't stack).
		- BIM:Empty

		You can specify what can exist in a container using these tags in the block name.
		Once you've tagged a block you need to recompile the script.

		A block tagged like this will try to export anything that doesn't belong in its inventory.
		It will not export to an inventory with no tag.

		If you tag something with two inventories like Refineries it will use the second one.

		I usually put the 'BIM:Empty' tag on Refineries and connectors. Drills not needed since
		they empty themselves anyway.


		// Exporting to other ships \\__________________________________________________
		This feature is meant for emptying mining ships and similar.
		You cannot limit how much it exports.

		Tag a connector with one of these:
		- BIM:export-ores
		- BIM:export-ingots
		- BIM:export-components
		- BIM:export-tools
		- BIM:export-empty

		When that connector is connected to another, it will pull from corresponding containers
		and stuff it in the other connector. If the other ship doesn't have an inventory manager
		emptying the connector, then though luck. I tag receiving connectors with 'BIM:Empty'. 

		An export connector is automatically also a 'BIM:Empty' inventory. This allows for two
		way item transfer. 


		// Setting \\__________________________________________________
		You can change the tag.
		*/
		const string TAG = "BIM:";
/*
But don't touch anything beyond this point.
Fly safe!
*/




















		#endregion
	}
}
