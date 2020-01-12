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
	class DataHydrogenTime : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 1;
		public string Unit { get; private set; } = "s";

		Program p;
		Dictionary<Data, IData> shipData;
		const int VALUES = 10;
		double[] values = new double[VALUES];
		int index;
		double val;
		double lastValue = 0;
		TimeSpan lastTime;

		public DataHydrogenTime(Dictionary<Data, IData> shipData, Program p)
		{
			this.shipData = shipData;
			this.p = p;
		}
		
		public bool Update()
		{
			//time = distance / rate

			double rate = MathHelperD.Clamp(lastValue - shipData[Data.Hydrogen].Value, 0, double.MaxValue) / (p.Time - lastTime).TotalSeconds; //rate = amount / time

			double distance = shipData[Data.Hydrogen].Value;
			
			double time = distance / rate;

			values[index] = time;
			index++;
			if(index >= VALUES) index = 0;

			val = values.Average();
			
			lastValue = shipData[Data.Hydrogen].Value;
			lastTime = p.Time;

			if (double.IsNaN(val)) val = 0;

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
