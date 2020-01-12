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
	class MeterAction : IMeter
	{
		MeterDefinition def;
		Dictionary<Data, IData> shipData;
		List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

		bool conditionMet = true;	//For only running when the conditionsis met, an not every time while it's true.
									//True to not trigger at start


		bool UseDataMinMax = false;
		double total = 0;
		double val = 0;

		public MeterAction(SurfaceMath sm, MeterDefinition def, Dictionary<Data, IData> shipData, List<IMyTerminalBlock> blocks, IMyGridTerminalSystem gts, IMyProgrammableBlock me)
		{
			this.def = def;
			this.shipData = shipData;

			if (def.textData == "") def.textData = "OnOff";

			if (def.min == 0 && def.max == 0)
			{
				UseDataMinMax = true;
			}
			else
			{
				total = def.max - def.min;
			}

			foreach (string text in def.blocks)
			{
				if(text.StartsWith("*") && text.EndsWith("*"))
				{
					//Group.
					var group = gts.GetBlockGroupWithName(text.Trim('*'));
					if(group != null)
					{
						var groupBlocks = new List<IMyTerminalBlock>();
						group.GetBlocks(groupBlocks);
						foreach (var block in groupBlocks)
						{
							if(block.IsSameConstructAs(me)) this.blocks.Add(block);
						}
					}
				}
				else
				{
					foreach (var block in blocks)
					{
						if(block.CustomName == text)
						{
							this.blocks.Add(block);
							break;
						}
					}
				}
			}

		}

		public void Draw(MySpriteDrawFrame frame, Data dataChanged)
		{
			//if(def.unit == Units.Percent)
			//{
			//	if (UseDataMinMax)
			//	{
			//		total = shipData[def.data].Max - shipData[def.data].Min;
			//	}
			//	val = shipData[def.data].Value / total * 100;
			//}
			//else if(def.unit == Units.Default) 
			//{
			//	val = shipData[def.data].Value;
			//}
			//else
			//{
			//	val = Ini.ConvertTo(def.unit, shipData[def.data].Value);
			//}
			if ((dataChanged & def.data) != 0)
			{
				val = Ini.AdjustToUnit(def, shipData, total, UseDataMinMax);


				if (Ini.MeetsConditions(def.conditions, val, def.condVals))
				{
					if (!conditionMet) Execute();
					conditionMet = true;
				}
				else
				{
					conditionMet = false;
				}
			}
		}

		private void Execute()
		{
			try
			{
				foreach (var block in blocks) block.ApplyAction(def.textData);
			}
			finally
			{

			}
		}


	}
	#endregion
}
