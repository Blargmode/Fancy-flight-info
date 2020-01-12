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
	class Profiler
	{
		double[] lastRuntimes;
		long count = 0;
		int sampleSize;
		IMyProgrammableBlock Me;
		bool hasPrinted = false;

		public double Avrage { get; private set; }
		public double Peak { get; private set; }

		public Profiler(IMyProgrammableBlock Me, int sampleSize = 60, int waitCycles = 0)
		{
			this.Me = Me;
			this.sampleSize = sampleSize;
			lastRuntimes = new double[sampleSize];
			count -= waitCycles;
		}

		public float Update(double lastRuntimeMs)
		{
			if(count >= sampleSize)
			{
				if (!hasPrinted)
				{
					Avrage = lastRuntimes.Average();
					Peak = lastRuntimes.Max();
					hasPrinted = true;
					Me.CustomData = string.Join("\n", lastRuntimes.Select(p => p.ToString()));
				}
				return 1f;
			}

			else if(count < 0)
			{
				count++;
				return 0f;
			}

			lastRuntimes[count] = lastRuntimeMs;

			count++;
			return (float) count / sampleSize;
		}
	}
	#endregion
}
