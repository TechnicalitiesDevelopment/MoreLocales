using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreLocales.Core;
using MoreLocales.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MoreLocales
{
	public class MoreLocales : Mod
	{
        private static ILHook hook;
        private static Hook unloadHook;
        public override void PostSetupContent()
        {
            FontHelper.InitLocalizedFonts();
            ExtraLocalesSupport.cachedVanillaCulture = LanguageManager.Instance.ActiveCulture.LegacyId;
            ExtraLocalesSupport.LoadCustomCultureData();
        }
        public override void Load()
        {
            Type[] mParams =
            [
                typeof(Mod),
                typeof(string),
                typeof(GameCulture)
            ];
            MethodInfo peskyLegacyMarker = typeof(LocalizationLoader).GetMethod("UpdateLocalizationFilesForMod", BindingFlags.Static | BindingFlags.NonPublic, mParams);
            ILHook newHook = new(peskyLegacyMarker, FixPeskyLegacyMarking);
            hook = newHook;
            hook.Apply();

            // explanation:
            // unload is too late because the mod's file is already closed. the method i run tries to reload language which crashes the game at this point.
            // thought close would work. turns out close is also too late because other mods' mod files are already closed.
            // so this is the only place where it actually works.
            MethodInfo onlyFknMethodThatWorksForUnloadingCulturesAtTheCorrectPlace = typeof(ModContent).GetMethod("UnloadModContent", BindingFlags.Static | BindingFlags.NonPublic);
            Hook newHook2 = new(onlyFknMethodThatWorksForUnloadingCulturesAtTheCorrectPlace, UnloadInCorrectPlace);
            unloadHook = newHook2;
            unloadHook.Apply();

            ExtraLocalesSupport.DoLoad();
        }
        public delegate void UnloadModContentOrig();
        private static void UnloadInCorrectPlace(UnloadModContentOrig orig)
        {
            ExtraLocalesSupport.DoUnload();
            orig();
        }
        private static void FixPeskyLegacyMarking(ILContext il)
        {
            Mod mod = ModContent.GetInstance<MoreLocales>();
            try
            {
                var c = new ILCursor(il);

                MethodInfo move = typeof(File).GetMethod("Move", [typeof(string), typeof(string)]);

                if (!c.TryGotoNext
                (
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdloc(out _),
                    i => i.MatchCall(move)
                ))
                {
                    mod.Logger.Warn("FixPeskyLegacyMarking: Couldn't find start of legacy marking");
                    return;
                }

                var skipLabel = il.DefineLabel();

                c.EmitLdarg0();

                c.EmitDelegate<Func<Mod, bool>>(m =>
                {
                    return m.Name == ModContent.GetInstance<MoreLocales>().Name;
                });

                c.EmitBrtrue(skipLabel);

                if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(move)))
                {
                    mod.Logger.Warn("FixPeskyLegacyMarking: Couldn't find branch target");
                    return;
                }

                c.MarkLabel(skipLabel);
            }
            catch
            {
                MonoModHooks.DumpIL(mod, il);
            }
        }
        public override void Unload()
        {
            FontHelper.ResetFont(true);
            hook?.Dispose();
            unloadHook?.Dispose();
        }
    }
}
