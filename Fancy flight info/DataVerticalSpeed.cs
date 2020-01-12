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
	class DataVerticalSpeed : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = -100;
		public double Max { get; private set; } = 100;
		public string Unit { get; private set; } = "m/s";
		private double newValue = 0;

		IMyShipController Controller;

		public DataVerticalSpeed(IMyShipController controller)
		{
			Controller = controller;
		}
		
		public bool Update()
		{
			//Speed towards the center of nearest gravity well.
			if (Controller != null)
			{
				newValue = -Vector3.Dot(Controller.GetShipVelocities().LinearVelocity,
											Vector3D.Normalize(Controller.GetNaturalGravity()));
				if (newValue != Value)
				{
					Value = newValue;
					return true;
				}
			}
			return false;
		}
	}
	#endregion
}
