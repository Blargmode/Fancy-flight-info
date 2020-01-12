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
	class MeterLineGraph : IMeter
	{
		const int LINES = 10;
		const float SECTION = 0.1f; //Based on LINES. Change this if you change LINES

		MySprite[] sprites = new MySprite[LINES];
		MySprite background;

		SurfaceMath sm;
		MeterDefinition def;
		Dictionary<Data, IData> shipData;
		double total = 0;
		double[] values = new double[LINES];
		int valueIndex = 0;
		float thickness = 5;
		Vector2 pos;
		bool UseDataMinMax = false;
		double val;

		public MeterLineGraph(SurfaceMath sm, MeterDefinition def, Dictionary<Data, IData> shipData)
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

			for (int i = 0; i < LINES; i++)
			{
				sprites[i] = new MySprite(SpriteType.TEXTURE, "SquareSimple", color: def.color);
			}

			pos = def.position;

			pos.X -= def.size.X * 0.5f - (def.size.X * SECTION * 0.5f);
			pos.Y += def.size.Y * 0.5f - (thickness * 0.5f);
			def.size.Y -= thickness;

			valueIndex = LINES - 1;

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
				values[valueIndex] = shipData[def.data].Value;
				
				if (def.backgroundSet) frame.Add(background);

				int prev = LINES - 1;
				int value = (valueIndex + 1) % LINES;
				Vector2 prevPos = sm.AdjustToRotation(new Vector2((pos.X), (float)(pos.Y - values[value] / total * def.size.Y)), def.position, def.rotation);
				for (int i = 0; i < LINES; i++)
				{
					value = (valueIndex + i + 1) % LINES;

					//1. Calc distance between points, fow rect width.
					//2. Calc rotation between points, for rect rotation
					//3. Calc point between points, for rect position
					//4. Place rect there.

					Vector2 newPos = sm.AdjustToRotation(new Vector2((pos.X + def.size.X * SECTION * i), (float)(pos.Y - values[value] / total * def.size.Y)), def.position, def.rotation);

					sprites[i].Position = Vector2.Lerp(newPos, prevPos, 0.5f);

					sprites[i].Size = new Vector2(Vector2.Distance(prevPos, newPos), thickness);

					sprites[i].RotationOrScale = (float)Math.Atan2(newPos.Y - prevPos.Y, newPos.X - prevPos.X);

					if (values[value] <= def.max && value >= def.min)
						frame.Add(sprites[i]);

					prevPos = newPos;
					prev = i;
				}

				valueIndex++;
				if (valueIndex >= LINES) valueIndex = 0;
			}
			else
			{
				if (def.backgroundSet) frame.Add(background);
				for (int i = 0; i < LINES; i++)
				{
					frame.Add(sprites[i]);
				}
			}
		}
	}
	#endregion
}
