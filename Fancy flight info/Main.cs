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
namespace IngameScript
{
	public class Program : MyGridProgram
	{
		#endregion
		#region in-game


		bool debug = false;

		public TimeSpan Time { get; private set; }

		IEnumerator<bool> InitStateMachine;
		IEnumerator<bool> DrawStateMachine;
		bool DoDraw = false;
		byte Initialized = 0;

		List<Skin> Skins = new List<Skin>();


		Dictionary<Data, IData> ShipData = new Dictionary<Data, IData>();
		
		long Count10 = 0;

		IMyShipController Controller = null;

		int MaxInstructionsPerTick = 0;

		Data DataChanged = 0;

		List<IMyTerminalBlock> blocks;

		List<string> InitProblemBlockNames = new List<string>();

		IMyTextSurface pbSurface;
		SurfaceMath pbSM;

		Dictionary<string, string> Preloads;
		
		Profiler profiler;


		public Program()
		{
			pbSurface = Me.GetSurface(0);
			pbSM = new SurfaceMath(pbSurface);
			pbSurface.ContentType = ContentType.SCRIPT;
			pbSurface.Script = "";
			if (pbSurface.ScriptBackgroundColor != problemBG) defaultBG = pbSurface.ScriptBackgroundColor;

			InitPreloads();

			MaxInstructionsPerTick = Runtime.MaxInstructionCount / 4; //Limit how much the script can do on a single tick
			Runtime.UpdateFrequency = UpdateFrequency.Update10 | UpdateFrequency.Update1;
			InitStateMachine = Init();
		}

		public void Main(string argument, UpdateType updateType)
		{
			Time = Time + Runtime.TimeSinceLastRun;

			if (argument == "update")
			{
				Initialized = 0;
				InitStateMachine = Init();
			}
			else if (argument == "profiler")
			{
				profiler = new Profiler(Me, 600, 10);
			}

			if ((updateType & UpdateType.Update1) != 0)
			{
				PrintDetailedInfo(); //TODO: Move this back to Update10 once the flickering echo bug has been fixed.
			}
			if ((updateType & UpdateType.Update10) != 0)
			{
				Count10++;
				if(!debug) DrawOutsidePBLcd();
			}


			if (Initialized == 0)
			{
				if (InitStateMachine != null)
				{
					if (!InitStateMachine.MoveNext() || !InitStateMachine.Current)
					{
						InitStateMachine.Dispose();
						InitStateMachine = null;
					}
				}
			}
			else
			{
				if ((updateType & UpdateType.Update10) != 0)
				{
					DataChanged = 0;
					foreach (var data in ShipData)
					{
						if (data.Value.Update())
						{
							DataChanged |= data.Key;
						}
					}
					DoDraw = true;

					if (Initialized == 1)
					{
						//Make all show as changed if it's the first run.
						DataChanged = 0;
						DataChanged = ~DataChanged;
						Initialized++;
					}
				}
				if ((updateType & UpdateType.Update1) != 0)
				{
					if (DrawStateMachine != null)
					{
						if (!DrawStateMachine.MoveNext() || !DrawStateMachine.Current)
						{
							DrawStateMachine.Dispose();
							DrawStateMachine = null;
						}
					}
				}
			}

			if (profiler != null)
			{
				Echo("Profiler: " + profiler.Update(Runtime.LastRunTimeMs).ToString("P") + "\nAvr: " + profiler.Avrage.ToString("n4") + "ms, Peak: " + profiler.Peak.ToString("n4") + "ms");
			}
		}


		private IEnumerator<bool> Draw()
		{
			while (true)
			{
				if (DoDraw)
				{
					foreach (var skin in Skins)
					{
						if (OverInstructionLimit()) yield return true;
						skin.Draw(DataChanged);
					}
					DoDraw = false;
				}
				yield return true;
			}
		}


