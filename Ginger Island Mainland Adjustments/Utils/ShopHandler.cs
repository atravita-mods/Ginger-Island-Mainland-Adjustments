﻿using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Locations;
using StardewValley.Menus;

namespace GingerIslandMainlandAdjustments.Utils;

/// <summary>
/// Handles shops of NPCs at resort.
/// </summary>
internal static class ShopHandler
{
    private static readonly PerScreen<bool> HandlingShop = new(createNewState: () => false);

    /// <summary>
    /// Handles running Sandy's shop if she's not there.
    /// </summary>
    /// <param name="e">Button pressed event arguments.</param>
    public static void HandleSandyShop(ButtonPressedEventArgs e)
    {
        if (!e.Button.IsActionButton() || !Game1.currentLocation.Name.Equals("SandyHouse", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        GameLocation sandyHouse = Game1.currentLocation;
        if (HandlingShop.Value || !Game1.IsVisitingIslandToday("Sandy") || sandyHouse.getCharacterFromName("Sandy") is not null)
        {
            return;
        }
        if (!Utils.YieldSurroundingTiles(Game1.player.getTileLocation()).Any((Point v) => sandyHouse.doesTileHaveProperty(v.X, v.Y, "Action", "Buildings")?.Contains("Buy") == true))
        {
            return;
        }
        IReflectedMethod? onSandyShop = Globals.ReflectionHelper.GetMethod(sandyHouse, "onSandyShopPurchase");
        IReflectedMethod? getSandyStock = Globals.ReflectionHelper.GetMethod(sandyHouse, "sandyShopStock");
        if (onSandyShop is not null && getSandyStock is not null)
        {
            HandlingShop.Value = true; // Do not want to intercept any more clicks until shop menu is finished.
            Game1.player.FacingDirection = Game1.up;
            Game1.drawObjectDialogue(I18n.SandyAwayShopMessage());
            Game1.afterDialogues = () =>
            {
                Game1.activeClickableMenu = new ShopMenu(
                        itemPriceAndStock: getSandyStock.Invoke<Dictionary<ISalable, int[]>>(),
                        on_purchase: (ISalable sellable, Farmer who, int amount) => onSandyShop.Invoke<bool>(sellable, who, amount));
                HandlingShop.Value = false;
            };
        }
    }

    /// <summary>
    /// Handles running Willy's shop if he's not there.
    /// </summary>
    /// <param name="e">Button pressed event arguments.</param>
    public static void HandleWillyShop(ButtonPressedEventArgs e)
    {
        if (!e.Button.IsActionButton() || Game1.currentLocation is not FishShop fishShop)
        {
            return;
        }
        if (HandlingShop.Value || !Game1.IsVisitingIslandToday("Willy") || fishShop.getCharacterFromName("Willy") is not null)
        {
            return;
        }
        if (!Utils.YieldSurroundingTiles(Game1.player.getTileLocation()).Any((Point v) => fishShop.doesTileHaveProperty(v.X, v.Y, "Action", "Buildings")?.Contains("Buy") == true))
        {
            return;
        }
        HandlingShop.Value = true; // Do not want to intercept any more clicks until shop menu is finished.
        Game1.player.FacingDirection = Game1.up;
        Game1.drawObjectDialogue(I18n.WillyAwayShopMessage());
        Game1.afterDialogues = () =>
        {
            Game1.activeClickableMenu = new ShopMenu(itemPriceAndStock: Utility.getFishShopStock(Game1.player));
            HandlingShop.Value = false;
        };
    }

    /// <summary>
    /// Handles adding a box to Sandy's shop if she's gone.
    /// </summary>
    /// <param name="e">On Warped event arguments.</param>
    public static void AddBoxToShop(WarpedEventArgs e)
    {
        if ((e.NewLocation.Name.Equals("SandyHouse", StringComparison.OrdinalIgnoreCase) && Game1.IsVisitingIslandToday("Sandy")
            && e.NewLocation.getCharacterFromName("Sandy") is null) // Sandy has left already
            || (e.NewLocation is FishShop fishShop && Game1.IsVisitingIslandToday("Willy") && fishShop.getCharacterFromName("Willy") is null))
        {
            Vector2 tile = e.NewLocation is FishShop ? new Vector2(5f, 5f) : new Vector2(2f, 6f); // default location of shop.
            foreach (Vector2 v in Utils.YieldAllTiles(e.NewLocation))
            { // find the shop tile - a mod may have moved it.
                if (e.NewLocation.doesTileHaveProperty((int)v.X, (int)v.Y, "Action", "Buildings")?.Contains("Buy") == true)
                {
                    tile = v;
                    break;
                }
            }

            // add box
            e.NewLocation.temporarySprites.Add(new TemporaryAnimatedSprite
            {
                texture = Game1.mouseCursors2,
                sourceRect = new Rectangle(129, 210, 13, 16),
                animationLength = 1,
                sourceRectStartingPos = new Vector2(129f, 210f),
                interval = 50000f,
                totalNumberOfLoops = 9999,
                position = (new Vector2(tile.X, tile.Y - 1) * Game1.tileSize) + (new Vector2(3f, 0f) * 4f),
                scale = 4f,
                layerDepth = (((tile.Y - 0.5f) * Game1.tileSize) / 10000f) + 0.01f, // a little offset so it doesn't show up on the floor.
                id = 777f,
            });
        }
    }
}