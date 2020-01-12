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
	class DataJumpDriveDistance : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 100;
		public double Max { get; private set; } = 100;
		public string Unit { get; private set; } = "km";

		List<IMyJumpDrive> jumpDrives = new List<IMyJumpDrive>();
		Dictionary<Data, IData> shipData;

		double val;
		float minJump = 100; //is percent.
		double maxDistance = 0;

		public DataJumpDriveDistance(List<IMyTerminalBlock> blocks, Dictionary<Data, IData> shipData)
		{
			this.shipData = shipData;
			foreach (var block in blocks)
			{
				if (block is IMyJumpDrive)
				{
					jumpDrives.Add(block as IMyJumpDrive);
				}
			}
		}

		public bool Update()
		{
			val = 0;
			minJump = 100;
			maxDistance = 0;
			for (int i = 0; i < jumpDrives.Count; i++)
			{
				if (jumpDrives[i].Enabled)
				{
					minJump = Math.Min(minJump, jumpDrives[i].GetValue<float>("JumpDistance"));

					maxDistance += 2000 * (1250000 / shipData[Data.Mass].Value); //2000000 if using meters
				}
			}
			maxDistance = MathHelper.Clamp(maxDistance, 0, 2000);
			Max = maxDistance;
			val = maxDistance * (minJump / 100f);

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
