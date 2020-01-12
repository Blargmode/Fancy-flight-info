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
	class MeterValue : IMeter
	{
		MySprite sprite;
		MeterDefinition def;
		Dictionary<Data, IData> shipData;
		double total = 0;
		string unit = "";
		double abs = 0;
		double val;
		
		bool UseDataMinMax = false;

		public MeterValue(SurfaceMath sm, MeterDefinition def, Dictionary<Data, IData> shipData)
		{
			sm.PostionInterpreter(def);
			this.def = def;
			this.shipData = shipData;

			if (def.size.X == 0) def.size.X = 1;

			def.position += sm.Center;
			def.position.Y -= sm.TextHeight(def.size.X) * 0.5f;

			
			
			if(def.min == 0 && def.max == 0)
			{
				UseDataMinMax = true;
			}
			else
			{
				total = def.max - def.min;
			}

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

			if (def.showUnit)
			{
				switch (def.unit)
				{
					case Units.Percent:
						unit = "%";
						break;
					case Units.Kilo:
						unit = "k";
						break;
					case Units.Mega:
						unit = "M";
						break;
					case Units.Giga:
						unit = "G";
						break;
					case Units.Tera:
						unit = "T";
						break;
				}
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

			if ((dataChanged & def.data) != 0)
			{
				
				if (def.unit == Units.Percent)
				{
					if (UseDataMinMax) total = shipData[def.data].Max - shipData[def.data].Min;
					val = shipData[def.data].Value / total * 100;
					if (double.IsNaN(val)) sprite.Data = "--";
					else if (double.IsInfinity(val)) sprite.Data = "••";
					else sprite.Data = string.Format(def.decimalFormat, val);
					if (def.showUnit) sprite.Data += unit;
				}
				else if (def.unit == Units.Auto && def.data == Data.HydrogenTime)
				{
					if (double.IsNaN(shipData[def.data].Value)) sprite.Data = "--";
					else if (double.IsInfinity(shipData[def.data].Value)) sprite.Data = "••";
					else
					{
						TimeSpan time = TimeSpan.FromSeconds(shipData[def.data].Value);
						if (def.showUnit)
						{
							if (time.TotalDays >= 1)
							{
								sprite.Data = string.Format("{0,2}d {1,2}h", (long)time.TotalDays, (long)time.Hours);
							}
							else if (time.TotalHours >= 1)
							{
								sprite.Data = string.Format("{0,2}h {1,2}m", (long)time.TotalHours, (long)time.Minutes);
							}
							else
							{
								sprite.Data = string.Format("{0,2}m {1,2}s", (long)time.TotalMinutes, (long)time.Seconds);
							}
						}
						else
						{
							sprite.Data = string.Format("{0,2:D2}:{1,2:D2}:{2,2:D2}", (long)time.TotalHours, (long)time.Minutes, (long)time.Seconds);
						}
					}
				}
				else
				{
					if (double.IsNaN(shipData[def.data].Value)) sprite.Data = "--";
					else if (double.IsInfinity(shipData[def.data].Value)) sprite.Data = "••";
					else sprite.Data = string.Format(def.decimalFormat, Ini.ConvertTo(def.unit, shipData[def.data].Value));
					if (def.showUnit)
					{
						if (def.unit == Units.Auto)
						{
							abs = Math.Abs(shipData[def.data].Value);
							if (abs >= 1000000000000) unit = "T";
							else if (abs >= 1000000000) unit = "G";
							else if (abs >= 1000000) unit = "M";
							else if (abs >= 1000) unit = "k";
							else unit = "";
						}
						sprite.Data += unit + shipData[def.data].Unit;
					}


				}

				frame.Add(sprite);
			}
			else
			{
				frame.Add(sprite);
			}
		}

	}
	#endregion
}
