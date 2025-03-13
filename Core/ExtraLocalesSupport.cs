using MoreLocales.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

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

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "SetTitle")]
        public static extern void CallSetTitle(Main instance);

        void ILoadable.Load(Mod mod)
        {
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

            LoadCustomCultureData();
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
        }
    }
}
