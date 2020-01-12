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
	class DataMassCargo : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 100000;
		public string Unit { get; private set; } = "kg";

		IMyShipController Controller;
		double val;
		MyShipMass masses;

		public DataMassCargo(IMyShipController controller)
		{
			Controller = controller;
		}

		public bool Update()
		{
			if (Controller != null)
			{
				//Masses of ship
				masses = Controller.CalculateShipMass();
				val = masses.PhysicalMass - masses.BaseMass;
				if(masses.PhysicalMass == 0)
				{
					val = 0;
				}

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
