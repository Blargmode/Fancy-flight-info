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
	class DataBatteryCharge : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 100;
		public string Unit { get; private set; } = "Wh";

		List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();


		double val;
		double total;

		public DataBatteryCharge(List<IMyTerminalBlock> blocks)
		{
			foreach (var block in blocks)
			{
				if (block is IMyBatteryBlock)
				{
					batteries.Add(block as IMyBatteryBlock);
					total += batteries[batteries.Count - 1].MaxStoredPower * 1000000; //Convert from MW to W
				}
			}
			Max = total;
		}

		public bool Update()
		{
			val = 0;
			for (int i = 0; i < batteries.Count; i++)
			{
				val += batteries[i].CurrentStoredPower * 1000000; //Convert from MW to W
			}

			if (val != Value)
			{
				Value = val;
				return true;
			}
			return false;
		}
	}
	#endregion
}