		private IEnumerator<bool> Init()
		{
			
			Skins.Clear();
			ShipData.Clear();
			InitProblemBlockNames.Clear();

			blocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType(blocks, x => x.IsSameConstructAs(Me));
			

			var sprites = new List<string>();
			
			foreach (var block in blocks)
			{
				if (block is IMyShipController)
				{
					var contr = block as IMyShipController;
					if (contr.CanControlShip)
					{
						if (Controller == null)
						{
							//Take any controller just to have one.
							Controller = contr;
						}
						if (contr.IsMainCockpit)
						{
							//If a main one is defined, use that.
							Controller = contr;
						}
					}
				}

				var surfaceProvider = block as IMyTextSurfaceProvider;
				if (surfaceProvider != null && surfaceProvider.SurfaceCount > 0)
				{
					//Get all textures, might be needed in the ini parser
					if (sprites.Count == 0 )
					{
						surfaceProvider.GetSurface(0).GetSprites(sprites);
					}
					
					//Has Surface, check custom data

					Ini ini = new Ini(block.CustomData, surfaceProvider.SurfaceCount, sprites, Preloads);
					//Skins will be added to the Skins list in ParseIni
					
					while (true)
					{
						if(ini.ParserDone() == true)
						{
							break;
						}
						else
						{
							if (OverInstructionLimit()) yield return true;
						}
					}

					if (!ini.NoErrors)
					{
						//TODO
						InitProblemBlockNames.Add(block.CustomName);
					}
					block.CustomData = ini.Processed;
					
					foreach (var skinDef in ini.SkinDataList)
					{
						if (skinDef.screenId < surfaceProvider.SurfaceCount)
						{
							var surface = surfaceProvider.GetSurface(skinDef.screenId);
							var sm = new SurfaceMath(surface);
							var meters = new List<IMeter>();
							foreach (var meter in skinDef.meters)
							{
								if (meter.type == Meter.Text)
								{
									RegisterDataPoint(meter.data);
									meters.Add(new MeterText(sm, meter, ShipData));
								}
								else if (meter.type == Meter.Value)
								{
									RegisterDataPoint(meter.data);
									meters.Add(new MeterValue(sm, meter, ShipData));
								}
								else if (meter.type == Meter.Bar)
								{
									RegisterDataPoint(meter.data);
									meters.Add(new MeterBar(sm, meter, ShipData));
								}
								else if (meter.type == Meter.LineGraph)
								{
									RegisterDataPoint(meter.data);
									meters.Add(new MeterLineGraph(sm, meter, ShipData));
								}
								else if (meter.type == Meter.Meter)
								{
									RegisterDataPoint(meter.data);
									meters.Add(new MeterMeter(sm, meter, ShipData, (skinDef.backgroundColorSet ? skinDef.backgroundColor : surface.ScriptBackgroundColor)));
								}
								else if (meter.type == Meter.HalfMeter)
								{
									RegisterDataPoint(meter.data);
									meters.Add(new MeterHalfMeter(sm, meter, ShipData, (skinDef.backgroundColorSet ? skinDef.backgroundColor : surface.ScriptBackgroundColor)));
								}
								else if (meter.type == Meter.ThreeQuaterMeter)
								{
									RegisterDataPoint(meter.data);
									meters.Add(new MeterThreeQuaterMeter(sm, meter, ShipData, (skinDef.backgroundColorSet ? skinDef.backgroundColor : surface.ScriptBackgroundColor)));	
								}
								else if (meter.type == Meter.Sprite)
								{
									RegisterDataPoint(meter.data);
									meters.Add(new MeterSprite(sm, meter, ShipData));
								}
								else if (meter.type == Meter.Action)
								{
									RegisterDataPoint(meter.data);
									meters.Add(new MeterAction(sm, meter, ShipData, blocks, GridTerminalSystem, Me));
								}

							}
							Skins.Add(new Skin(this, surface, sm, block, skinDef, meters));
						}
					}

					/* Adding all isn't what we want.
					for (int i = 0; i < surfaceProvider.SurfaceCount; i++)
					{
						Skins.Add(new Skin(this, surfaceProvider.GetSurface(i), i, block));
					}
					*/
				}
			}

			DrawStateMachine = Draw();
			Initialized++;
			yield return false;
		}

