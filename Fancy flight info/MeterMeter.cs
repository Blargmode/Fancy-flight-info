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
	class MeterMeter : IMeter
	{
		MySprite full;
		MySprite semi;
		MySprite top;
		MySprite bottom;
		MySprite inner;

		SurfaceMath sm;
		MeterDefinition def;
		Dictionary<Data, IData> shipData;
		double total = 0;
		Color background;
		bool UseDataMinMax = false;
		double val;

		public MeterMeter(SurfaceMath sm, MeterDefinition def, Dictionary<Data, IData> shipData, Color background)
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

			float innerSize = MathHelper.Clamp(1 - def.stroke, 0, 1);
			def.size.Y = def.size.X; //Make square. Y is used for cicle thickness.

			full = new MySprite(SpriteType.TEXTURE, "Circle", def.position, color: def.color);
			semi = new MySprite(SpriteType.TEXTURE, "SemiCircle", def.position, def.size);
			top = new MySprite(SpriteType.TEXTURE, "SemiCircle", def.position, color: def.color);
			bottom = new MySprite(SpriteType.TEXTURE, "SemiCircle", def.position, def.size);
			inner = new MySprite(SpriteType.TEXTURE, "Circle", def.position, def.size * innerSize, color: background);

			if (def.backgroundSet)
			{
				semi.Color = def.background;
				bottom.Color = def.background;
				full.Size = def.size;
				top.Size = def.size;
			}
			else
			{
				semi.Color = background;
				bottom.Color = background;
				full.Size = def.size * 0.97f; //Shrinking to prevent color shining through. Looks weird with background so skipping it there.
				top.Size = def.size * 0.97f;
			}

			top.RotationOrScale = def.rotation;
			bottom.RotationOrScale = MathHelper.ToRadians(180f) + def.rotation;
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
				
				val = MathHelper.Clamp(shipData[def.data].Value, def.min, def.max) / total;
				semi.RotationOrScale = (float) MathHelper.ToRadians(val * 360f) + def.rotation;

				frame.Add(full);

				if (val > 0.5)
				{
					frame.Add(semi);
					frame.Add(top);
				}
				else
				{
					frame.Add(semi);
					frame.Add(bottom);
				}
				frame.Add(inner);
			}
			else
			{
				frame.Add(full);

				if (val > 0.5)
				{
					frame.Add(semi);
					frame.Add(top);
				}
				else
				{
					frame.Add(semi);
					frame.Add(bottom);
				}
				frame.Add(inner);
			}
		}
	}
	#endregion
}
