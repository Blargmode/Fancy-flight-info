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
	class MeterText : IMeter
	{
		MySprite sprite;
		MeterDefinition def;
		Dictionary<Data, IData> shipData;

		bool UseDataMinMax = false;
		double total = 0;
		double val = 0;

		public MeterText(SurfaceMath sm, MeterDefinition def, Dictionary<Data, IData> shipData)
		{
			sm.PostionInterpreter(def);
			this.def = def;
			this.shipData = shipData;

			if (def.min == 0 && def.max == 0)
			{
				UseDataMinMax = true;
			}
			else
			{
				total = def.max - def.min;
			}

			if (def.size.X == 0) def.size.X = 1;

			def.position += sm.Center;
			def.position.Y -= sm.TextHeight(def.size.X) * 0.5f;

			sprite = MySprite.CreateText(def.textData, "Debug", def.color, def.size.X);
			sprite.Position = def.position;
			switch (def.anchor)
			{
				case Anchor.Left:
					sprite.Alignment = TextAlignment.LEFT;
					break;
				case Anchor.Right:
					sprite.Alignment = TextAlignment.RIGHT;
					break;
				case Anchor.Center:
				default:
					sprite.Alignment = TextAlignment.CENTER;
					break;
			}
			
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
