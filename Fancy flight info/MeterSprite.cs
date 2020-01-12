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
	class MeterSprite : IMeter
	{
		MySprite sprite;
		MeterDefinition def;
		Dictionary<Data, IData> shipData;

		bool UseDataMinMax = false;
		double total = 0;
		double val = 0;

		public MeterSprite(SurfaceMath sm, MeterDefinition def, Dictionary<Data, IData> shipData)
		{
			sm.PostionInterpreter(def);
			this.def = def;
			this.shipData = shipData;
			def.position += sm.Center;

			if (def.min == 0 && def.max == 0)
			{
				UseDataMinMax = true;
			}
			else
			{
				total = def.max - def.min;
			}

			sprite = new MySprite(SpriteType.TEXTURE, def.textData, color: def.color);
			sprite.Size = def.size;
			sprite.Position = sm.AdjustToRotation(sm.AdjustToAnchor(def.anchor, def.position, def.size), def.position, def.rotation);
			sprite.RotationOrScale = def.rotation;
		}

		public void Draw(MySpriteDrawFrame frame, Data dataChanged)
		{
			if (def.data != Data.None)
			{
				val = Ini.AdjustToUnit(def, shipData, total, UseDataMinMax);

				//Check for hide condition.
				if (Ini.MeetsConditions(def.conditions, val, def.condVals)) return;
			}

			frame.Add(sprite);
		}
	}
	#endregion
}
