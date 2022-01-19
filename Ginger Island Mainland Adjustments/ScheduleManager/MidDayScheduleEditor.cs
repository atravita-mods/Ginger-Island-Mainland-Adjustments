﻿using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using GingerIslandMainlandAdjustments.Utils;

namespace GingerIslandMainlandAdjustments.ScheduleManager;

/// <summary>
/// Class that handles middle of the day schedule editing.
/// </summary>
internal class MidDayScheduleEditor
{
    /// <summary>
    /// When Ginger Island schedules end.
    /// </summary>
    private const int GIEndTime = 1800;

    /// <summary>
    /// Which map is the resort on.
    /// </summary>
    private const string GIMap = "IslandSouth";

    /// <summary>
    /// Keep track of the NPCs I've edited already, so I don't edit anyone twice.
    /// </summary>
    private readonly Dictionary<string, bool> scheduleAltered = new();

    /// <summary>
    /// Instance of a class than handles finding the schedules.
    /// </summary>
    private readonly ScheduleManager scheduleManager = new();

    /// <summary>
    /// Regex for a schedulepoint format.
    /// </summary>
    [RegexPattern]
    private readonly Regex scheduleRegex = new(
        // <time> [location] <tileX> <tileY> [facingDirection] [animation] \"[dialogue]\"
        pattern: @"(?<arrival>a)?(?<time>\d{1,4})(?<location> \S+)*?(?<x> \d{1,4})(?<y> \d{1,4})(?<direction> \d)?(?<animation> [^\s\""]+)?(?<dialogue> \"".*\"")?",
        options: RegexOptions.CultureInvariant | RegexOptions.Compiled,
        new TimeSpan(1000000));

    /// <summary>
    /// Clears the ScheduleAltered dictionary.
    /// </summary>
    public void Reset()
    {
        this.scheduleAltered.Clear();
        Globals.ModMonitor.Log("Reset scheduleAltered", LogLevel.Trace);
    }

    /// <summary>
    /// Attempt a mid-day adjustment of a character's schedule
    /// if they're headed to Ginger Island.
    /// Does one per ten minutes.
    /// </summary>
    /// <param name="sender">Unknown, never used.</param>
    /// <param name="e">Time Changed Parameters from SMAPI.</param>
    public void AttemptAdjustGISchedule(object? sender, TimeChangedEventArgs e)
    {
        if (e.NewTime >= 900)
        { // skip after 9AM.
            return;
        }
        if (Globals.Config.UseThisScheduler)
        {// fancy scheduler is on.
            return;
        }
        foreach (string name in Game1.netWorldState.Value.IslandVisitors.Keys)
        {
            if (name.Equals("Gus", StringComparison.OrdinalIgnoreCase))
            { // Gus runs saloon, skip.
                continue;
            }
            if (!Game1.netWorldState.Value.IslandVisitors[name])
            {
                continue;
            }
            if (this.scheduleAltered.TryGetValue(name, out bool hasbeenaltered) && hasbeenaltered)
            {
                continue;
            }
            Globals.ModMonitor.Log(I18n.MiddayScheduleEditor_NpcFoundForAdjustment(name), LogLevel.Trace);
            this.scheduleAltered[name] = true;
            NPC npc = Game1.getCharacterFromName(name);
            if (npc is not null)
            {
                this.AdjustSpecificSchedule(npc);
                break; // Do the next person at the next ten minute tick.
            }
        }
    }

    /// <summary>
    /// Midday adjustment of a schedule for a specific NPC.
    /// </summary>
    /// <param name="npc">NPC who's schedule may need adjusting.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public bool AdjustSpecificSchedule(NPC npc)
    {
        if (npc.islandScheduleName?.Value is null || npc.islandScheduleName?.Value == string.Empty)
        {
            if (Globals.Config.EnforceGITiming)
            {
                Globals.ModMonitor.Log(I18n.MiddayScheduleEditor_NpcNotIslander(npc.Name), LogLevel.Warn);
            }
            return false;
        }
        if (npc.IsInvisible)
        {
            Globals.ModMonitor.DebugLog($"NPC {npc.Name} is invisible, not altering schedule", LogLevel.Trace);
            return false;
        }
        if (npc.Schedule is null)
        {
            if (Globals.Config.EnforceGITiming)
            {
                Globals.ModMonitor.Log(I18n.MiddayScheduleEditor_NpcHasNoSchedule(npc.Name), LogLevel.Warn);
            }
            return false;
        }

        List<int> keys = npc.Schedule.Keys.ToList();
        keys.Sort();
        if (keys.Count == 0 || keys[^1] != GIEndTime)
        {
            Globals.ModMonitor.DebugLog($"Recieved {npc.Name} to adjust but last schedule key is not {GIEndTime}");
            return false;
        }
        string? schedule = this.scheduleManager.FindProperGISchedule(npc, SDate.Now());
        Dictionary<int, SchedulePathDescription>? remainderSchedule = this.ParseSchedule(schedule, npc);

        if (remainderSchedule is not null)
        {
            npc.Schedule.Update(remainderSchedule);
        }
        return remainderSchedule is null;
    }

    /// <summary>
    /// Gets the SchedulePathDescription schedule for the schedule string,
    /// assumes a 1800 start on Ginger Island.
    /// </summary>
    /// <param name="schedule">Raw schedule string.</param>
    /// <param name="npc">NPC.</param>
    /// <returns>null if the schedule could not be parsed, a schedule otherwise.</returns>
    [ContractAnnotation("schedule:null => null")]
    public Dictionary<int, SchedulePathDescription>? ParseSchedule(string? schedule, NPC npc)
    {
        if (schedule is null)
        {
            return null;
        }
        string previousMap = GIMap;
        Point lastStop = npc.Schedule[GIEndTime].route.Peek();
        int lastx = lastStop.X;
        int lasty = lastStop.Y;
        int lasttime = GIEndTime - 10;

        Dictionary<int, SchedulePathDescription> remainderSchedule = new();

        foreach (string schedulepoint in schedule.Split('/'))
        {
            try
            {
                Match match = this.scheduleRegex.Match(schedulepoint);
                Dictionary<string, string> matchDict = match.MatchGroupsToDictionary((key) => key, (value) => value.Trim());
                int time = int.Parse(matchDict["time"]);
                if (time <= lasttime)
                {
                    Globals.ModMonitor.Log(I18n.TOOTIGHTTIMELINE(time, schedule, npc.Name), LogLevel.Warn);
                    continue;
                }

                string location = matchDict.GetValueOrDefaultOverrideNull("location", previousMap);
                int x = int.Parse(matchDict["x"]);
                int y = int.Parse(matchDict["y"]);
                string direction_str = matchDict.GetValueOrDefault("direction", "2");
                if (!int.TryParse(direction_str, out int direction))
                {
                    direction = 2;
                }

                // Adjust schedules for locations not being open....
                if (!Game1.isLocationAccessible(location))
                {
                    string replacement_loc = location + "_Replacement";
                    if (npc.hasMasterScheduleEntry(replacement_loc))
                    {
                        string[] replacementdata = npc.getMasterScheduleEntry(replacement_loc).Split();
                        x = int.Parse(replacementdata[0]);
                        y = int.Parse(replacementdata[1]);
                        if (!int.TryParse(replacementdata[2], out direction))
                        {
                            direction = 2;
                        }
                    }
                    else
                    {
                        if (Globals.Config.EnforceGITiming)
                        {
                            Globals.ModMonitor.Log(I18n.NOREPLACEMENTLOCATION(location, npc.Name), LogLevel.Warn);
                        }
                        continue; // skip this schedule point
                    }
                }

                matchDict.TryGetValue("animation", out string? animation);
                matchDict.TryGetValue("message", out string? message);

                SchedulePathDescription newpath = Globals.ReflectionHelper.GetMethod(npc, "pathfindToNextScheduleLocation").Invoke<SchedulePathDescription>(
                    previousMap,
                    lastx,
                    lasty,
                    location,
                    x,
                    y,
                    direction,
                    animation,
                    message);

                if (matchDict.TryGetValue("arrival", out string? arrival) && arrival.Equals("a", StringComparison.OrdinalIgnoreCase))
                {
                    time = Utility.ModifyTime(time, 0 - (newpath.GetExpectedRouteTime() * 10 / 10));
                }
                if (time <= lasttime)
                {
                    Globals.ModMonitor.Log(I18n.TOOTIGHTTIMELINE(time, schedule, npc.Name), LogLevel.Warn);
                    continue;
                }
                Globals.ModMonitor.DebugLog($"Adding GI schedule for {npc.Name}", LogLevel.Debug);
                remainderSchedule.Add(time, newpath);
                previousMap = location;
                lasttime = time;
                lastx = x;
                lasty = y;
                if (Globals.Config.EnforceGITiming)
                {
                    int expectedTravelTime = newpath.GetExpectedRouteTime();
                    Utility.ModifyTime(time, expectedTravelTime);
                    Globals.ModMonitor.DebugLog($"Expected travel time of {expectedTravelTime} minutes", LogLevel.Debug);
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                Globals.ModMonitor.Log(I18n.REGEXTIMEOUTERROR(schedulepoint, ex), LogLevel.Trace);
                continue;
            }
        }

        if (remainderSchedule.Count > 0)
        {
            return remainderSchedule;
        }

        return null;
    }
}