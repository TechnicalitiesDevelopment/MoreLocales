using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using ReLogic.Content;
using Terraria.ModLoader;
using Terraria.Localization;
using MoreLocales.Core;
using MoreLocales.Utilities;
using Terraria.Chat;
using Terraria.UI.Chat;
using Terraria.GameContent;
using Humanizer;
using Terraria.Audio;
using Terraria.ID;
using System.Text.RegularExpressions;
using ReLogic.Graphics;

namespace MoreLocales.Common
{
    public class BetterLangMenuUI : UIState, IHaveBackButtonCommand
    {
        public static Asset<Texture2D> _panelTexture;
        public UIState PreviousUIState { get; set; }
        public List<LanguageButton> buttons = [];
        public BetterLangMenuUI()
        {
            Main.OnResolutionChanged += ResolutionChanged;
        }
        private void ResolutionChanged(Vector2 obj)
        {
            RecalculateButtonPositions();
            Recalculate();
        }
        ~BetterLangMenuUI()
        {
            Main.OnResolutionChanged -= ResolutionChanged;
        }
        static BetterLangMenuUI()
        {
            _panelTexture = ModContent.Request<Texture2D>("MoreLocales/Assets/BetterLangPanel");
        }
        public override void OnInitialize()
        {
            foreach (var culture in GameCulture.KnownCultures)
            {
                var newButton = new LanguageButton(culture);
                buttons.Add(newButton);
                Append(newButton);
            }
            RecalculateButtonPositions();
        }
        public int ButtonsCount => buttons.Count;
        public static int WrapVertical => 9;
        public static int SingleButtonWidth => 256;
        public static int SingleButtonHeight => 48;
        public int Columns => (ButtonsCount + WrapVertical - 1) / WrapVertical;
        public static int Padding => 16;
        public static int PaddingBetweenButtons => 8;
        public void RecalculateButtonPositions()
        {
            int buttonsCount = ButtonsCount;
            int wrapVertical = WrapVertical;

            int singleButtonWidth = SingleButtonWidth;
            int singleButtonHeight = SingleButtonHeight;

            int columns = Columns;

            int padding = PaddingBetweenButtons;

            int allButtonsWidth = (singleButtonWidth + padding) * columns;

            float startX = Main.screenWidth / 2f - (allButtonsWidth / 2f);
            int startY = 256;

            for (int i = 0; i < buttonsCount; i++)
            {
                int column = (i / wrapVertical);
                int row = i % wrapVertical;

                LanguageButton b = buttons[i];
                b.Left.Set(startX + ((singleButtonWidth + padding) * column), 0f);
                b.Top.Set(startY + ((singleButtonHeight + padding) * row), 0f);
                b.Width.Set(singleButtonWidth, 0f);
                b.Height.Set(singleButtonHeight, 0f);
            }
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            Vector2 start = ButtonsCount > 0 ? buttons[0].GetDimensions().ToRectangle().TopLeft() : Vector2.Zero;
            int allButtonsWidth = (SingleButtonWidth + PaddingBetweenButtons) * Columns;
            float startX = Main.screenWidth / 2f - (allButtonsWidth / 2f);

            if (startX != start.X)
            {
                RecalculateButtonPositions();
                Recalculate();
            }

            int padding = Padding;
            int buttonPadding = PaddingBetweenButtons;
            Rectangle frameBounds = new((int)start.X - padding, (int)start.Y - padding, ((SingleButtonWidth + buttonPadding) * Columns) + (padding + padding), ((SingleButtonHeight + buttonPadding) * WrapVertical) + (padding + padding));
            UIHelper.DrawAdjustableBox(spriteBatch, _panelTexture.Value, frameBounds, Color.White);
            base.Draw(spriteBatch);
        }
    }
    public class LanguageButton : UIElement
    {
        private static Asset<Texture2D> _flagAtlas;
        public static Asset<Texture2D> _panelHighlight;
        private const int _flagFrames = 2;
        private readonly GameCulture _culture;
        private Rectangle _flagFrame = Rectangle.Empty;
        private  LocalizedText _cultureTitle;
        private LocalizedText _cultureSubtitle;
        private LocalizedText _cultureDescription = null;
        private bool Interactable => _culture != LanguageManager.Instance.ActiveCulture;
        private const string _culturesKey = "Mods.MoreLocales.Cultures";
        private bool hovered = false;
        static LanguageButton()
        {
            _flagAtlas = ModContent.Request<Texture2D>("MoreLocales/Assets/Flags");
            _panelHighlight = ModContent.Request<Texture2D>("MoreLocales/Assets/BetterLangPanel_Highlight");
        }
        public LanguageButton(GameCulture culture)
        {
            _culture = culture;

            string cultureName = culture.IsCustom() ? ((CultureNamePlus)culture.LegacyId).ToString() : ((GameCulture.CultureName)culture.LegacyId).ToString();
            string cultureKey = $"{_culturesKey}.{cultureName}";

            _cultureTitle = Language.GetOrRegister($"{cultureKey}.Title");

            if (culture.HasSubtitle())
                _cultureSubtitle = Language.GetOrRegister($"{cultureKey}.Subtitle");

            if (culture.HasDescription())
                _cultureDescription = Language.GetOrRegister($"{cultureKey}.Description");

            OnLeftClick += Clicked;
            OnMouseOver += Hovered;
            OnMouseOut += Unhovered;
        }
        private void Clicked(UIMouseEvent evt, UIElement listeningElement)
        {
            if (!Interactable)
                return;

            LanguageManager.Instance.SetLanguage(_culture);
        }