		void RegisterDataPoint(Data data)
		{
			if (!ShipData.ContainsKey(data))
			{
				switch (data)
				{
					case Data.None:
						ShipData.Add(data, new DataNone());
						break;
					case Data.Pulse:
						ShipData.Add(data, new DataPulse());
						break;
					case Data.Speed:
						ShipData.Add(data, new DataSpeed(Controller));
						break;
					case Data.VerticalSpeed:
						ShipData.Add(data, new DataVerticalSpeed(Controller));
						break;
					case Data.Altitude:
						ShipData.Add(data, new DataAltitude(Controller));
						break;
					case Data.AltitudeSea:
						ShipData.Add(data, new DataAltitudeSea(Controller));
						break;
					case Data.Mass:
						ShipData.Add(data, new DataMass(Controller));
						break;
					case Data.MassCargo:
						ShipData.Add(data, new DataMassCargo(Controller));
						break;
					case Data.Gravity:
						ShipData.Add(data, new DataGravity(Controller));
						break;
					case Data.Dampeners:
						ShipData.Add(data, new DataDampeners(Controller));
						break;
					case Data.Hydrogen:
						ShipData.Add(data, new DataHydrogen(blocks));
						break;
					case Data.Oxygen:
						ShipData.Add(data, new DataOxygen(blocks));
						break;
					case Data.BatteryCharge:
						ShipData.Add(data, new DataBatteryCharge(blocks));
						break;
					case Data.BatteryUsage:
						ShipData.Add(data, new DataPowerUsage(blocks, "MyObjectBuilder_BatteryBlock"));
						break;
					case Data.JumpDriveCharge:
						ShipData.Add(data, new DataJumpDriveCharge(blocks));
						break;
					case Data.SolarUsage:
						ShipData.Add(data, new DataPowerUsage(blocks, "MyObjectBuilder_SolarPanel"));
						break;
					case Data.ReactorUsage:
						ShipData.Add(data, new DataPowerUsage(blocks, "MyObjectBuilder_Reactor"));
						break;
					case Data.HydrogenTime:
						RegisterDataPoint(Data.Hydrogen);
						ShipData.Add(data, new DataHydrogenTime(ShipData, this));
						break;
					case Data.EngineUsage:
						ShipData.Add(data, new DataPowerUsage(blocks, "MyObjectBuilder_HydrogenEngine"));
						break;
					case Data.PowerUsage:
						ShipData.Add(data, new DataPowerUsage(blocks));
						break;
					case Data.WindUsage:
						ShipData.Add(data, new DataPowerUsage(blocks, "MyObjectBuilder_WindTurbine"));
						break;
					case Data.JumpDriveDistance:
						RegisterDataPoint(Data.Mass);
						ShipData.Add(data, new DataJumpDriveDistance(blocks, ShipData));
						break;
					case Data.Lift:
						RegisterDataPoint(Data.Mass);
						ShipData.Add(data, new DataLift(Controller, blocks, ShipData));
						break;
					case Data.LandingGears:
						ShipData.Add(data, new DataLandingGear(blocks));
						break;
					case Data.Connectors:
						ShipData.Add(data, new DataConnectors(blocks));
						break;
					case Data.Seated:
						ShipData.Add(data, new DataSeated(blocks));
						break;
					case Data.InMainSeat:
						ShipData.Add(data, new DataInSeat(Controller));
						break;
					case Data.Ruler:
						ShipData.Add(data, new DataRuler(blocks));
						break;
					case Data.StoppingDistance:
						RegisterDataPoint(Data.Mass);
						RegisterDataPoint(Data.Speed);
						ShipData.Add(data, new DataStoppingDistance(Controller, blocks, ShipData));
						break;
					case Data.Handbrake:
						ShipData.Add(data, new DataHandbrake(Controller));
						break;
				}
			}
		}

		public bool OverInstructionLimit()
		{
			return Runtime.CurrentInstructionCount > MaxInstructionsPerTick;
		}


		FixedWidthText detailedInfo = new FixedWidthText(40);
		void PrintDetailedInfo()
		{

			detailedInfo.Clear();
			detailedInfo.AppendLine("Blarg's Fancy Flight Info");


			if (Initialized < 2)
			{
				detailedInfo.AppendLine("Initializing...");
				detailedInfo.Append(new string('.', (int)(Count10 % 16) / 4));
				detailedInfo.AppendLine("Please wait.");
			}
			else
			{
				detailedInfo.AppendLine("Running");
				detailedInfo.Append(new string('.', (int)(Count10 % 16) / 4));
				detailedInfo.AppendLine();

				if (InitProblemBlockNames.Count > 0)
				{
					detailedInfo.AppendLine("Problem(s) in Custom Data of:");
					for (int i = 0; i < InitProblemBlockNames.Count; i++)
					{
						detailedInfo.AppendLine("• " + InitProblemBlockNames[i]);
					}
				}
				else
				{
					detailedInfo.AppendLine("No problems detected.");
				}
			}
			
			Echo(detailedInfo.ToString());
		}

