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
	class DataSeated : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 0;
		public string Unit { get; private set; } = "#";

		List<IMyShipController> controllers = new List<IMyShipController>();


		double val;

		public DataSeated(List<IMyTerminalBlock> blocks)
		{
			foreach (var block in blocks)
			{
				if (block is IMyShipController && !(block is IMyRemoteControl))
				{
					controllers.Add(block as IMyShipController);
				}
			}
			Max = controllers.Count;
		}

		public bool Update()
		{
			val = 0;
			for (int i = 0; i < controllers.Count; i++)
			{
				if (controllers[i].IsUnderControl) val++;
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