        private void Hovered(UIMouseEvent evt, UIElement listeningElement)
        {
            if (!Interactable)
                return;
            hovered = true;
            SoundEngine.PlaySound(in SoundID.MenuTick);
        }

        private void Unhovered(UIMouseEvent evt, UIElement listeningElement)
        {
            hovered = false;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

        }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Texture2D tex = BetterLangMenuUI._panelTexture.Value;

            bool interact = Interactable;

            Color drawColor = interact ? Color.White : Color.Gray;

            Rectangle bounds = GetDimensions().ToRectangle();

            Vector2 pos = bounds.TopLeft();

            UIHelper.DrawAdjustableBox(spriteBatch, tex, bounds, drawColor);

            if (hovered && interact)
                UIHelper.DrawAdjustableBox(spriteBatch, _panelHighlight.Value, bounds, Color.White);

            if (_flagFrame == Rectangle.Empty)
            {
                int cultureI2D = (_culture.LegacyId > (int)CultureNamePlus.Indonesian || _culture.LegacyId >= _flagFrames) ? 0 : _culture.LegacyId;
                _flagFrame = _flagAtlas.Frame(1, _flagFrames, 0, cultureI2D);
            }

            int padding = BetterLangMenuUI.PaddingBetweenButtons;

            Texture2D flag = _flagAtlas.Value;
            Vector2 flagOffset = new(6f);
            spriteBatch.Draw(flag, pos + flagOffset, _flagFrame, drawColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            DynamicSpriteFont font = FontAssets.MouseText.Value;

            Vector2 center = pos + ((bounds.Size() + new Vector2(flag.Width + flagOffset.X, 0f)) * 0.5f);

            bool sub = _culture.HasSubtitle();
            if (sub)
            {
                float subSize = 0.85f;
                string subtitle = _cultureSubtitle.Value;
                float xSize = font.MeasureString(subtitle).X * subSize;
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, subtitle, center - new Vector2(xSize * 0.5f, 0f), drawColor, 0f, Vector2.Zero, new Vector2(subSize));
            }
            string title = _cultureTitle.Value;
            float xSizeTitle = font.MeasureString(title).X;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, _cultureTitle.Value, center - new Vector2(xSizeTitle * 0.5f, sub ? 18f : 10f), drawColor, 0f, Vector2.Zero, Vector2.One) ;

            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, $"({_culture.Name})", pos + new Vector2(8f, 32f), drawColor, 0f, Vector2.Zero, new Vector2(0.75f));
        }
    }
}
