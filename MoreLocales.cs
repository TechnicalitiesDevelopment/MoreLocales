using MoreLocales.Utilities;
using Terraria.ModLoader;

namespace MoreLocales
{
	public class MoreLocales : Mod
	{
        public override void Unload()
        {
            FontHelper.ResetFont(true);
        }
    }
}