		Color pbSquare = new Color(Color.White, 0.1f);
		Color defaultBG = new Color(0, 88, 151);
		Color problemBG = new Color(151, 88, 0);
		float[] heartBeat = {1, 1.3f, 1.5f, 1.3f, 1, 1.3f, 1.5f, 1.3f, 1, 1, 1, 1};
		int heartBeatIndex = 0;
		void DrawOutsidePBLcd()
		{
			using (var frame = pbSurface.DrawFrame())
			{
				Vector2 headerPos = new Vector2(pbSM.Center.X, pbSM.Center.Y - pbSM.Size.Y * 0.333f);

				MySprite sprite;

				sprite = new MySprite(SpriteType.TEXTURE, "SquareSimple", color: pbSquare); //This cannot be reused?
				sprite.Size = new Vector2(pbSM.Size.X, pbSM.Size.Y * 0.333f);
				sprite.Position = headerPos;
				frame.Add(sprite);

				sprite = new MySprite(SpriteType.TEXTURE, "Grid", color: pbSurface.ScriptForegroundColor);
				sprite.Size = pbSM.BGSize * 2;
				frame.Add(sprite);



				string status = "";
				string problems = "";
				if (Initialized < 2)
				{
					status = "Initializing...\nPlease wait.";
				}
				else
				{
					if (InitProblemBlockNames.Count > 0)
					{
						status = "Problem(s) with:";
						for (int i = 0; i < InitProblemBlockNames.Count; i++)
						{
							problems += InitProblemBlockNames[i] + "\n";
						}
						pbSurface.ScriptBackgroundColor = problemBG;
					}
					else
					{
						status = "Running";
						pbSurface.ScriptBackgroundColor = defaultBG;

						heartBeatIndex++;
						sprite = new MySprite(SpriteType.TEXTURE, "SquareSimple", color: pbSquare);
						sprite.Size = Vector2.One * (pbSM.SmallestSize * 0.15f) * heartBeat[heartBeatIndex % heartBeat.Length];
						sprite.Position = new Vector2(pbSM.Center.X, pbSM.Center.Y + pbSM.Size.Y * 0.165f);
						frame.Add(sprite);
					}
				}

				//Add text
				MySprite textSprite; //This can be reused after running frame.Add
				textSprite = MySprite.CreateText("Blargmode's\nFancy Flight Info", "Debug", pbSurface.ScriptForegroundColor, 1f, TextAlignment.CENTER);
				textSprite.Position = new Vector2(headerPos.X, headerPos.Y - pbSM.TextHeight(1, 2) * 0.5f);
				frame.Add(textSprite);

				textSprite = MySprite.CreateText(status, "Debug", pbSurface.ScriptForegroundColor, 1f, TextAlignment.CENTER);
				textSprite.Position = new Vector2(headerPos.X, headerPos.Y + pbSM.Size.Y * 0.165f);
				frame.Add(textSprite);

				textSprite = MySprite.CreateText(problems, "Debug", pbSurface.ScriptForegroundColor, 0.7f, TextAlignment.CENTER);
				textSprite.Position = new Vector2(headerPos.X, headerPos.Y + pbSM.Size.Y * 0.165f + pbSM.TextHeight(1, 1));
				frame.Add(textSprite);
			}
		}

