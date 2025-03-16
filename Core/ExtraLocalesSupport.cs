using MonoMod.RuntimeDetour;
using MoreLocales.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace MoreLocales.Core
{
    public class ExtraLocalesSupport : ILoadable
    {
        private const string customCultureDataName = "LocalizationPlusData.dat";
        private static CultureNamePlus loadedCulture = CultureNamePlus.Unknown;
        public static readonly Dictionary<CultureNamePlus, GameCulture> extraCultures = [];

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_NamedCultures")]
        public static extern ref Dictionary<GameCulture.CultureName, GameCulture> GetNamedCultures(GameCulture type = null);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "_legacyCultures")]
        public static extern ref Dictionary<int, GameCulture> GetLegacyCultures(GameCulture type = null);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SetTitle")]
        public static extern void CallSetTitle(Main instance);

        void ILoadable.Load(Mod mod)
        {
            IL_LanguageManager.ReloadLanguage += AddFallbacks;
            On_LanguageManager.SetLanguage_GameCulture += On_LanguageManager_SetLanguage_GameCulture;

            var vanillaNamedCultures = GetNamedCultures();

            CultureNamePlus[] values = Enum.GetValues<CultureNamePlus>();
            for (int i = 0; i < values.Length; i++)
            {
                CultureNamePlus newCulture = values[i];

                if (newCulture == CultureNamePlus.Unknown)
                    continue;

                GameCulture generatedCulture = new(newCulture.LangCode(), (int)newCulture);

                extraCultures.Add(newCulture, generatedCulture);
                vanillaNamedCultures.Add((GameCulture.CultureName)newCulture, generatedCulture);
            }
        }

        private void On_LanguageManager_SetLanguage_GameCulture(On_LanguageManager.orig_SetLanguage_GameCulture orig, LanguageManager self, GameCulture culture)
        {
            if (culture.TryGetLocalizedFont(out LocalizedFont font))
            {
                FontHelper.SwitchFont(font);
            }
            else
            {
                FontHelper.SwitchFont(LocalizedFont.Default);
            }
            orig(self, culture);
        }

        private static void AddFallbacks(ILContext il)
        {
            Mod mod = ModContent.GetInstance<MoreLocales>();
            try
            {
                // first we need to add a local var for our custom GameCulture
                var localGameCulture = new VariableDefinition(il.Import(typeof(GameCulture)));
                il.Body.Variables.Add(localGameCulture);

                var c = new ILCursor(il);

                // this is inside the if statement, so we already know that the active culture isn't english
                if (!c.TryGotoNext(i => i.MatchLdarg0(), i => i.MatchLdarg0(), i => i.MatchCall<LanguageManager>("get_ActiveCulture")))
                {
                    mod.Logger.Warn("AddFallbacks: Couldn't find in-between step insertion position");
                    return;
                }

                // load this in order to consume it for our delegate
                c.EmitLdarg0();

                // figure out if the current lang has a fallback defined
                c.EmitDelegate<Func<LanguageManager, GameCulture>>(l =>
                {
                    CultureNamePlus possibleCustomCulture = (CultureNamePlus)l.ActiveCulture.LegacyId;
                    if (extraCultures.ContainsKey(possibleCustomCulture))
                    {
                        GameCulture.CultureName possibleFallback = possibleCustomCulture.FallbackLang();
                        if (possibleFallback != GameCulture.CultureName.English)
                        {
                            return GetNamedCultures()[possibleFallback];
                        }
                    }
                    return null;
                });

                // store that value in the variable
                c.EmitStloc(localGameCulture.Index);

                var skipLabel = il.DefineLabel();

                // load the variable to check if it's null
                c.EmitLdloc(localGameCulture.Index);

                // if it's null, skip the call
                c.EmitBrfalse(skipLabel);

                // otherwise, load arguments
                c.EmitLdarg0();
                c.EmitLdloc(localGameCulture.Index);

                // then call the method
                c.EmitCall(typeof(LanguageManager).GetMethod("LoadFilesForCulture", BindingFlags.Instance | BindingFlags.NonPublic));

                // it should skip to after the call
                c.MarkLabel(skipLabel);
            }
            catch
            {
                MonoModHooks.DumpIL(mod, il);
            }
        }

        public static void LoadCustomCultureData()
        {
            string pathToCustomCultureData = Path.Combine(Main.SavePath, customCultureDataName);

            if (!File.Exists(pathToCustomCultureData))
                return;

            using var reader = new BinaryReader(File.Open(pathToCustomCultureData, FileMode.Open));
            CultureNamePlus culture = (CultureNamePlus)reader.ReadByte();

            if (!culture.IsValid())
                return;

            loadedCulture = culture;

            LanguageManager.Instance.SetLanguage(extraCultures[loadedCulture]);
            CallSetTitle(Main.instance);
        }
        #region mightnotbeneeded
        private GameCulture On_GameCulture_FromLegacyId(On_GameCulture.orig_FromLegacyId orig, int id)
        {
            throw new NotImplementedException();
        }

        private static GameCulture On_GameCulture_FromCultureName(On_GameCulture.orig_FromCultureName orig, GameCulture.CultureName name)
        {
            GameCulture possibleCulture = orig(name);

            // if this culture is already valid then just return that culture
            if (possibleCulture != GameCulture.DefaultCulture)
                return possibleCulture;

            if (!Enum.TryParse(((int)name).ToString(), out CultureNamePlus validExtraCulture))
            {
                return possibleCulture;
            }
            return extraCultures[validExtraCulture];

        }
        #endregion
        void ILoadable.Unload()
        {
            string pathToCustomCultureData = Path.Combine(Main.SavePath, customCultureDataName);

            void WriteFile()
            {
                using var writer = new BinaryWriter(File.Open(pathToCustomCultureData, FileMode.OpenOrCreate));
                writer.Write((byte)LanguageManager.Instance.ActiveCulture.LegacyId);
            }

            if (!File.Exists(pathToCustomCultureData))
            {
                WriteFile();
            }
            else
            {
                File.WriteAllText(pathToCustomCultureData, "");
                WriteFile();
            }

            UnregisterCultures();
        }
        private static void UnregisterCultures()
        {
            // TODO: Revert to previous vanilla culture instead of the default culture.
            LanguageManager.Instance.SetLanguage(GameCulture.DefaultCulture);
            CallSetTitle(Main.instance);

            extraCultures.Clear();

            var vanillaLegacyCultures = GetLegacyCultures();
            var vanillaNamedCultures = GetNamedCultures();

            CultureNamePlus[] values = Enum.GetValues<CultureNamePlus>();
            for (int i = 0; i < values.Length; i++)
            {
                CultureNamePlus newCulture = values[i];

                if (newCulture == CultureNamePlus.Unknown)
                    continue;

                vanillaLegacyCultures.Remove((int)newCulture);
                vanillaNamedCultures.Remove((GameCulture.CultureName)newCulture);
            }
        }
    }
}
