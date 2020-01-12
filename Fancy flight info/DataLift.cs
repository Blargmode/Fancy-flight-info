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
	class DataLift : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 0;
		public string Unit { get; private set; } = "g";

		IMyShipController controller;
		List<IMyThrust> thrusters = new List<IMyThrust>();
		Dictionary<Data, IData> shipData;
		
		public DataLift(IMyShipController controller, List<IMyTerminalBlock> blocks, Dictionary<Data, IData> shipData)
		{
			this.controller = controller;
			this.shipData = shipData;
			foreach (var block in blocks)
			{
				if (block is IMyThrust)
				{
					thrusters.Add(block as IMyThrust);
				}
			}
		}

		double lift;
		double thrustSum = 0; //In Newton
		double thrustTotal = 0; //In newton
		Vector3D gravity;
		Vector3D desiredDirection;
		Vector3D thisThrustVec;
		Vector3D thisThrustVecTotal;
		public bool Update()
		{
			//Thanks to Whiplash141 for help with this code. 

			gravity = controller.GetNaturalGravity();

			desiredDirection = gravity; //pushing direction
			if (gravity == Vector3D.Zero) desiredDirection = controller.WorldMatrix.Forward;
			desiredDirection = Vector3D.Normalize(-desiredDirection); //normalize the direction vector so we can avoid trig

			thrustSum = 0;
			thrustTotal = 0;

			for (int i = 0; i < thrusters.Count; i++)
			{
				
				if (thrusters[i].WorldMatrix.Backward.Dot(desiredDirection) < 0)
					continue; //skip this thruster if it isnt able to contribute to the desired direction
				
				if (thrusters[i].Enabled)
				{
					thisThrustVec = thrusters[i].WorldMatrix.Backward * thrusters[i].MaxEffectiveThrust; //get a vector with length equal to its max thrust and direction equal to the thrusting direction
					thrustSum += thisThrustVec.Dot(desiredDirection); //this is the projection of the thrust vec on the desired direction. Since the desired direction is normalized, we can simplify the projection to a simple dot product
				}

				thisThrustVecTotal = thrusters[i].WorldMatrix.Backward * thrusters[i].MaxThrust;
				thrustTotal += thisThrustVecTotal.Dot(desiredDirection);
				
			}

			lift = (thrustSum / shipData[Data.Mass].Value) - gravity.Normalize(); //Lift In m/s2
			lift = MathHelper.Clamp(lift / 9.81, 0, double.PositiveInfinity);
			
			Max = (thrustTotal / shipData[Data.Mass].Value) - gravity.Normalize();
			Max = MathHelper.Clamp(Max / 9.81, 0, double.PositiveInfinity);

			if (lift != Value)
			{
				Value = lift;
				return true;
			}
			return false;
		}
	}
	#endregion
}
