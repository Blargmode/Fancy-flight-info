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
	class DataHydrogen : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 100;
		public string Unit { get; private set; } = "l";
		
		List<IMyGasTank> tanks = new List<IMyGasTank>();


		double val;
		double total;

		public DataHydrogen(List<IMyTerminalBlock> blocks)
		{
			foreach (var block in blocks)
			{
				if(block is IMyGasTank)
				{
					if (block.BlockDefinition.SubtypeId.Contains("Hydrogen"))
					{
						tanks.Add(block as IMyGasTank);
						total += tanks[tanks.Count -1 ].Capacity;
					}
				}
			}
			Max = total;
		}

		public bool Update()
		{
			val = 0;
			for (int i = 0; i < tanks.Count; i++)
			{
				val += tanks[i].FilledRatio * tanks[i].Capacity;
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
