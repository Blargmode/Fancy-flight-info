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
	class DataDampeners : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 1;
		public string Unit { get; private set; } = "-";

		IMyShipController Controller;
		double val;

		public DataDampeners(IMyShipController controller)
		{
			Controller = controller;
		}

		public bool Update()
		{
			if (Controller != null)
			{
				val = (Controller.DampenersOverride ? 1 : 0);

				if (val != Value)
				{
					Value = val;
					return true;
				}
			}
			return false;
		}
	}
	#endregion
}