		void InitPreloads()
		{
			Preloads = new Dictionary<string, string>()
			{
				{ "speed", "\n[FFI_Speedometer_Meter]\ntype=quater meter\ndata=speed\nposition=0, 10%\nsize=50%, 0.1\nbackground=#3af\n\n[FFI_Speedometer_Value]\ntype=value\ndata=speed\nposition=0, 10h\nsize=25%\ndecimals=-2\n\n[FFI_Speedometer_Text]\ntype=text\ntext=m/s\nposition=0, -10h\ncolor=#66bfff\nsize=13%\n\n\n[FFI_VSpeed_ArrowUp]\ntype=sprite\nsprite=Triangle\nposition=-43w, 42h\nsize=6%\ncolor=#3af\n\n[FFI_VSpeed_ArrowDown]\ntype=sprite\nsprite=Triangle\nposition=-43w, -42h\nsize=6%\nrotation=180\ncolor=#3af\n\n[FFI_VSpeed_PositiveBar]\ntype=bar\ndata=vertical speed\nposition=-43w, 2.5h\nsize=39.1h, 2.5w\nanchor=left\nrotation=-90\nmin=0\nmax=100\nbackground=#3af\n\n[FFI_VSpeed_NegativeBar]\ntype=bar\ndata=vertical speed\nposition=-43w, -2.5h\nsize=39.1h, 2.5w\nanchor=left\nrotation=90\nmin=-100\nmax=0\nbackground=#3af\n\n[FFI_VSpeed_CenterCircle]\ntype=sprite\nsprite=Circle\nposition=-43w, 0\nsize=7%\ncolor=#3af\n\n[FFI_VSpeed_ValueBackground]\ntype=sprite\nsprite=Square\nposition=-25w, -35h\nsize=25w, 14h\ncolor=#3af\n\n[FFI_VSpeed_Value]\ntype=value\ndata=vertical speed\nposition=-25w, -35h\nsize=13%\ndecimals=-1\n\n[FFI_Altitude_ValueBackgrund]\ntype=sprite\nSprite=square\nposition=25w, -35h\nsize=25w, 14h\ncolor=#3af\n\n[FFI_Altitude_Value]\ntype=value\ndata=altitude\nposition=25w, -35h\nsize=13%\ndecimals=-1\n\n[FFI_Altitude_ArrowUp]\ntype=sprite\nsprite=Triangle\nposition=43w, 42h\nsize=6%\ncolor=#3af\n\n[FFI_Altitude_Bar2000]\ntype=bar\ndata=altitude\nposition=43w, -39h\nsize=79h, 2.5w\nanchor=left\nrotation=-90\nmax=2000\nhide=< 20\nbackground=#3af\n\n[FFI_Altitude_Bar20]\ntype=bar\ndata=altitude\nposition=43w, -39h\nsize=79h, 2.5w\nanchor=left\nrotation=-90\nmax=20\ncolor=#e33\nbackground=#3af\nhide=>= 20\n\n[FFI_Altitude_CenterCircle]\ntype=sprite\nsprite=Circle\nposition=43w, -42h\nsize=6%\ncolor=#3af\n" },
				{ "propulsion", "\n[FFI_Hydrogen_Background]\ntype=sprite\nsprite=square\nsize=45w, 45h\nposition=-24w, -24h\ncolor=#3af\n\n[FFI_Hydrogen_Background_red]\ntype=sprite\nsprite=square\nsize=45w, 45h\nposition=-24w, -24h\ncolor=#e33\ndata = hydrogen\nunit = %\nhide=> 25\n\n[FFI_Hydrogen_Icon]\ntype=sprite\nsprite=IconHydrogen\nsize=15%\nposition=-24w, -24h\ncolor=255, 150\n\n[FFI_Hydrogen_Graph]\ntype=line graph\ndata=hydrogen\nsize=45w, 45h\nposition=-24w, -24h\n\n\n[FFI_Power_Background]\ntype=sprite\nsprite=square\nsize=45w, 45h\nposition=-24w, 24h\ncolor=#3af\n\n[FFI_Power_Icon]\ntype=sprite\nsprite=IconEnergy\nsize=20%\nposition=-24w, 24h\ncolor=255, 150\n\n[FFI_Power_Graph]\ntype=line graph\ndata=power usage\nsize=45w, 45h\nposition=-24w, 24h\n\n[FFI_Power_Graph]\ntype=line graph\ndata=battery charge\nsize=45w, 45h\nposition=-24w, 24h\ncolor=255, 50\n\n\n[FFI_Lift_Outline]\ntype=sprite\nsprite=SquareHollow\nposition=33w, 39h\nsize=25w, 15h\n\n[FFI_Lift_Value_Ok]\ntype=value\ndata=lift\nsize=14%\nposition=33w, 39h\ncolor=#66ff66\nhide=<= 1\nunit=show\n\n[FFI_Lift_Value_Warning]\ntype=value\ndata=lift\nsize=14%\nposition=33w, 39h\ncolor=#ff6666\nhide=> 1\nunit=show\n\n[FFI_Lift_Text]\ntype=text\ntext=Lift\nsize=14%\nanchor=left\nposition=1w, 39h\ncolor=#66bfff\n\n\n[FFI_Grav_Outline]\ntype=sprite\nsprite=SquareHollow\nposition=33w, 20h\nsize=25w, 15h\n\n[FFI_Grav_Value]\ntype=value\ndata=gravity\nsize=14%\nposition=33w, 20h\nunit=show\n\n[FFI_Grav_Text]\ntype=text\ntext=Grav\nsize=14%\nanchor=left\nposition=1w, 20h\ncolor=#66bfff\n\n\n[FFI_HydrogenTime_Outline]\ntype=sprite\nsprite=SquareHollow\nposition=23.5w, -39h\nsize=44w, 15h\n\n[FFI_HydrogenTime_Value]\ntype=value\ndata=hydrogen time\nsize=14%\nposition=23.5w, -39h\nunit=auto, show\n\n[FFI_HydrogenTime_Text]\ntype=text\ntext=H2 Time\nsize=14%\nposition=23.5w, -25h\ncolor=#66bfff\n\n\n[FFI_CargoWeight_Outline]\ntype=sprite\nsprite=SquareHollow\nposition=23.5w, -10h\nsize=44w, 15h\n\n[FFI_CargoWeight_Value]\ntype=value\ndata=cargo mass\nsize=14%\nposition=23.5w, -10h\nunit=auto, show\n\n[FFI_CargoWeight_Text]\ntype=text\ntext=Cargo\nsize=14%\nposition=23.5w, 6h\ncolor=#66bfff\n"},
				{ "land and connect", "\n[FFI_LandingGears1]\ntype=sprite\nsprite=Square\nposition=-25w, 0\nsize=46w, 92h\ncolor=#3af\n\n[FFI_LandingGears2]\ntype=sprite\nsprite=Square\nposition=25w, 0\nsize=46w, 92h\ncolor=#3af\n\n[FFI_LandingGears3]\ntype=sprite\nsprite=SemiCircle\nrotation=180\nposition=-23w, 15%\nsize=20%\ncolor=255\n\n[FFI_LandingGears4]\ntype=sprite\nsprite=Square\nrotation=110\nanchor=left\nposition=-20w, 10%\nsize=13%, 8%\ncolor=255\n\n[FFI_LandingGears5]\ntype=sprite\nsprite=SemiCircle\nposition=-23w, -10%\nsize=20%\ncolor=255\n\n[FFI_LandingGears6]\ntype=sprite\nsprite=Square\nposition=-25w, -10%\nsize=28%, 5%\ncolor=255\n\n[FFI_LandingGears_ColoredBar_Locked]\ntype=sprite\nsprite=Square\nposition=-25w, -14%\nsize=30%, 3%\ncolor=#66ff66\n\n[FFI_LandingGears_ColoredBar_Unlocked]\ntype=sprite\nsprite=Square\nposition=-25w, -14%\nsize=30%, 3%\ncolor=#e33\ndata=landing gears\nhide=> 0\n\n[FFI_LandingGears_Distance_Value]\ntype=value\ndata=ruler\nposition=-25w, -25%\nunit=show\ndecimals=1\n\n\n[FFI_Connector1]\ntype=sprite\nsprite=SemiCircle\nrotation=180\nposition=25w, -8%\nsize=20%, 16%\n\n[FFI_Connector2]\ntype=sprite\nsprite=Square\nposition=25w, 0%\nsize=20%\n\n[FFI_Connector3]\ntype=sprite\nsprite=SemiCircle\nposition=25w, 8%\nsize=20%, 16%\n\n[FFI_Connector_ColoredBar_Locked]\ntype=sprite\nsprite=Circle\nposition=25w, 0%\nsize=19%\ncolor=#66ff66\n\n[FFI_Connector_ColoredBar_Unlocked]\ntype=sprite\nsprite=Circle\nposition=25w, 0%\nsize=19%\ncolor=#e33\ndata=connectors\nhide=> 0\n\n[FFI_Connector_InnerCircle]\ntype=sprite\nsprite=Circle\nposition=25w, 0%\nsize=13%\ncolor=150\n" },
				{ "space rover", "\n;========================== Fuel\n[FFI H2 time background triangle]\ntype = sprite\nsprite = Triangle\nrotation = 180\nposition = 0, -25h\nsize = 150h\ncolor = 254, 94, 0\n\n[FFI H2 time value]\ntype = value\ndata = hydrogen time\nsize = 20h\nposition = 0, 25h\nunit = show\n\n[FFI H2 bar]\ntype = bar\ndata = hydrogen\nsize = 62h, 10h \nposition = 55h, -2h\ncolor = 254, 94, 0\nbackground = 50\nanchor = left\nrotation = -60\n\n[FFI Battery bar]\ntype = bar\ndata = battery charge\nsize = 62h, 10h \nposition = -55h, -2h\ncolor = 0, 94, 254\nbackground = 50\nanchor = left\nrotation = -120\n\n[FFI H2 time background square]\ntype = sprite\nsprite = Square\nposition = 0, -25h\nsize = 125h, 50h\ncolor = 0\n\n[FFI H2 icon]\ntype = sprite\nsprite = IconHydrogen\nsize = 14h \nposition = 55h, -8h\ncolor = 255, 100\n\n[FFI Battery icon]\ntype = sprite\nsprite = IconEnergy\nsize = 14h \nposition = -55h, -8h\ncolor = 255, 100\n\n;========================== Left bars\n[FFI Speed bar]\ntype = bar\ndata = speed\nsize = 6.7w, 7h;\ncolor = 0, 94, 254\nbackground = 50\nposition = -50w, 24h\nanchor = left\n\n[FFI V-speed bar]\ntype = bar\ndata = vertical speed\nsize = 10w, 7h;\ncolor = 0, 94, 254\nbackground = 50\nposition = -50w, -1h\nanchor = left\n\n[FFI V-speed bar]\ntype = bar\ndata = vertical speed\nsize = 10w, 7h;\ncolor = 254, 94, 94\nposition = -50w, -1h\nanchor = left\nrotation = 180\nmin = -100\nmax = 0\n\n;========================== Black triangle\n\n[FFI Right angle triangle]\ntype = sprite\nsprite = Triangle\nrotation = 180\nposition = -36w, 0\nsize = 100h\ncolor = 0\n\n;========================== Left text\n[FFI Speed value]\ntype = value\ndata = speed\nsize = 20h\nposition = -41w, 25h\nanchor = left\nunit = show, auto\ndecimals = 0\n\n[FFI v-Speed value]\ntype = value\ndata = vertical speed\nsize = 20h\nposition = -37w, 0\nanchor = left\nunit = show, auto\ndecimals = 0\n\n;========================== Right bars\n[FFI Handbrake checkbox]\ntype = bar\ndata = landing gears\nsize = 10w, 7h;\ncolor = 254, 94, 0\nbackground = 50\nposition = 50w, 24h\nanchor = right\n\n[FFI Landing gears checkbox]\ntype = bar\ndata = handbrake\nsize = 12w, 7h;\ncolor = 254, 94, 0\nbackground = 50\nposition = 50w, -1h\nanchor = right\n\n[FFI Lift checkbox]\ntype = bar\ndata = lift\nsize = 14w, 7h;\ncolor = 254, 94, 0\nbackground = 50\nposition = 50w, -26h\nanchor = right\nmin = 0\nmax = 1\n\n;========================== Black triangle\n\n[FFI Right angle triangle]\ntype = sprite\nsprite = Triangle\nrotation = 180\nposition = 36w, 0\nsize = 100h\ncolor = 0\n\n;========================== Right text\n[FFI Landing gears text]\ntype = text\ntext = L-gear\nsize = 20h\nanchor = right\nposition = 41w, 25h\n\n[FFI Handbrake text]\ntype = text\ntext = E-brake\nsize = 20h\nanchor = right\nposition = 37w, 0\n\n[FFI Lift  text]\ntype = text\ntext = Thrusters\nsize = 20h\nanchor = right\nposition = 34w, -25h\ndata = lift\nhide = > 0\n\n[FFI Lift val]\ntype = value\ndata = lift\nsize = 20h\nanchor = right\nposition = 34w, -25h\nhide = == 0\nunit = show\n"}
			};
		}

		#endregion
		#region post-script
	}
}
#endregion