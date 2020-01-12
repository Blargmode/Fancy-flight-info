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
	class DataLandingGear : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 100;
		public string Unit { get; private set; } = "Wh";

		List<IMyLandingGear> landingGears = new List<IMyLandingGear>();


		double val;

		public DataLandingGear(List<IMyTerminalBlock> blocks)
		{
			foreach (var block in blocks)
			{
				if (block is IMyLandingGear)
				{
					landingGears.Add(block as IMyLandingGear);
				}
			}
			Max = landingGears.Count;
		}

		public bool Update()
		{
			val = 0;
			for (int i = 0; i < landingGears.Count; i++)
			{
				if (landingGears[i].IsLocked) val++;
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
