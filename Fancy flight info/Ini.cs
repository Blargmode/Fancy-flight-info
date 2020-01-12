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
	class Ini
	{
		enum Header
		{
			None,
			FFI,
			Meter
		}

		public bool NoErrors { get; private set; } = true;
		public string Processed { get; private set; }
		public List<SkinDefinition> SkinDataList { get; private set; } = new List<SkinDefinition>();

		IEnumerator<bool> IniStateMachine;
		List<string> lines;
		SkinDefinition skinData = null;
		MeterDefinition meter = null;
		Header header = Header.None;
		int surfaceCount;
		List<string> sprites;
		bool finnishedIni = false;
		bool inject = false;
		string injectKey = "";

		Dictionary<string, string> preloads;

		public Ini(string input, int surfaceCount, List<string> sprites, Dictionary<string, string> preloads)
		{
			this.surfaceCount = surfaceCount;
			this.sprites = sprites;
			this.preloads = preloads;
			lines = input.Split('\n').ToList();
			IniStateMachine = ParseIni();
		}

		public bool ParserDone()
		{
			if (!finnishedIni)
			{
				if (IniStateMachine != null)
				{
					if (!IniStateMachine.MoveNext() || !IniStateMachine.Current)
					{
						IniStateMachine.Dispose();
						IniStateMachine = null;
					}
				}
				return false;
			}
			else
			{
				return true;
			}
		}
		
		/// <summary>
		/// Takes an INI text and spits out commands to run
		/// </summary>
		/// <param name="input">Text in INI format</param>
		/// <param name="processed">Outputs the input plus error messages</param>
		/// <returns>True if there were no errors at all</returns>
		private IEnumerator<bool> ParseIni()
		{
			//TODO out List<Command>
			//Whatever Command will be...
			//Some sort of class that can do the whole command
			//Point is, this method should be functinal, ie nothing external modified whithin
			//All in/outputs should be done via returns and outs.

			
			for (int i = 0; i < lines.Count; i++)
			{
				// • Trim exess indentation etc.
				string trimmed = lines[i].Trim();


				// • Stop parsing if line is empty
				if (string.IsNullOrWhiteSpace(lines[i]) || trimmed.StartsWith("---"))
				{
					//inject preload
					if (inject)
					{
						inject = false;
						Inject(i, injectKey);
					}

					header = Header.None;
					continue;
				}

				// • Skip line if it's a comment
				if (trimmed.StartsWith(";"))
				{
					continue;
				}

				// • Parse headers
				if (lines[i].StartsWith("["))
				{
					//This is a header, probalby
					//Don't care about it being properly fomrated with end bracked and such. The point comes across anyway..

					trimmed = trimmed.Trim(new char[] { '[', ']' });

					if (trimmed.StartsWith("FFI"))
					{
						//it's a FFI header!
						//Cant just check if header is only "FFI", MyIni can't take that. Needs unique headers...
						//So, check if the next line is screen. Then it's the main header.
						if(lines.Count > i + 1)
						{
							if (lines[i + 1].ToLower().StartsWith("screen"))
							{
								header = Header.FFI;
								//Save last skin data
								if (skinData != null)
								{
									SkinDataList.Add(skinData);
									//Save last meter
									if (meter != null)
									{
										skinData.meters.Add(meter);
									}
								}
								//Begin new skin data
								skinData = new SkinDefinition();
								meter = null;
							}
							else
							{
								header = Header.Meter;
								//Save last meter
								if (meter != null && skinData != null)
								{
									skinData.meters.Add(meter);
								}
								//Begin new meter
								meter = new MeterDefinition();
							}
						}
						else
						{
							lines[i] += " ;! Can't have a header withouth a body.";
							NoErrors = false;
						}
						
					}
					else
					{
						header = Header.None;
					}
					continue; //No need to parse the line any further
				}

				//Don't parse outside of FFI blocks
				if (header == Header.None) continue;
				

				// • Remove old error messages
				int index = lines[i].IndexOf(";!");
				if (index != -1)
				{
					lines[i] = lines[i].Substring(0, index); //index+1?
					lines[i] = lines[i].TrimEnd(); //Remove excess spaces as well.
				}
				

				// • Trim eol comments
				index = trimmed.IndexOf(';');
				if (index != -1)
				{
					trimmed = trimmed.Substring(0, index);
				}

				// • Parse key value pairs
				var keyvalue = trimmed.Split(new char[] { '=' }, 2);
				if (keyvalue.Length != 2)
				{
					lines[i] += " ;! Couldn't split into 'key = value'.";
					NoErrors = false;
					continue;
				}
				keyvalue[0] = keyvalue[0].Trim().ToLower();
				keyvalue[1] = keyvalue[1].Trim();

				// • Handle keys and values
				int inum = 0;
				double dnum = 0;
				float fnum = 0;
				string[] parts;
				if(header == Header.FFI)
				{
					switch (keyvalue[0])
					{
						case "screen":
							if (int.TryParse(keyvalue[1], out inum))
							{
								inum -= 1;
								if(inum < 0)
								{
									lines[i] += " ;! Invalid screen number. Starts at 1.";
									NoErrors = false;
								}
								else if(inum >= surfaceCount)
								{
									lines[i] += $" ;! Invalid screen number. Only {surfaceCount} screens avalible.";
									NoErrors = false;
								}
								else
								{
									skinData.screenId = inum;
								}
							}
							else
							{
								lines[i] += " ;! Did not understand, it should be a nuber.";
								NoErrors = false;
							}
							break;
						case "background":
							//Check if first char in value string is a digit
							if(char.IsDigit(keyvalue[1][0]))
							{
								//Progrably an rgb color
								Color color;
								if(TryParseRGBColor(keyvalue[1], out color))
								{
									color.A = 255; //Prevent setting alpha for the background
									skinData.backgroundColor = color;
									skinData.backgroundColorSet = true;
								}
								else
								{
									lines[i] += " ;! Couldnt read RGB color. Needs to be 1-3 comma separated values, ranging from 0-255. E.g: 102, 51, 153 is purple. You can also use Hex.";
									NoErrors = false;
								}
							}
							else if(keyvalue[1][0] == '#')
							{
								//Progrably a hex color
								Color color;
								if (TryParseHexColor(keyvalue[1], out color))
								{
									color.A = 255; //Prevent setting alpha for the background
									skinData.backgroundColor = color;
									skinData.backgroundColorSet = true;
								}
								else
								{
									lines[i] += " ;! Couldn't read hex color. Needs to be 3 or 6 characters preceeded by #. E.g: #663399 is purple. ";
									NoErrors = false;
								}
							}
							else
							{
								//Probably a texture
								if (sprites.Contains(keyvalue[1]))
								{
									skinData.background = keyvalue[1];
								}
								else
								{
									lines[i] += " ;! No background with that name. Use an RGB color or one of these: ";
									for (int y = 0; y < sprites.Count; y++)
									{
										lines[i] += sprites[y] + ", ";
									}
									NoErrors = false;
								}
							}
							break;
						case "color":
							//Check if first char in value string is a digit
							if (char.IsDigit(keyvalue[1][0]))
							{
								//Progrably an rgb color
								Color color;
								if (TryParseRGBColor(keyvalue[1], out color))
								{
									skinData.color = color;
									skinData.colorSet = true;
								}
								else
								{
									lines[i] += " ;! Couldnt read RGB[A] color. Needs to be 1-3 comma separated values, ranging from 0-255. E.g: 102, 51, 153 is purple. You can also use Hex.";
									NoErrors = false;
								}
							}
							else if (keyvalue[1][0] == '#')
							{
								//Progrably a hex color
								Color color;
								if (TryParseHexColor(keyvalue[1], out color))
								{
									skinData.color = color;
									skinData.colorSet = true;
								}
								else
								{
									lines[i] += " ;! Couldn't read hex color. Needs to be 3, 4, 6, or 8 characters preceeded by #. E.g: #663399 is purple. ";
									NoErrors = false;
								}
							}
							else
							{
								lines[i] += " ;! Couldn't read color. Use RGB or hex. E.g: 102, 51, 153  or #663399 is purple.";
								NoErrors = false;
							}
							break;

						case "inject":
						case "preload":
							if (preloads.ContainsKey(keyvalue[1].ToLower()))
							{
								inject = true;
								injectKey = keyvalue[1].ToLower();
								lines[i] = ";" + lines[i];
							}
							else
							{
								lines[i] += " ;! No preload with that name. Avalible are:";
								foreach (var key in preloads.Keys)
								{
									lines[i] += " " + key + ",";
								}
								lines[i].TrimEnd(',');
								lines[i] += ".";
								NoErrors = false;
							}
							break;

						default:
							lines[i] += " ;! Unknown key <" + keyvalue[0] + ">";
							NoErrors = false;
							continue;
					}
				}
				else if(header == Header.Meter)
				{
					switch (keyvalue[0])
					{
						case "type":
							switch (keyvalue[1].ToLower())
							{
								case "text":
									meter.type = Meter.Text;
									break;

								case "value":
									meter.type = Meter.Value;
									break;

								case "bar":
									meter.type = Meter.Bar;
									break;

								case "meter":
									meter.type = Meter.Meter;
									break;

								case "half meter":
									meter.type = Meter.HalfMeter;
									break;

								case "quarter meter":
								case "quater meter":
									meter.type = Meter.ThreeQuaterMeter;
									break;

								case "line graph":
									meter.type = Meter.LineGraph;
									break;

								case "sprite":
									meter.type = Meter.Sprite;
									break;

								case "action":
									meter.type = Meter.Action;
									break;
									
								default:
									lines[i] += " ;! Didn't understand type. Avalible types: text, value, bar, meter, half meter, quarter meter, sprite, and action.";
									NoErrors = false;
									break;
							}

							break;

						case "data":
							if(meter.type == Meter.None)
							{
								lines[i] += " ;! Can't proceed. You need to set type before data.";
								NoErrors = false;
							}
							else
							{
								//TODO: Figure out what data we're looking for
								switch (keyvalue[1].ToLower())
								{
									case "pulse":
										meter.data = Data.Pulse;
										break;
									case "speed":
										meter.data = Data.Speed;
										break;
									case "vertical speed":
										meter.data = Data.VerticalSpeed;
										break;
									case "altitude":
									case "altitude land":
										meter.data = Data.Altitude;
										break;
									case "altitude sea":
										meter.data = Data.Altitude;
										break;
									case "mass":
										meter.data = Data.Mass;
										break;
									case "cargo mass":
										meter.data = Data.MassCargo;
										break;
									case "gravity":
										meter.data = Data.Gravity;
										break;
									case "dampeners":
										meter.data = Data.Dampeners;
										break;
									case "hydrogen":
										meter.data = Data.Hydrogen;
										break;
									case "oxygen":
										meter.data = Data.Oxygen;
										break;
									case "battery charge":
									case "battery level":
										meter.data = Data.BatteryCharge;
										break;
									case "battery usage":
										meter.data = Data.BatteryUsage;
										break;
									case "jumpdrive charge":
									case "jumpdrive level":
										meter.data = Data.JumpDriveCharge;
										break;
									case "solar usage":
										meter.data = Data.SolarUsage;
										break;
									case "reactor usage":
										meter.data = Data.ReactorUsage;
										break;
									case "hydrogen time":
										meter.data = Data.HydrogenTime;
										break;
									case "hydrogen engine usage":
										meter.data = Data.EngineUsage;
										break;
									case "power usage":
										meter.data = Data.PowerUsage;
										break;
									case "wind usage":
										meter.data = Data.WindUsage;
										break;
									case "jumpdrive distance":
										meter.data = Data.JumpDriveDistance;
										break;
									case "lift":
										meter.data = Data.Lift;
										break;
									case "landing gears":
										meter.data = Data.LandingGears;
										break;
									case "connectors":
										meter.data = Data.Connectors;
										break;
									case "seated":
										meter.data = Data.Seated;
										break;
									case "in main seat":
										meter.data = Data.InMainSeat;
										break;
									case "ruler":
										meter.data = Data.Ruler;
										break;
									case "stopping distance":
										meter.data = Data.StoppingDistance;
										break;
									case "handbrake":
										meter.data = Data.Handbrake;
										break;


									default:
										lines[i] += " ;! Didn't understand data. Avalible: pulse, speed, vertical speed, altitude, altitude sea, mass, cargo mass, gravity, lift, dampeners, oxygen, hydrogen, hydrogen time, jumpdrive distance, jumpdrive charge, battery charge, battery usage, solar usage, reactor usage, engine usage, wind usage, power usage, landing gears, connectors, seated, in main seat, ruler, stopping distance.";
										NoErrors = false;
										break;
								}
							}
							break;

						case "text":
							if (meter.type == Meter.Text)
							{
								meter.textData = keyvalue[1];
							}
							break;

						case "sprite":
							if (meter.type == Meter.Sprite)
							{
								if (sprites.Contains(keyvalue[1]))
								{
									meter.textData = keyvalue[1];
								}
								else
								{
									if (keyvalue[1].ToLower() == "square")
									{
										//Special case becasue SquareSimple is dumb.
										meter.textData = "SquareSimple";
									}
									else
									{
										lines[i] += " ;! No sprite with that name. Use one of these: Square, ";
										for (int y = 0; y < sprites.Count; y++)
										{
											lines[i] += sprites[y] + ", ";
										}
										NoErrors = false;
									}
								}
							}
							break;

						case "action":
							if (meter.type == Meter.Action)
							{
								meter.textData = keyvalue[1];
							}
							break;

						case "position":
							parts = keyvalue[1].Split(',');
							
							if(parts.Length == 2)
							{
								meter.posType.X = ParseVectorType(parts[0]);
								meter.posType.Y = ParseVectorType(parts[1]);
								parts[0] = parts[0].Trim('%', 'w', 'h');
								parts[1] = parts[1].Trim('%', 'w', 'h');

								if (float.TryParse(parts[0], out fnum))
								{
									Vector2 vec = Vector2.Zero;
									vec.X = fnum;
									if (float.TryParse(parts[1], out fnum))
									{
										vec.Y = fnum;
										meter.position = vec;
									}
									else
									{
										lines[i] += " ;! Didn't understand value, make sure it's a number. Use %, w, or h to make your value a percentage of the screen's minimum dimention, width or height.";
										NoErrors = false;
									}
								}
								else
								{
									lines[i] += " ;! Didn't understand value, make sure it's a number. Use %, w, or h to make your value a percentage of the screen's minimum dimention, width or height.";
									NoErrors = false;
								}
							}
							else
							{
								lines[i] += " ;! Wrong amount of values. Should be 2 (x and y), separated by a comma.";
								NoErrors = false;
							}
							break;

						case "size":
							parts = keyvalue[1].Split(',');
							if (parts.Length == 2)
							{
								meter.sizeType.X = ParseVectorType(parts[0]);
								meter.sizeType.Y = ParseVectorType(parts[1]);
								parts[0] = parts[0].Trim('%', 'w', 'h');
								parts[1] = parts[1].Trim('%', 'w', 'h');

								if (float.TryParse(parts[0], out fnum))
								{
									meter.size.X = fnum;
									if (float.TryParse(parts[1], out fnum))
									{
										if (meter.type == Meter.Meter || meter.type == Meter.HalfMeter || meter.type == Meter.ThreeQuaterMeter)
										{
											meter.size.Y = meter.size.X;
											meter.sizeType.Y = meter.sizeType.X;

											if (fnum > 1 || fnum < 0)
											{
												lines[i] += " ;! Specal case, here the second value is the line thickness and has to be between 0 and 1. Using default 0.3.";
												NoErrors = false;
												meter.stroke = 0.3f;
											}
											else
											{
												meter.stroke = fnum;
											}
										}
										else
										{
											meter.size.Y = fnum;
										}
									}
									else
									{
										lines[i] += " ;! Didn't understand value, make sure it's a number. Use %, w, or h to make your value a percentage of the screen's minimum dimention, width or height.";
										NoErrors = false;
									}
								}
								else
								{
									lines[i] += " ;! Didn't understand value, make sure it's a number. Use %, w, or h to make your value a percentage of the screen's minimum dimention, width or height.";
									NoErrors = false;
								}
							}
							else if (parts.Length == 1)
							{

								meter.sizeType.X = ParseVectorType(parts[0]);
								meter.sizeType.Y = meter.sizeType.X;
								parts[0] = parts[0].Trim('%', 'w', 'h');
								
								if (float.TryParse(parts[0], out fnum))
								{
									meter.size.X = fnum;
									meter.size.Y = fnum;
								}
								else
								{
									lines[i] += " ;! Didn't understand value, make sure it's a number.";
									NoErrors = false;
								}
							}
							else
							{
								lines[i] += " ;! Wrong amount of values. Should be 1 for text and 1 or 2 for everything else (x and y, separated by a comma).";
								NoErrors = false;
							}
							break;

						case "min":
							if (double.TryParse(keyvalue[1], out dnum))
							{
								meter.min = dnum;
							}
							else
							{
								lines[i] += " ;! Couldn't parse number.";
								NoErrors = false;
							}
							break;

						case "max":
							if (double.TryParse(keyvalue[1], out dnum))
							{
								meter.max = dnum;
							}
							else
							{
								lines[i] += " ;! Couldn't parse number.";
								NoErrors = false;
							}
							break;

						case "unit":
							parts = keyvalue[1].Split(',');
							for (int j = 0; j < parts.Length; j++)
							{
								parts[j] = parts[j].Trim();
								if (parts[j].Length > 1)
								{

									switch (parts[j].ToLower())
									{
										case "show":
											meter.showUnit = true;
											break;
										case "default":
											meter.unit = Units.Default;
											break;
										case "auto":
											meter.unit = Units.Auto;
											break;
										default:
											lines[i] += " ;! Input '" + parts[j] + "' wasn't understood. To change the value, use: auto, %, k, M, G, or T. To show the unit (kg, W, m/s etc..) add 'show'. E.g. 'show, %' will show the value as percent with a %-sign appended.";
											NoErrors = false;
											break;
									}
								}
								else if (parts[j].Length == 1)
								{
									switch (parts[j])
									{
										case "":
											meter.unit = Units.Auto;
											break;
										case "%":
											meter.unit = Units.Percent;
											break;
										case "k":
										case "K":
											meter.unit = Units.Kilo;
											break;
										case "M":
											meter.unit = Units.Mega;
											break;
										case "G":
											meter.unit = Units.Giga;
											break;
										case "T":
											meter.unit = Units.Tera;
											break;
										default:
											lines[i] += " ;! Input '" + parts[j] + "' wasn't understood. To change the value, use: auto, %, k, M, G, or T. To show the unit (kg, W, m/s etc..) add 'show'. E.g. 'show, %' will show the value as percent wiht a % sign-appended.";
											NoErrors = false;
											break;
									}
								}
								else
								{
									lines[i] += " ;! Value empty? To change the value, use: auto, %, k, M, G, or T. To show the unit (kg, W, m/s etc..) add 'show'. E.g. 'show, %' will show the value as percent wiht a % sign-appended.";
									NoErrors = false;
								}
							}

							
							break;

						case "decimals":
							if(int.TryParse(keyvalue[1], out inum))
							{
								if(inum == 0)
								{
									meter.decimalFormat = "{0:" + keyvalue[1] + "}";
								}
								else if(inum > 0 && inum < 10)
								{
									//Decimals
									meter.decimalFormat = "{0:0." + new string('0', Math.Abs(inum)) + "}";
								}
								else if (inum < 0 && inum > -10)
								{
									//Digits
									meter.decimalFormat = "{0:" + new string('0', Math.Abs(inum)) + "}";
								}
								else
								{
									lines[i] += " ;! That's just ridiculous.. ;)";
									NoErrors = false;
								}
							}
							else
							{
								if (CheckNumberFormat(keyvalue[1]))
								{
									meter.decimalFormat = "{0:" + keyvalue[1] + "}";
								}
								else
								{
									lines[i] += " ;! Invalid number format. Use whole number (positive for decimals, negative for digits) or use advanced number formats: Use 0 for fixed digits/decimals, and # for optional. E.g: value: 13.37, format: 0.#, result: 13.8. Or format: 000.000, reuslt: 013.370.";
									NoErrors = false;
								}
							}
							break;

						case "rotation":
							if (float.TryParse(keyvalue[1], out fnum))
							{
								meter.rotation = MathHelper.ToRadians(fnum);
							}
							else
							{
								lines[i] += " ;! Didn't understand value, make sure it's a number.";
								NoErrors = false;
							}
							break;

						case "color":
							//Check if first char in value string is a digit
							if (char.IsDigit(keyvalue[1][0]))
							{
								//Progrably an rgb color
								Color color;
								if (TryParseRGBColor(keyvalue[1], out color))
								{
									meter.color = color;
									meter.colorSet = true;
								}
								else
								{
									lines[i] += " ;! Couldnt read RGB[A] color. Needs to be 1-3 comma separated values, ranging from 0-255. E.g: 102, 51, 153 is purple. You can also use Hex.";
									NoErrors = false;
								}
							}
							else if (keyvalue[1][0] == '#')
							{
								//Progrably a hex color
								Color color;
								if (TryParseHexColor(keyvalue[1], out color))
								{
									meter.color = color;
									meter.colorSet = true;
								}
								else
								{
									lines[i] += " ;! Couldn't read hex color. Needs to be 3, 4, 6, or 8 characters preceeded by #. E.g: #663399 is purple. ";
									NoErrors = false;
								}
							}
							else
							{
								lines[i] += " ;! Couldn't read color. Use RGB or hex. E.g: 102, 51, 153  or #663399 is purple.";
								NoErrors = false;
							}
							break;

						case "background":
							//Check if first char in value string is a digit
							if (char.IsDigit(keyvalue[1][0]))
							{
								//Progrably an rgb color
								Color color;
								if (TryParseRGBColor(keyvalue[1], out color))
								{
									meter.background = color;
									meter.backgroundSet = true;
								}
								else
								{
									lines[i] += " ;! Couldnt read RGB[A] color. Needs to be 1-3 comma separated values, ranging from 0-255. E.g: 102, 51, 153 is purple. You can also use Hex.";
									NoErrors = false;
								}
							}
							else if (keyvalue[1][0] == '#')
							{
								//Progrably a hex color
								Color color;
								if (TryParseHexColor(keyvalue[1], out color))
								{
									meter.background = color;
									meter.backgroundSet = true;
								}
								else
								{
									lines[i] += " ;! Couldn't read hex color. Needs to be 3, 4, 6, or 8 characters preceeded by #. E.g: #663399 is purple. ";
									NoErrors = false;
								}
							}
							else
							{
								lines[i] += " ;! Couldn't read color. Use RGB or hex. E.g: 102, 51, 153  or #663399 is purple.";
								NoErrors = false;
							}
							break;

						case "anchor":
							switch (keyvalue[1].ToLower())
							{
								case "center":
									meter.anchor = Anchor.Center;
									break;

								case "left":
									meter.anchor = Anchor.Left;
									break;

								case "right":
									meter.anchor = Anchor.Right;
									break;

								default:
									lines[i] += " ;! Unknown anchor point. Use left, center, or right.";
									NoErrors = false;
									break;
							}
							break;

						case "hide":
						case "condition":
							parts = keyvalue[1].Split(',');
							if(parts.Length > 0)
							{
								var chars = "><=!".ToCharArray();
								for (int j = 0; j < parts.Length; j++)
								{
									parts[j] = parts[j].Trim();
									int end = parts[j].LastIndexOfAny(chars);
									
									if(parts[j].Length > end + 1)
									{
										//Prevent out of range exception
										if (double.TryParse(parts[j].Substring(end + 1).Trim(), out dnum))
										{
											if (end == 0)
											{
												switch (parts[j][0])
												{
													case '>':
														meter.conditions.Add(Condition.Greater);
														meter.condVals.Add(dnum);
														break;
													case '<':
														meter.conditions.Add(Condition.Less);
														meter.condVals.Add(dnum);
														break;
													case '=':
														meter.conditions.Add(Condition.Equal);
														meter.condVals.Add(dnum);
														break;
													default:
														lines[i] += " ;! Can't read condtion. Use one of these operators and a number: (> greater), (>= greater or equal), (< less), (<= less or equal), (== equal), (!= not equal). Writing '>= 50' means it will only be drawn if the value is greater than or equal to 50";
														NoErrors = false;
														break;
												}
											}
											else if(end == 1)
											{
												switch(parts[j].Substring(0, 2))
												{
													case ">=":
														meter.conditions.Add(Condition.GreaterEqual);
														meter.condVals.Add(dnum);
														break;
													case "<=":
														meter.conditions.Add(Condition.LessEqual);
														meter.condVals.Add(dnum);
														break;
													case "==":
														meter.conditions.Add(Condition.Equal);
														meter.condVals.Add(dnum);
														break;
													case "!=":
														meter.conditions.Add(Condition.Not);
														meter.condVals.Add(dnum);
														break;
													default:
														lines[i] += " ;! Can't read condtion. Use one of these operators and a number: (> greater), (>= greater or equal), (< less), (<= less or equal), (== equal), (!= not equal). Writing '>= 50' means it will only be drawn if the value is greater than or equal to 50";
														NoErrors = false;
														break;
												}
											}
											else
											{
												lines[i] += " ;! Invalid condition. Use one of these operators and a number: (> greater), (>= greater or equal), (< less), (<= less or equal), (== equal), (!= not equal). Writing '>= 50' means it will only be drawn if the value is greater than or equal to 50";
												NoErrors = false;
											}
										}
										else
										{
											lines[i] += " ;! Can't read this. Use one of these operators and a number: (> greater), (>= greater or equal), (< less), (<= less or equal), (== equal), (!= not equal). Writing '>= 50' means it will only be drawn if the value is greater than or equal to 50";
											NoErrors = false;
										}
									}
									else
									{
										lines[i] += " ;! Can't read this. Use one of these operators and a number: (> greater), (>= greater or equal), (< less), (<= less or equal), (== equal), (!= not equal). Writing '>= 50' means it will only be drawn if the value is greater than or equal to 50";
										NoErrors = false;
									}
								}
							}
							else
							{
								lines[i] += " ;! Can't read this. Use one of these operators and a number: (> greater), (>= greater or equal), (< less), (<= less or equal), (== equal), (!= not equal). Writing '>= 50' means it will only be drawn if the value is greater than or equal to 50";
								NoErrors = false;
							}
							break;

						case "block":
							parts = keyvalue[1].Split(',');
							if (parts.Length > 0)
							{
								for (int j = 0; j < parts.Length; j++)
								{
									parts[j] = parts[j].Trim().Trim('"');
									meter.blocks.Add(parts[j]);
								}
							}
							else
							{
								lines[i] += " ;! No block? Use block name, and/or *group name* in asterics. Separete more than one with comma.";
								NoErrors = false;
							}
							break;


						default:
							lines[i] += " ;! Unknown key";
							NoErrors = false;
							continue;
					}
				}

				//inject preload if it's the last line and havn't injected yet.
				if (i == lines.Count - 1 && inject)
				{
					inject = false;
					Inject(i+1, injectKey);
					header = Header.None;
				}
			}
			

			if (meter != null && skinData != null)
			{
				skinData.meters.Add(meter);
			}
			if (skinData != null)
			{
				SkinDataList.Add(skinData);
			}

			Processed = string.Join("\n", lines);
			finnishedIni = true;
			yield return true;
		}

		void Inject(int lineIndex, string preload)
		{
			//Inject before this line
			lines.InsertRange(lineIndex, preloads[preload].Split('\n'));
		}


		/// <summary>
		/// Parses a color string separated by comma. 
		/// Acceptable are:
		/// 128 - Grayscale
		/// 128, 128 - Grayscale with alpha
		/// 128, 128, 128 - RGB color
		/// 128, 128, 128, 128 - RGBA color
		/// </summary>
		/// <param name="text">Comma separated RGB value</param>
		/// <param name="color">Return color</param>
		/// <returns></returns>
		static bool TryParseRGBColor(string text, out Color color)
		{
			color = Color.White;
			var parts = text.Split(',');
			byte val = 0;
			if (parts.Length == 1)
			{
				//Grayscale
				if (byte.TryParse(parts[0].Trim(), out val))
				{
					color.R = val;
					color.G = val;
					color.B = val;
				}
				else return false;
			}
			else if (parts.Length == 2)
			{
				//Grayscale with alpha
				if (byte.TryParse(parts[0].Trim(), out val))
				{
					color.R = val;
					color.G = val;
					color.B = val;
				}
				else return false;
				if (byte.TryParse(parts[1].Trim(), out val))
				{
					color.A = val;
				}
				else return false;
			}
			else if (parts.Length == 3)
			{
				//RGB
				if (byte.TryParse(parts[0].Trim(), out val))
				{
					color.R = val;
				}
				else return false;
				if (byte.TryParse(parts[1].Trim(), out val))
				{
					color.G = val;
				}
				else return false;
				if (byte.TryParse(parts[2].Trim(), out val))
				{
					color.B = val;
				}
				else return false;
			}
			else if (parts.Length == 4)
			{
				//RGB
				if (byte.TryParse(parts[0].Trim(), out val))
				{
					color.R = val;
				}
				else return false;
				if (byte.TryParse(parts[1].Trim(), out val))
				{
					color.G = val;
				}
				else return false;
				if (byte.TryParse(parts[2].Trim(), out val))
				{
					color.B = val;
				}
				else return false;
				if (byte.TryParse(parts[3].Trim(), out val))
				{
					color.A = val;
				}
				else return false;
			}
			else return false;
			return true;
		}

		static bool TryParseHexColor(string text, out Color color)
		{
			color = Color.White;
			text = text.TrimStart('#').Trim();
			if (text.Length == 3)
			{
				color.R = (byte)(Convert.ToInt16(text.Substring(0, 1), 16) * 16);
				color.G = (byte)(Convert.ToInt16(text.Substring(1, 1), 16) * 16);
				color.B = (byte)(Convert.ToInt16(text.Substring(2, 1), 16) * 16);
			}
			else if (text.Length == 4)
			{
				color.R = (byte)(Convert.ToInt16(text.Substring(0, 1), 16) * 16);
				color.G = (byte)(Convert.ToInt16(text.Substring(1, 1), 16) * 16);
				color.B = (byte)(Convert.ToInt16(text.Substring(2, 1), 16) * 16);
				color.A = (byte)(Convert.ToInt16(text.Substring(3, 1), 16) * 16);
			}
			else if (text.Length == 6)
			{
				color.R = Convert.ToByte(text.Substring(0, 2), 16);
				color.G = Convert.ToByte(text.Substring(2, 2), 16);
				color.B = Convert.ToByte(text.Substring(4, 2), 16);
			}
			else if (text.Length == 8)
			{
				color.R = Convert.ToByte(text.Substring(0, 2), 16);
				color.G = Convert.ToByte(text.Substring(2, 2), 16);
				color.B = Convert.ToByte(text.Substring(4, 2), 16);
				color.A = Convert.ToByte(text.Substring(6, 2), 16);
			}
			else return false;
			return true;
		}

		static bool CheckNumberFormat(string input)
		{
			string whitelist = "0#,.";
			foreach (char c in input)
			{
				if (whitelist.IndexOf(c) == -1)
					return false;
			}
			return true;
		}

		static VectorType ParseVectorType(string input)
		{
			if (input.Contains('%'))
			{
				return VectorType.ViewMin;
			}
			else if (input.Contains('w'))
			{
				return VectorType.ViewWidth;
			}
			else if (input.Contains('h'))
			{
				return VectorType.ViewHeight;
			}
			return VectorType.Pixel;
		}

		//TODO: This is in a weird place.
		public static bool MeetsConditions(List<Condition> conditions, double value, List<double> conditionValues)
		{
			for (int i = 0; i < conditions.Count; i++)
			{
				if (TryCondition(conditions[i], value, conditionValues[i]))
				{
					return true;
				}
			}
			return false;
		}

		public static double AdjustToUnit(MeterDefinition def, Dictionary<Data, IData> shipData, double total, bool UseDataMinMax)
		{
			double val = shipData[def.data].Value;
			if (def.unit == Units.Percent)
			{
				if (UseDataMinMax)
				{
					total = shipData[def.data].Max - shipData[def.data].Min;
				}
				val = shipData[def.data].Value / total * 100;
			}
			else if (def.unit == Units.Default)
			{
				val = shipData[def.data].Value;
			}
			else
			{
				val = ConvertTo(def.unit, shipData[def.data].Value);
			}
			return val;
		}

		//TODO: This is in a weird place.
		/// <summary>
		/// Trys a condition defined as the enum Condition
		/// </summary>
		/// <param name="condition">Condition</param>
		/// <param name="left">Left value, e.g. this >= other</param>
		/// <param name="right">Right value, e.g. other >= this</param>
		/// <returns></returns>
		public static bool TryCondition(Condition condition, double left, double right)
		{
			switch (condition)
			{
				case Condition.Greater:
					return left > right;
				case Condition.Less:
					return left < right;
				case Condition.GreaterEqual:
					return left >= right;
				case Condition.LessEqual:
					return left <= right;
				case Condition.Equal:
					return left == right;
				case Condition.Not:
					return left != right;
			}
			return false;
		}

		
		public static double ConvertTo(Units unit, double value)
		{
			//NOTE! This can't handle percent.
			switch (unit)
			{
				case Units.Auto:
					double abs = Math.Abs(value);
					if (abs >= 1000000000000) return ConvertTo(Units.Tera, value);
					if (abs >= 1000000000) return ConvertTo(Units.Giga, value);
					if (abs >= 1000000) return ConvertTo(Units.Mega, value);
					if (abs >= 1000) return ConvertTo(Units.Kilo, value);
					return value;
				case Units.Default:
					return value;
				case Units.Kilo:
					return value * 0.001;
				case Units.Mega:
					return value * 0.000001;
				case Units.Giga:
					return value * 0.000000001;
				case Units.Tera:
					return value * 0.000000000001;
			}
			return value;
		}

	}

	enum Condition
	{
		None,
		Greater,
		Less,
		GreaterEqual,
		LessEqual,
		Equal,
		Not
	}

	enum Units
	{
		None,
		Default,
		Auto,
		Percent,
		Kilo,
		Mega,
		Giga,
		Tera
	}


	class SkinDefinition
	{
		public int screenId;
		public Color color;
		public bool colorSet;
		public string background;
		public Color backgroundColor;
		public bool backgroundColorSet;
		public List<MeterDefinition> meters = new List<MeterDefinition>();
	}

	struct Vector2Type
	{
		public VectorType X;
		public VectorType Y;
	}

	enum VectorType
	{
		Pixel,
		ViewWidth,
		ViewHeight,
		ViewMin
	}
	
	class MeterDefinition
	{
		public Meter type;
		public Data data;
		public string textData;
		public Vector2 position = Vector2.Zero;
		public Vector2Type posType;
		public Vector2 size = Vector2.Zero;
		public Vector2Type sizeType;
		public float stroke = 0.3f; //Created for use with meter only
		public Color color = Color.White;
		public bool colorSet = false;
		public Color background = Color.White;
		public bool backgroundSet = false;
		public double min;
		public double max;
		public Units unit = Units.Default;
		public bool showUnit;
		public string decimalFormat = "{0:0.##}";
		public Anchor anchor = Anchor.Center;
		public float rotation = 0;
		public List<Condition> conditions = new List<Condition>();
		public List<double> condVals = new List<double>();
		public List<string> blocks = new List<string>();
	}
	#endregion
}
