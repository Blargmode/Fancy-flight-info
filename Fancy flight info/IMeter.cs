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
	enum Meter
	{
		None,
		Text,
		Value,
		Bar,
		Sprite,
		Meter,
		HalfMeter,
		ThreeQuaterMeter,
		LineGraph,
		Action
	}

	interface IMeter
	{
		void Draw(MySpriteDrawFrame frame, Data dataChanged);
	}
	#endregion
}
