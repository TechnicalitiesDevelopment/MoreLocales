﻿using Microsoft.Xna.Framework.Graphics;
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
using Terraria.ModLoader.UI;

namespace MoreLocales.Common
{
    public class BetterLangMenuUI : UIState, IHaveBackButtonCommand
    {
        public static Asset<Texture2D> _panelTexture;
        public UIState PreviousUIState { get; set; }
        public List<LanguageButton> buttons = [];
        public BackButton backButton;
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

            backButton = new();
            Append(backButton);

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

            float screenMiddle = Main.screenWidth * 0.5f;
            float startX = screenMiddle - (allButtonsWidth * 0.5f);
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

            Vector2 backButtonDimensions = new(70, 50);
            float halfX = backButtonDimensions.X * 0.5f;

            backButton.Left.Set(screenMiddle - halfX, 0f);
            backButton.Top.Set(Main.screenHeight - backButtonDimensions.Y - 50f, 0f);

            backButton.Width.Set(backButtonDimensions.X, 0f);
            backButton.Height.Set(backButtonDimensions.Y, 0f);
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
        void IHaveBackButtonCommand.HandleBackButtonUsage()
        {
            Main.MenuUI.SetState(null);
            Main.menuMode = MenuID.Settings;

            if (backButton != null)
            {
                backButton.grow = false;
                backButton.extraScale = 0f;
            }

            SoundEngine.PlaySound(in SoundID.MenuClose);
        }
    }
    public class LanguageButton : UIElement
    {
        private static Asset<Texture2D> _flagAtlas;
        public static Asset<Texture2D> _panelHighlight;
        private const int _flagFrames = 28;
        private readonly GameCulture _culture;
        private Rectangle _flagFrame = Rectangle.Empty;
        private readonly LocalizedText _cultureTitle;
        private readonly LocalizedText _cultureSubtitle;
        private readonly LocalizedText _cultureDescription = null;
        private bool Active => _culture == LanguageManager.Instance.ActiveCulture;
        private bool HasUsableOrDoesntNeedLocalizedFont => ((CultureNamePlus)_culture.LegacyId).LocalizedFontAvailable() != false;
        private bool Interactable => !Active && HasUsableOrDoesntNeedLocalizedFont;
        private const string _culturesKey = "Mods.MoreLocales.Cultures";
        private bool hovered = false;
        private bool needsLocalizedFontTitle = false;
        static LanguageButton()
        {
            _flagAtlas = ModContent.Request<Texture2D>("MoreLocales/Assets/Flags");
            _panelHighlight = ModContent.Request<Texture2D>("MoreLocales/Assets/BetterLangPanel_Highlight");
        }
        public LanguageButton(GameCulture culture)
        {
            _culture = culture;

            string cultureName = culture.FullName();
            string cultureKey = $"{_culturesKey}.{cultureName}";

            _cultureTitle = Language.GetOrRegister($"{cultureKey}.Title");

            if (culture.HasSubtitle())
                _cultureSubtitle = Language.GetOrRegister($"{cultureKey}.Subtitle");

            if (culture.HasDescription())
                _cultureDescription = Language.GetOrRegister($"{cultureKey}.Description");

            if (CultureHelper.NeedsLocalizedTitle(cultureKey))
            {
                needsLocalizedFontTitle = true;
            }

            OnLeftClick += Clicked;
            OnMouseOver += Hovered;
            OnMouseOut += Unhovered;
            OnUpdate += Upd;
        }

        private void Upd(UIElement affectedElement)
        {
            if (!ContainsPoint(Main.MouseScreen))
                return;
            bool available = HasUsableOrDoesntNeedLocalizedFont;
            if (available)
            {
                if (_cultureDescription != null)
                {
                    Main.instance.MouseText(_cultureDescription.Value);
                }
            }
            else
            {
                Main.instance.MouseText(Language.GetTextValue($"{_culturesKey}.Common.LocalizedFontUnavailable"));
            }
        }

