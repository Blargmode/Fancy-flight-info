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
	class MeterBar : IMeter
	{
		MySprite sprite;
		MySprite background;
		SurfaceMath sm;
		MeterDefinition def;
		Dictionary<Data, IData> shipData;
		double total = 0;
		Vector2 spriteSize = Vector2.Zero;
		bool UseDataMinMax = false;
		double val;

		public MeterBar(SurfaceMath sm, MeterDefinition def, Dictionary<Data, IData> shipData)
		{
			this.sm = sm;
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

			sprite = new MySprite(SpriteType.TEXTURE, "SquareSimple", color: def.color);
			sprite.Position = sm.AdjustToRotation(sm.AdjustToAnchor(def.anchor, def.position, def.size), def.position, def.rotation);
			spriteSize.Y = def.size.Y;
			sprite.RotationOrScale = def.rotation;

			if (def.backgroundSet)
			{
				background = new MySprite(SpriteType.TEXTURE, "SquareSimple", color: def.background);
				background.Position = sm.AdjustToRotation(sm.AdjustToAnchor(def.anchor, def.position, def.size), def.position, def.rotation);
				background.Size = def.size;
				background.RotationOrScale = def.rotation;
			}

		}

		public void Draw(MySpriteDrawFrame frame, Data dataChanged)
		{
			if (UseDataMinMax)
			{
				def.min = shipData[def.data].Min;
				def.max = shipData[def.data].Max;
				total = def.max - def.min;
			}
			val = Ini.AdjustToUnit(def, shipData, total, UseDataMinMax);

			//Check for hide condition.
			if (Ini.MeetsConditions(def.conditions, val, def.condVals)) return;

			if ((dataChanged & def.data) != 0)
			{
				spriteSize.X = (float)Math.Abs(MathHelper.Clamp(shipData[def.data].Value, def.min, def.max) / total * def.size.X);
				//_sprite.Position = sm.AdjustToAnchor(def.anchor, def.position, spriteSize);
				sprite.Position = sm.AdjustToRotation(sm.AdjustToAnchor(def.anchor, def.position, spriteSize), def.position, def.rotation);
				sprite.Size = spriteSize;
				if (def.backgroundSet) frame.Add(background);
				frame.Add(sprite);
			}
			else
			{
				sprite.Size = spriteSize;
				if (def.backgroundSet) frame.Add(background);
				frame.Add(sprite);
			}
		}

	}
	#endregion
}
