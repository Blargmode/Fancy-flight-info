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
	class Skin
	{
		Program P;
		IMyTextSurface Surface;
		IMyTerminalBlock Block;
		List<IMeter> Meters;
		SurfaceMath SM;
		SkinDefinition SkinDef;
		Data DataInUse;



		public Skin(Program P, IMyTextSurface surface, SurfaceMath surfaceMath, IMyTerminalBlock block, SkinDefinition skinDefinition, List<IMeter> meters)
		{
			this.P = P;
			Surface = surface;
			SM = surfaceMath;
			Block = block;
			SkinDef = skinDefinition;
			Meters = meters;
			
			foreach (var meter in SkinDef.meters)
			{
				//This data could also be acceses if meters.def was public.
				DataInUse |= meter.data;
			}
			
			//These 3 could be run every interation, but why?
			//On setup should be enough.
			Surface.ContentType = ContentType.SCRIPT;
			Surface.Script = "";
			if (skinDefinition.backgroundColorSet)
				Surface.ScriptBackgroundColor = skinDefinition.backgroundColor;
		}

		public void Draw(Data dataChanged)
		{
			if(Meters.Count == 0)
			{
				//No meters, show a helpful placeholder
				using (var frame = Surface.DrawFrame())
				{
					// Fill background with background color 
					MySprite sprite;
					sprite = new MySprite(SpriteType.TEXTURE, "UVChecker", color: Color.White);
					sprite.Size = SM.BGSize;
					sprite.Position = SM.Center;
					frame.Add(sprite);
					
					sprite = new MySprite(SpriteType.TEXTURE, "SquareSimple", color: Color.Red); //This cannot be reused?
					sprite.Size = new Vector2(110, 110);
					sprite.Position = SM.Center;
					frame.Add(sprite);

					//Add text
					MySprite textSprite; //This can be reused after running frame.Add
					
					textSprite = MySprite.CreateText($"{SkinDef.screenId+1}", "Debug", Color.White, 3f, TextAlignment.CENTER);
					Vector2 center = SM.Center;
					center.Y -= SM.TextHeight(3f) * 0.5f;
					center.Y += 12f;
					textSprite.Position = center;
					frame.Add(textSprite);

					textSprite = MySprite.CreateText("Screen", "Debug", Color.White, 1f, TextAlignment.CENTER);
					center = SM.Center;
					center.Y -= SM.TextHeight(4f) * 0.5f;
					center.Y += 12f;
					textSprite.Position = center;
					frame.Add(textSprite);

					textSprite = MySprite.CreateText(Surface.SurfaceSize.X.ToString("#.#") + " x " + Surface.SurfaceSize.Y.ToString("#.#"), "Debug", Color.White, 1f, TextAlignment.LEFT);
					textSprite.Position = SM.TopLeft;
					frame.Add(textSprite);
				}
				return;
			}

			if (true || (DataInUse & dataChanged) != 0) // DataInUse == Data.None //Option for drawing without data. Need to run profiler on multiple displays.
			{
				using (var frame = Surface.DrawFrame())
				{
					if (SkinDef.background != null)
					{
						// Fill background with sprite color 
						MySprite sprite;
						sprite = new MySprite(SpriteType.TEXTURE, SkinDef.background);
						if (SkinDef.colorSet)
						{
							sprite.Color = SkinDef.color;
						}
						else
						{
							sprite.Color = Surface.ScriptForegroundColor;
						}
						sprite.Size = SM.BGSize;
						sprite.Position = SM.Center;
						frame.Add(sprite);
					}

					//Idea: Send 'dataChanged' to the meters and let them return either a new sprite or the same as before.
					foreach (var meter in Meters)
					{
						meter.Draw(frame, dataChanged);
					}
				}
			}
			
			
		}
	}
	#endregion
}
