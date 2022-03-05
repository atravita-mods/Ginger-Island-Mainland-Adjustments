﻿using AtraShared.Utils.Extensions;
using GingerIslandMainlandAdjustments.AssetManagers;
using GingerIslandMainlandAdjustments.CustomConsoleCommands;
using GingerIslandMainlandAdjustments.DialogueChanges;
using GingerIslandMainlandAdjustments.Integrations;
using GingerIslandMainlandAdjustments.Niceties;
using GingerIslandMainlandAdjustments.ScheduleManager;
using HarmonyLib;
using StardewModdingAPI.Events;

namespace GingerIslandMainlandAdjustments;

/// <inheritdoc />
[UsedImplicitly]
public class ModEntry : Mod
{
    private bool haveFixedSchedulesToday = false;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        Globals.Initialize(helper, this.Monitor);

        ConsoleCommands.Register(this.Helper.ConsoleCommands);

        this.ApplyPatches(new Harmony(this.ModManifest.UniqueID));

        // Register events
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        helper.Events.GameLoop.ReturnedToTitle += this.ReturnedToTitle;

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

        helper.Events.Input.ButtonPressed += this.OnButtonPressed;

        helper.Events.Player.Warped += this.OnPlayerWarped;

        // Add my asset loader and editor.
        helper.Content.AssetLoaders.Add(AssetLoader.Instance);
        helper.Content.AssetEditors.Add(AssetEditor.Instance);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        => Globals.LoadDataFromSave();

    /// <summary>
    /// Clear all caches at the end of the day and if the player exits to menu.
    /// </summary>
    private void ClearCaches()
    {
        MidDayScheduleEditor.Reset();
        IslandSouthPatches.ClearCache();
        GIScheduler.ClearCache();
        GIScheduler.DayEndReset();
        DialogueUtilities.ClearDialogueLog();
        ConsoleCommands.ClearCache();
        ScheduleUtilities.ClearCache();
        this.haveFixedSchedulesToday = false;
    }

    /// <summary>
    /// Clear caches when returning to title.
    /// </summary>
    /// <param name="sender">Unknown, never used.</param>
    /// <param name="e">Possible parameters.</param>
    private void ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.ClearCaches();
    }

    /// <summary>
    /// Clear cache at day end.
    /// </summary>
    /// <param name="sender">Unknown, never used.</param>
    /// <param name="e">Possible parameters.</param>
    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        Game1.netWorldState.Value.IslandVisitors.Clear();
        this.ClearCaches();
        NPCPatches.ResetAllFishers();
        Globals.SaveCustomData();
    }

    /// <summary>
    /// Applies and logs this mod's harmony patches.
    /// </summary>
    /// <param name="harmony">My harmony instance.</param>
    private void ApplyPatches(Harmony harmony)
    {
        try
        {
            // handle patches from annotations.
            harmony.PatchAll();
            if (Globals.Config.DebugMode)
            {
                ScheduleDebugPatches.ApplyPatches(harmony);
            }
        }
        catch (Exception ex)
        {
            Globals.ModMonitor.Log($"{I18n.HarmonyCrash()} {Globals.GithubLocation}{Environment.NewLine}{ex}", LogLevel.Error);
        }

        harmony.Snitch(Globals.ModMonitor, this.ModManifest.UniqueID);
    }

    /// <summary>
    /// Initialization after other mods have started.
    /// </summary>
    /// <param name="sender">Unknown, never used.</param>
    /// <param name="e">Possible parameters.</param>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        // Generate the GMCM for this mod.
        GenerateGMCM.Build(this.ModManifest, this.Helper.Translation);

        // Add CP tokens for this mod.
        GenerateCPTokens.AddTokens(this.ModManifest);

        // Bind Child2NPC's IsChildNPC method
        if (Globals.GetIsChildToNPC())
        {
            Globals.ModMonitor.Log("Successfully grabbed Child2NPC for integration", LogLevel.Debug);
        }
    }

    private void OnPlayerWarped(object? sender, WarpedEventArgs e)
    {
        ShopHandler.AddBoxToShop(e);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        // Thanks, RSV, for reminding me that there are other conditions for which I should probably not be handling shops....
        // From: https://github.com/Rafseazz/Ridgeside-Village-Mod/blob/816a66d0c9e667d3af662babc170deed4070c9ff/Ridgeside%20SMAPI%20Component%202.0/RidgesideVillage/TileActionHandler.cs#L37
        if (!Context.IsWorldReady || !Context.CanPlayerMove || Game1.player.isRidingHorse()
            || Game1.currentLocation is null || Game1.eventUp || Game1.isFestival() || Game1.IsFading())
        {
            return;
        }
        ShopHandler.HandleWillyShop(e);
        ShopHandler.HandleSandyShop(e);
    }

    private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
    {
        MidDayScheduleEditor.AttemptAdjustGISchedule(e);
        if (e.NewTime > 615 && !this.haveFixedSchedulesToday)
        {
            // No longer need the exclusions cache.
            IslandSouthPatches.ClearCache();

            ScheduleUtilities.FixUpSchedules();
            if (Globals.Config.DebugMode)
            {
                ScheduleDebugPatches.FixNPCs();
            }
            this.haveFixedSchedulesToday = true;
        }
    }
}
