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
	class DataPulse : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 100;
		public string Unit { get; private set; } = "t";

		int counter = 0;
		bool up = true;
		
		public DataPulse()
		{
		}

		public bool Update()
		{
			if (counter >= Max) up = false;
			else if (counter <= Min) up = true;
			if (up) counter += 2;
			else counter -= 2;

			Value = counter;
			return true; //Change to false to test screen drawing pausing.
		}
	}
	#endregion
}
