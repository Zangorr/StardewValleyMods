using System;
using Magic.Schools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using SObject = StardewValley.Object;

namespace Magic.Spells
{
    internal class MeteorSpell : Spell
    {
        public MeteorSpell()
            : base(SchoolId.Eldritch, "meteor") { }

        public override int getManaCost(Farmer player, int level)
        {
            return 0;
        }

        public override bool canCast(Farmer player, int level)
        {
            return base.canCast(player, level) && player.hasItemInInventory(SObject.iridium, 1);
        }

        public override int getMaxCastingLevel()
        {
            return 1;
        }

        public override IActiveEffect onCast(Farmer player, int level, int targetX, int targetY)
        {
            player.consumeObject(SObject.iridium, 1);
            return new Meteor(player, targetX, targetY);
        }
    }

    internal class Meteor : IActiveEffect
    {
        private readonly GameLocation loc;
        private readonly Farmer source;
        private static readonly Random rand = new();
        private readonly Vector2 position;
        private readonly float yVelocity;
        private float height = 1000;

        public Meteor(Farmer theSource, int tx, int ty)
        {
            this.loc = theSource.currentLocation;
            this.source = theSource;

            this.position.X = tx;
            this.position.Y = ty;
            this.yVelocity = 64;
        }

        /// <summary>Update the effect state if needed.</summary>
        /// <param name="e">The update tick event args.</param>
        /// <returns>Returns true if the effect is still active, or false if it can be discarded.</returns>
        public bool Update(UpdateTickedEventArgs e)
        {
            // decrease height until zero
            this.height -= (int)this.yVelocity;
            if (this.height > 0)
                return true;

            // trigger explosion
            {
                Game1.playSound("explosion");
                for (int i = 0; i < 10; ++i)
                {
                    for (int ix = -i; ix <= i; ++ix)
                        for (int iy = -i; iy <= i; ++iy)
                            Game1.createRadialDebris(this.loc, Game1.objectSpriteSheetName, new Rectangle(352, 400, 32, 32), 4, (int)this.position.X + ix * 20, (int)this.position.Y + iy * 20, 15 - 14 + Meteor.rand.Next(15 - 14), (int)((double)this.position.Y / (double)Game1.tileSize) + 1, new Color(255, 255, 255, 255), 4.0f);
                }
                foreach (var npc in this.source.currentLocation.characters)
                {
                    if (npc is Monster mob)
                    {
                        float rad = 8 * 64;
                        if (Vector2.Distance(mob.position, new Vector2(this.position.X, this.position.Y)) <= rad)
                        {
                            // TODO: Use location damage method for xp and quest progress
                            mob.takeDamage(300, 0, 0, false, 0, this.source);
                            this.source.AddCustomSkillExperience(Magic.Skill, 5);
                        }
                    }
                }
                this.loc.explode(new Vector2((int)this.position.X / Game1.tileSize, (int)this.position.Y / Game1.tileSize), 4 + 2, this.source);
                return false;
            }
        }

        /// <summary>Draw the effect to the screen if needed.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            Vector2 drawPos = Game1.GlobalToLocal(new Vector2(this.position.X, this.position.Y - this.height));
            spriteBatch.Draw(Game1.objectSpriteSheet, drawPos, new Rectangle(352, 400, 32, 32), Color.White, 0, new Vector2(16, 16), 2 + 8, SpriteEffects.None, (float)(((double)this.position.Y - this.height + (double)(Game1.tileSize * 3 / 2)) / 10000.0));
        }
    }
}
