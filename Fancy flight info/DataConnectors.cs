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
	class DataConnectors : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 100;
		public string Unit { get; private set; } = "Wh";

		List<IMyShipConnector> connectors = new List<IMyShipConnector>();


		double val;

		public DataConnectors(List<IMyTerminalBlock> blocks)
		{
			foreach (var block in blocks)
			{
				if (block is IMyShipConnector)
				{
					connectors.Add(block as IMyShipConnector);
				}
			}
			Max = connectors.Count;
		}

		public bool Update()
		{
			val = 0;
			for (int i = 0; i < connectors.Count; i++)
			{
				if (connectors[i].Status == MyShipConnectorStatus.Connected) val++;
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
