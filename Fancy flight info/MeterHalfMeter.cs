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
	class MeterHalfMeter : IMeter
	{
		MySprite full;
		MySprite semi;
		MySprite box;
		MySprite inner;

		SurfaceMath sm;
		MeterDefinition def;
		Dictionary<Data, IData> shipData;
		double total = 0;
		Color background;
		bool UseDataMinMax = false;
		double val;

		public MeterHalfMeter(SurfaceMath sm, MeterDefinition def, Dictionary<Data, IData> shipData, Color background)
		{
			this.sm = sm;
			sm.PostionInterpreter(def);
			this.def = def;
			this.shipData = shipData;
			this.background = background;
			def.position += sm.Center;

			if (def.min == 0 && def.max == 0)
			{
				UseDataMinMax = true;
			}
			else
			{
				total = def.max - def.min;
			}

			float innerSize = MathHelper.Clamp(1 - def.stroke, 0 , 1);
			def.size.Y = def.size.X; //Make square. Y is used for cicle thickness.

			full = new MySprite(SpriteType.TEXTURE, "Circle", def.position, color: def.color);
			semi = new MySprite(SpriteType.TEXTURE, "SemiCircle", def.position, def.size);
			box = new MySprite(SpriteType.TEXTURE, "SquareSimple", color: background);
			inner = new MySprite(SpriteType.TEXTURE, "Circle", def.position, def.size * innerSize, color: background);

			if (def.backgroundSet)
			{
				semi.Color = def.background;
				full.Size = def.size;
			}
			else
			{
				semi.Color = background;
				full.Size = def.size * 0.97f; //Shrinking to prevent color shining through. Looks weird with background so skipping it there.
			}
			
			box.RotationOrScale =  def.rotation;
			box.Size = new Vector2(def.size.X, def.size.Y * 0.5f);
			box.Position = sm.AdjustToRotation(new Vector2(def.position.X, def.position.Y + def.size.Y * 0.25f), def.position, def.rotation);
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
				
				semi.RotationOrScale = MathHelper.ToRadians(MathHelper.Clamp((float)(shipData[def.data].Value / total * 180f), 0, 180)) + def.rotation;

				frame.Add(full);
				frame.Add(semi);
				frame.Add(inner);
				frame.Add(box);
			}
			else
			{
				frame.Add(full);
				frame.Add(semi);
				frame.Add(inner);
				frame.Add(box);
			}
		}
	}
	#endregion
}
