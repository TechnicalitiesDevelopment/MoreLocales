using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace MoreLocales
{
	public class MoreLocales : Mod
	{
		private static Asset<DynamicSpriteFont> StoredMouseText;
        public override void Load()
        {

        }
        public override void PostSetupContent()
        {
            StoredMouseText = FontAssets.MouseText;
            //FontAssets.MouseText = ModContent.Request<DynamicSpriteFont>("MoreLocales/Assets/MouseText", AssetRequestMode.ImmediateLoad);
        }
        public override void Unload()
        {
            FontAssets.MouseText = StoredMouseText;
        }
    }
}
