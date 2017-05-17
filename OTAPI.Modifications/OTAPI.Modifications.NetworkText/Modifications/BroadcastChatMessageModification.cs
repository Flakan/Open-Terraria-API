﻿using Microsoft.Xna.Framework;
using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Modification;
using System.Collections.Generic;

namespace OTAPI.Modifications.NetworkText.Modifications
{
	public class BroadcastChatMessageModification : ModificationBase
	{
		public override IEnumerable<string> AssemblyTargets => new[]
		{
			"TerrariaServer, Version=1.3.5.3, Culture=neutral, PublicKeyToken=null",
		};

		public override string Description => "Hooking Terraria.NetMessage.BroadcastChatMessage";

		public override void Run()
		{
			Color tmpColor = new Color();
			int tmpInt = 0;
			var broadcastChatMessage = Method<Terraria.NetMessage>("BroadcastChatMessage", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
			var broadcastChatMessageBeforeCallback = Method(() => Callbacks.Terraria.NetMessage.BeforeBroadcastChatMessage(null, ref tmpColor, ref tmpInt));
			var broadcastChatMessageAfterCallback = Method(() => Callbacks.Terraria.NetMessage.AfterBroadcastChatMessage(null, ref tmpColor, ref tmpInt));

			broadcastChatMessage.Wrap(broadcastChatMessageBeforeCallback, 
				broadcastChatMessageAfterCallback,
				beginIsCancellable: true,
				noEndHandling: true,
				allowCallbackInstance: false);
		}
	}
}
