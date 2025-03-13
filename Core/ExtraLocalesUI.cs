using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace MoreLocales.Core
{
    public class ExtraLocalesUI : ModSystem
    {
        private static bool testOverlap = false;
        public override void Load()
        {
            On_Main.DrawInterface += On_Main_DrawInterface;
        }

        private void On_Main_DrawInterface(On_Main.orig_DrawInterface orig, Main self, GameTime gameTime)
        {
            orig(self, gameTime);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

            Asset<DynamicSpriteFont> testFont = ModContent.Request<DynamicSpriteFont>("MoreLocales/Assets/ItemStack-JP", AssetRequestMode.ImmediateLoad);

            Vector2 padding = new(128f);
            float yBetween = 32f;
            float xBetween = 600f;

            SpriteBatch sb = Main.spriteBatch;
            DynamicSpriteFont testVanilla = FontAssets.CombatText[0].Value;

            for (int i = 0; i < 4; i++)
            {
                string testString = i switch
                {
                    0 => "abcdefg",
                    1 => "áêç",
                    2 => "бгд",
                    3 => "汉字测试",
                    _ => ""
                };

                for (int j = 0; j < 2; j++)
                {
                    DynamicSpriteFont font = j == 0 ? testVanilla : testFont.Value;
                    sb.DrawString(font, testString, padding + new Vector2(j == 0 ? 0 : testOverlap ? 0 : xBetween, i * yBetween), Color.White);
                }
            }

            Main.spriteBatch.End();
        }

        public override void PostUpdateDusts()
        {
            if (Main.keyState.IsKeyUp(Keys.G) && !Main.oldKeyState.IsKeyUp(Keys.G))
            {
                testOverlap = !testOverlap;
            }

            if (Main.keyState.IsKeyUp(Keys.F) && !Main.oldKeyState.IsKeyUp(Keys.F))
            {
                bool hasFont = ModContent.HasAsset("MoreLocales/Assets/MouseText");
                Main.NewText(hasFont);
                if (LanguageManager.Instance.ActiveCulture.Name != "ja-JP")
                    LanguageManager.Instance.SetLanguage("ja-JP");
                else
                    LanguageManager.Instance.SetLanguage("en-US");
                /*
                foreach (var culture in GameCulture.KnownCultures)
                {
                    Main.NewText(culture.Name);
                }
                */
            }
        }
    }
}
