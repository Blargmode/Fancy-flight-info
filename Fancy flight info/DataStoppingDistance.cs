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
	class DataStoppingDistance : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 200;
		public string Unit { get; private set; } = "m";

		List<IMyThrust> thrusters = new List<IMyThrust>();
		IMyShipController controller;
		Dictionary<Data, IData> shipData;


		double val;

		public DataStoppingDistance(IMyShipController controller, List<IMyTerminalBlock> blocks, Dictionary<Data, IData> shipData)
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
			Max = thrusters.Count;
		}

		double force;
		Vector3D grav;
		Vector3D vel;
		double gravInHeading;
		public bool Update()
		{
			grav = controller.GetNaturalGravity();
			vel = controller.GetShipVelocities().LinearVelocity;
			gravInHeading = Vector3D.Dot(grav, Vector3D.Normalize(vel));
			force = ForceInDirection(Vector3D.Normalize(vel), thrusters);

			val = StoppingDistance(shipData[Data.Mass].Value, force, vel.Length(), gravInHeading);

			if (val != Value)
			{
				Value = val;
				return true;
			}
			return false;
		}

		double StoppingDistance(double mass, double force, double velocity, double gravity)
		{
			//Acceleration, from F = m * a
			double acceleration = force / mass - gravity;

			//Math from https://physics.stackexchange.com/a/3821
			double distance = (velocity * velocity) / (2 * acceleration);
			return distance;
		}
		
		double thrustSum = 0; //In Newton
		Vector3D desiredDirection;
		Vector3D thisThrustVec;
		double ForceInDirection(Vector3D direction, List<IMyThrust> thrusters)
		{
			desiredDirection = direction; //pushing direction
			desiredDirection = Vector3D.Normalize(-desiredDirection); //normalize the direction vector so we can avoid trig

			thrustSum = 0;

			for (int i = 0; i < thrusters.Count; i++)
			{

				if (thrusters[i].WorldMatrix.Backward.Dot(desiredDirection) < 0)
					continue; //skip this thruster if it isnt able to contribute to the desired direction
				
				thisThrustVec = thrusters[i].WorldMatrix.Backward * thrusters[i].MaxEffectiveThrust; //get a vector with length equal to its max thrust and direction equal to the thrusting direction

				thrustSum += thisThrustVec.Dot(desiredDirection); //this is the projection of the thrust vec on the desired direction. Since the desired direction is normalized, we can simplify the projection to a simple dot product
			}
			
			return thrustSum;
		}
	}
	#endregion
}
