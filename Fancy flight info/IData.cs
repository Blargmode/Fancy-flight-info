#region pre-script
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
#endregion
namespace IngameScript
{
	#region in-game
	[Flags]
	enum Data
	{
		None = 0,
		Pulse = 1,
		Speed = 2,
		VerticalSpeed = 4,
		Altitude = 8,
		AltitudeSea = 16,
		Mass = 32,
		MassCargo = 64,
		Gravity = 128,
		Dampeners = 256,
		Hydrogen = 512,
		Oxygen = 1024,
		BatteryCharge = 2048,
		BatteryUsage = 4096,
		JumpDriveCharge = 8192,
		SolarUsage = 16384,
		ReactorUsage = 32768,
		HydrogenTime = 65636,
		EngineUsage = 131272,
		PowerUsage = 262544,
		WindUsage = 525088,
		JumpDriveDistance = 1050176,
		Lift = 2100352,
		LandingGears = 4200704,
		Connectors = 8401408,
		Seated = 16802816,
		InMainSeat = 33605632,
		Ruler = 67211264,
		StoppingDistance = 134422528,
		Handbrake = 268845056
	}

	interface IData
	{
		double Value { get; }
		double Min { get; }
		double Max { get; }

		string Unit { get; }

		//Return true if value has changed
		bool Update();
	}
	#endregion
}
