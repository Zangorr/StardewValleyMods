using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceShared;
using SpaceShared.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace GenericModConfigMenu.Framework
{
    internal class ModConfigMenu : IClickableMenu
    {
        /*********
        ** Fields
        *********/
        private RootElement Ui;
        private readonly Table Table;

        /// <summary>The number of field rows to offset when scrolling a config menu.</summary>
        private readonly int ScrollSpeed;

        /// <summary>Open the config UI for a specific mod.</summary>
        private readonly Action<IManifest, int> OpenModMenu;
        private bool InGame => Context.IsWorldReady;


        /*********
        ** Accessors
        *********/
        /// <summary>The scroll position, represented by the row index at the top of the visible area.</summary>
        public int ScrollRow
        {
            get => this.Table.Scrollbar.TopRow;
            set => this.Table.Scrollbar.ScrollTo(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="scrollSpeed">The number of field rows to offset when scrolling a config menu.</param>
        /// <param name="openModMenu">Open the config UI for a specific mod.</param>
        /// <param name="configs">The mod configurations to display.</param>
        /// <param name="scrollTo">The initial scroll position, represented by the row index at the top of the visible area.</param>
        public ModConfigMenu(int scrollSpeed, Action<IManifest, int> openModMenu, ModConfigManager configs, int? scrollTo = null)
        {
            this.ScrollSpeed = scrollSpeed;
            this.OpenModMenu = openModMenu;

            // init UI
            this.Ui = new RootElement();
            this.Table = new Table
            {
                RowHeight = 50,
                LocalPosition = new Vector2((Game1.uiViewport.Width - 800) / 2, 64),
                Size = new Vector2(800, Game1.uiViewport.Height - 128)
            };

            // editable mods section
            {
                // heading
                var heading = new Label
                {
                    String = I18n.List_EditableHeading(),
                    Bold = true
                };
                heading.LocalPosition = new Vector2((800 - heading.Measure().X) / 2, heading.LocalPosition.Y);
                this.Table.AddRow(new Element[] { heading });

                // mod list
                {
                    ModConfig[] editable = configs
                        .GetAll()
                        .Where(entry => entry.AnyEditableInGame || !this.InGame)
                        .OrderBy(entry => entry.ModName)
                        .ToArray();

                    foreach (ModConfig entry in editable)
                    {
                        Label label = new Label
                        {
                            String = entry.ModName,
                            Callback = _ => this.ChangeToModPage(entry.ModManifest)
                        };
                        this.Table.AddRow(new Element[] { label });
                    }
                }
            }

            // non-editable mods heading
            {
                ModConfig[] notEditable = configs
                    .GetAll()
                    .Where(entry => !entry.AnyEditableInGame && this.InGame)
                    .OrderBy(entry => entry.ModName)
                    .ToArray();

                if (notEditable.Any())
                {
                    // heading
                    var heading = new Label
                    {
                        String = I18n.List_NotEditableHeading(),
                        Bold = true
                    };
                    this.Table.AddRow(Array.Empty<Element>());
                    this.Table.AddRow(new Element[] { heading });

                    // mod list
                    foreach (ModConfig entry in notEditable)
                    {
                        Label label = new Label
                        {
                            String = entry.ModName,
                            IdleTextColor = Color.Black * 0.4f,
                            HoverTextColor = Color.Black * 0.4f
                        };

                        this.Table.AddRow(new Element[] { label });
                    }
                }
            }

            this.Ui.AddChild(this.Table);

            if (Constants.TargetPlatform == GamePlatform.Android)
                this.initializeUpperRightCloseButton();
            else
                this.upperRightCloseButton = null;

            if (scrollTo != null)
                this.ScrollRow = scrollTo.Value;
        }

        /// <inheritdoc />
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (this.upperRightCloseButton?.containsPoint(x, y) == true && this.readyToClose())
            {
                if (playSound)
                    Game1.playSound("bigDeSelect");

                Mod.ActiveConfigMenu = null;
            }
        }

        /// <inheritdoc />
        public override void receiveScrollWheelAction(int direction)
        {
            this.Table.Scrollbar.ScrollBy(direction / -this.ScrollSpeed);
        }

        /// <inheritdoc />
        public override void update(GameTime time)
        {
            base.update(time);
            this.Ui.Update();
        }

        /// <inheritdoc />
        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), new Color(0, 0, 0, 192));
            this.Ui.Draw(b);
            this.upperRightCloseButton?.draw(b); // bring it above the backdrop
            if (this.InGame)
                this.drawMouse(b);
        }

        /// <inheritdoc />
        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            this.Ui = new RootElement();

            Vector2 newSize = new Vector2(800, Game1.uiViewport.Height - 128);
            this.Table.LocalPosition = new Vector2((Game1.uiViewport.Width - 800) / 2, 64);
            foreach (Element opt in this.Table.Children)
                opt.LocalPosition = new Vector2(newSize.X / (this.Table.Size.X / opt.LocalPosition.X), opt.LocalPosition.Y);

            this.Table.Size = newSize;
            this.Table.Scrollbar.Update();
            this.Ui.AddChild(this.Table);
        }


        /*********
        ** Private methods
        *********/
        private void ChangeToModPage(IManifest modManifest)
        {
            Log.Trace("Changing to mod config page for mod " + modManifest.UniqueID);
            Game1.playSound("bigSelect");

            this.OpenModMenu(modManifest, this.ScrollRow);
        }
    }
}
