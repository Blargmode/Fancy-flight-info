#region pre-script
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
#endregion
namespace IngameScript
{
	#region untouched
	/*
	Blargmode's Fancy Flight Info (FFI) (V4.0.3, 2019-05-09)

	This script gives you free hands in creating a fancy cockpit with data about your ship. 
	When this project started, I intended to replace the awful hud, then the hud got better. Now it's mostly for looks. 

	There's no settings in this file.

	__/ Setup \__

	• Make sure there's a cockpit/flight seat on the ship. The script uses it to gather a lot of the data and for figuring 
	  out which way is down. If you want a specific block to be used, check the "main cockpit" checkbox.

	• Add the script to a programmable block. 
	
	__/ Creating panels \__

	This place is not good for describing this. Go to the workshop for detailed info. 

	Setup guide: https://steamcommunity.com/sharedfiles/filedetails/?id=907697156

	If you refuse or can't visit the workshop/watch the setup video, there is a way.
	You can figure out a whole lot in-game as well if you're a bit crafty. Start by loading a preload like this:
	1. Type the following 3 lines in Custom Data of a block with screens, e.g. an LCD panel.
	   [FFI 1]
	   screen=1
	   preload=speed
	2. Type "update" in the argument box of the programmable block and press run.

	This loads a setup into Custom Data of the LCD. If you misspell speed when doing that, an error message in the programmable 
	block will show you all available preloads. Looking at them and experimenting a bit should give you a pretty good idea of 
	how to make your own panel.
	Misspelling things in general can be a good idea since there's a lot of info hidden in error messages. 

	Good luck!











































	*/
	#endregion
}