        private void Clicked(UIMouseEvent evt, UIElement listeningElement)
        {
            if (!Interactable)
                return;

            LanguageManager.Instance.SetLanguage(_culture);
            SoundEngine.PlaySound(in SoundID.MenuOpen);
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
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Texture2D tex = BetterLangMenuUI._panelTexture.Value;

            bool active = Active;
            bool availableLocalizedFont = HasUsableOrDoesntNeedLocalizedFont;
            bool usable = !active && availableLocalizedFont;

            Color drawColor = active ? Color.DarkGray : !availableLocalizedFont ? Color.Gray : Color.White;

            Rectangle bounds = GetDimensions().ToRectangle();

            Vector2 pos = bounds.TopLeft();

            UIHelper.DrawAdjustableBox(spriteBatch, tex, bounds, drawColor);

            if (active || (hovered && usable))
                UIHelper.DrawAdjustableBox(spriteBatch, _panelHighlight.Value, bounds, Color.White);

            if (_flagFrame == Rectangle.Empty)
            {
                int cultureID = (_culture.LegacyId > (int)CultureNamePlus.Indonesian || _culture.LegacyId >= _flagFrames) ? 0 : _culture.LegacyId;
                _flagFrame = _flagAtlas.Frame(1, _flagFrames, 0, cultureID);
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
                Color drawSubColor = usable ? Color.LightGray : Color.DarkGray;
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, subtitle, center - new Vector2(xSize * 0.5f, 0f), drawSubColor, 0f, Vector2.Zero, new Vector2(subSize));
            }
            string title = needsLocalizedFontTitle ? _cultureTitle.Format(Language.GetTextValue($"{_culturesKey}.{_culture.FullName()}.{(FontHelper.IsUsingAppropriateFont(_culture) ? "LocalizedFont" : "DefaultFont")}")) : _cultureTitle.Value;
            float xSizeTitle = font.MeasureString(title).X;
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, title, center - new Vector2(xSizeTitle * 0.5f, sub ? 18f : 10f), drawColor, 0f, Vector2.Zero, Vector2.One);

            string cultureName = $"({_culture.Name})";
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, cultureName, pos + new Vector2(8f, 32f), drawColor, 0f, Vector2.Zero, new Vector2(0.75f));
        }
    }
    public class BackButton : UIElement
    {
        private IHaveBackButtonCommand DoBackAction => Parent as IHaveBackButtonCommand;
        public bool grow = false;
        public float extraScale = 0f;
        public BackButton()
        {
            OnMouseOver += Hovered;
            OnMouseOut += Unhovered;
            OnLeftClick += Clicked;
            OnUpdate += Upd;
        }

        private void Upd(UIElement affectedElement)
        {
            if (grow && extraScale < 1f)
                extraScale = Math.Min(extraScale + 0.1f, 1f);
            else if (!grow && extraScale > 0f)
                extraScale = Math.Max(extraScale - 0.1f, 0f);

            if (grow && !ContainsPoint(Main.MouseScreen))
                grow = false;
        }

        private void Unhovered(UIMouseEvent evt, UIElement listeningElement)
        {
            grow = false;
        }

        private void Hovered(UIMouseEvent evt, UIElement listeningElement)
        {
            SoundEngine.PlaySound(in SoundID.MenuTick);
            grow = true;
        }

        private void Clicked(UIMouseEvent evt, UIElement listeningElement) => DoBackAction?.HandleBackButtonUsage();

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            string text = Lang.menu[5].Value;
            DynamicSpriteFont font = FontAssets.DeathText.Value;
            float finalScale = 0.75f + (extraScale * 0.3f);
            Vector2 center = GetDimensions().Center();
            Vector2 textSize = font.MeasureString(text) * finalScale;
            Color finalColor = MiscHelper.LerpMany(extraScale, [Color.Gray, Color.White, Color.Gold]);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, center - (textSize * 0.5f), finalColor, 0f, Vector2.Zero, new Vector2(finalScale));
        }
    }
}
