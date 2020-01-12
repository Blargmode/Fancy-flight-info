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
	class DataPowerUsage : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 1;
		public string Unit { get; private set; } = "W";

		List<IMyPowerProducer> producers = new List<IMyPowerProducer>();


		double val;
		double total;

		public DataPowerUsage(List<IMyTerminalBlock> blocks, string typeID)
		{
			//GetBlocksOfType<IMyPowerProducer>(blocks, (b) => b.BlockDefinition.TypeIdString == "MyObjectBuilder_WindTurbine");
			//"MyObjectBuilder_HydrogenEngine"
			foreach (var block in blocks)
			{
				if (block is IMyPowerProducer && block.BlockDefinition.TypeIdString == typeID)
				{
					producers.Add(block as IMyPowerProducer);
					total += producers[producers.Count - 1].MaxOutput * 1000000; //Convert from MW to W
				}
			}
			Max = total;
		}

		public DataPowerUsage(List<IMyTerminalBlock> blocks)
		{
			foreach (var block in blocks)
			{
				if (block is IMyPowerProducer)
				{
					producers.Add(block as IMyPowerProducer);
					total += producers[producers.Count - 1].MaxOutput * 1000000; //Convert from MW to W
				}
			}
			Max = total;
		}

		public bool Update()
		{
			val = 0;
			for (int i = 0; i < producers.Count; i++)
			{
				val += producers[i].CurrentOutput * 1000000; //Convert from MW to W
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
