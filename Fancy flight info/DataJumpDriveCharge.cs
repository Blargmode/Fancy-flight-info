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
	class DataJumpDriveCharge : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 100;
		public string Unit { get; private set; } = "Wh";

		List<IMyJumpDrive> jumpDrives = new List<IMyJumpDrive>();


		double val;
		double total;

		public DataJumpDriveCharge(List<IMyTerminalBlock> blocks)
		{
			foreach (var block in blocks)
			{
				if (block is IMyJumpDrive)
				{
					jumpDrives.Add(block as IMyJumpDrive);
					total += jumpDrives[jumpDrives.Count - 1].MaxStoredPower * 1000000; //Convert from MW to W
				}
			}
			Max = total;
		}

		public bool Update()
		{
			val = 0;
			for (int i = 0; i < jumpDrives.Count; i++)
			{
				val += jumpDrives[i].CurrentStoredPower * 1000000; //Convert from MW to W
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
