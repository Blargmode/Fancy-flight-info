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
	class DataRuler : IData
	{
		public double Value { get; private set; }
		public double Min { get; private set; } = 0;
		public double Max { get; private set; } = 200;
		public string Unit { get; private set; } = "m";

		List<IMyCameraBlock> cameras = new List<IMyCameraBlock>();


		double val;

		public DataRuler(List<IMyTerminalBlock> blocks)
		{
			foreach (var block in blocks)
			{
				if (block is IMyCameraBlock && block.CustomName.Contains("FFI"))
				{
					cameras.Add(block as IMyCameraBlock);
					cameras[cameras.Count - 1].EnableRaycast = true;
				}
			}
			Max = cameras.Count;
		}

		double distance;
		public bool Update()
		{
			val = double.PositiveInfinity;
			for (int i = 0; i < cameras.Count; i++)
			{
				if (cameras[i].Enabled)
				{
					MyDetectedEntityInfo info = cameras[i].Raycast(200.1, 0, 0);
					if (info.HitPosition.HasValue)
					{
						distance = Vector3D.Distance(cameras[i].GetPosition(), info.HitPosition.Value);
						if (distance < val) val = distance;
					}
				}
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
