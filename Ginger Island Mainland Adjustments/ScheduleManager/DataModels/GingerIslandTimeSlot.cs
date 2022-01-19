﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;

namespace GingerIslandMainlandAdjustments.ScheduleManager.DataModels;

/// <summary>
/// A single timeslot on a Ginger Island schedule.
/// </summary>
internal class GingerIslandTimeSlot
{
    /// <summary>
    /// A list of possible island activities.
    /// </summary>
    private static readonly List<PossibleIslandActivity> PossibleActivities = GenerateIslandActivtyList();

    /// <summary>
    /// Location dancers can be in relation to a musician, if one is found.
    /// </summary>
    private static readonly List<Point> DanceDeltas = new()
    {
        new Point(1, 1),
        new Point(-1, -1),
    };

    /// <summary>
    /// Where the bartender should stand.
    /// </summary>
    private static readonly Point BartendPoint = new(14, 21);

    /// <summary>
    /// Activity for drinking (The adults only!). Should only happen if a bartender is around.
    /// </summary>
    private static readonly PossibleIslandActivity Drinking = new(new List<Point>() { new Point(12, 23), new Point(15, 23) },
        basechance: 0.5,
        animation: "beach_drink",
        animation_required: false,
        dialogueKey: "Resort_Bar");

    private static readonly PossibleIslandActivity Music = PossibleActivities[0];
    private static readonly PossibleIslandActivity Dance = PossibleActivities[1];

    /// <summary>
    /// Time this timeslot takes place in.
    /// </summary>
    private readonly int timeslot;

    private readonly NPC? bartender;
    private readonly NPC? musician;
    private readonly Random random;
    private readonly List<NPC> visitors;

    private readonly Dictionary<NPC, SchedulePoint> assignments = new();

    /// <summary>
    /// Location points already used, to avoid sticking two NPCs on top of each other...
    /// </summary>
    /// <remarks>Doesn't actually distinguish between different maps! This should be okay as different maps are shaped very differently.</remarks>
    private readonly HashSet<Point> usedPoints = new();

    private readonly Dictionary<NPC, string> animations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GingerIslandTimeSlot"/> class.
    /// </summary>
    /// <param name="timeslot">Time this TimeSlot should happen at.</param>
    /// <param name="bartender">Bartender, if I have one.</param>
    /// <param name="musician">Musician, if I have one.</param>
    /// <param name="random">Seeded random.</param>
    /// <param name="visitors">List of NPC visitors.</param>
    public GingerIslandTimeSlot(int timeslot, NPC? bartender, NPC? musician, Random random, List<NPC> visitors)
    {
        this.timeslot = timeslot;
        this.bartender = bartender;
        this.musician = musician;
        this.random = random;
        this.visitors = visitors.ToList();
        Utility.Shuffle(random, this.visitors);
    }

    /// <summary>
    /// Gets dictionary of animations NPCs may be using for this GITimeSlot.
    /// </summary>
    public Dictionary<NPC, string> Animations => this.animations;

    /// <summary>
    /// Gets which time this Timeslot should happen at.
    /// </summary>
    public int TimeSlot => this.timeslot;

    /// <summary>
    /// Gets a dictionary of current assignment (as a <see cref="SchedulePoint"/>) per NPC.
    /// </summary>
    public Dictionary<NPC, SchedulePoint> Assignments => this.assignments;

    /// <summary>
    /// Tries to assign all characters to an activity.
    /// </summary>
    /// <param name="lastAssignment">The previous set of animations, to avoid repeating.</param>
    /// <param name="animationDescriptions">The animation dictionary of the game.</param>
    /// <returns>The animations used, so the next time slot has that information.</returns>
    public Dictionary<NPC, string> AssignActivities(Dictionary<NPC, string> lastAssignment, Dictionary<string, string> animationDescriptions)
    {
        // Get a list of possible dancers (who have _beach_dance as a possible animation).
        HashSet<NPC> dancers = (this.musician is not null)
            ? this.visitors.FindAll((NPC npc) => animationDescriptions.ContainsKey($"{npc.Name.ToLowerInvariant()}_beach_dance")).ToHashSet()
            : new HashSet<NPC>();

        // assign bartenders and drinkers.
        if (this.bartender is not null)
        {
            this.AssignSchedulePoint(this.bartender, new SchedulePoint(
                random: this.random,
                npc: this.bartender,
                map: "IslandSouth",
                time: this.timeslot,
                point: BartendPoint,
                basekey: "Resort_Bartend"));

            foreach (NPC possibledrinker in this.visitors)
            {
                if (!this.assignments.ContainsKey(possibledrinker) && possibledrinker.Age != 2 && !dancers.Contains(possibledrinker) && possibledrinker != this.musician)
                {
                    SchedulePoint? schedulePoint = Drinking.TryAssign(
                        random: this.random,
                        character: possibledrinker,
                        time: this.timeslot,
                        usedPoints: this.usedPoints,
                        lastAssignment: lastAssignment,
                        animation_descriptions: animationDescriptions);
                    if (schedulePoint is not null)
                    {
                        this.AssignSchedulePoint(possibledrinker, schedulePoint);
                    }
                }
            }
        }

        // assign musician and dancers
        if (this.musician is not null && !this.assignments.ContainsKey(this.musician))
        {
            SchedulePoint? musicianPoint = Music.TryAssign(
                random: this.random,
                character: this.musician,
                time: this.timeslot,
                usedPoints: this.usedPoints,
                lastAssignment: lastAssignment,
                animation_descriptions: animationDescriptions);
            if (musicianPoint is not null)
            {
                this.AssignSchedulePoint(this.musician, musicianPoint);
                Point musician_loc = musicianPoint.Point;
                PossibleIslandActivity closeDancePoint = new(DanceDeltas.Select((Point pt) => new Point(musician_loc.X + pt.X, musician_loc.Y + pt.Y)).ToList(),
                    basechance: 0.7,
                    animation: "beach_dance",
                    animation_required: true);
                foreach (NPC dancer in dancers)
                {
                    SchedulePoint? dancerPoint = closeDancePoint.TryAssign(
                        random: this.random,
                        character: dancer,
                        time: this.timeslot,
                        usedPoints: this.usedPoints,
                        lastAssignment: lastAssignment,
                        animation_descriptions: animationDescriptions)
                        ?? Dance.TryAssign(
                            this.random,
                            character: dancer,
                            time: this.timeslot,
                            usedPoints: this.usedPoints,
                            lastAssignment: lastAssignment,
                            animation_descriptions: animationDescriptions);
                    if (dancerPoint is not null)
                    {
                        this.AssignSchedulePoint(dancer, dancerPoint);
                        dancer.currentScheduleDelay = 0f;
                        this.musician.currentScheduleDelay = 0f;
                    }
                }
            }
        }

        // assign the rest of the NPCs
        foreach (NPC visitor in this.visitors)
        {
            if (this.assignments.ContainsKey(visitor))
            {
                continue;
            }
            foreach (PossibleIslandActivity possibleIslandActivity in PossibleActivities)
            {
                SchedulePoint? schedulePoint = possibleIslandActivity.TryAssign(
                    random: this.random,
                    character: visitor,
                    time: this.timeslot,
                    usedPoints: this.usedPoints,
                    lastAssignment: lastAssignment,
                    animation_descriptions: animationDescriptions);
                if (schedulePoint is not null)
                {
                    this.AssignSchedulePoint(visitor, schedulePoint);
                }
            }

            // now iterate backwards through the list, forcibly assigning people to places....
            for (int i = PossibleActivities.Count - 1; i >= 0; i--)
            {
                SchedulePoint? schedulePoint = PossibleActivities[i].TryAssign(
                    random: this.random,
                    character: visitor,
                    time: this.timeslot,
                    usedPoints: this.usedPoints,
                    lastAssignment: lastAssignment,
                    animation_descriptions: animationDescriptions,
                    overrideChanceMap: (NPC npc) => 1.0);
                if (schedulePoint is not null)
                {
                    this.AssignSchedulePoint(visitor, schedulePoint);
                }
            }
        }
        return this.animations;
    }

    /// <summary>
    /// Adds a schedulepoint to the usedPoints dictionary, the animations log, and the character's assignment.
    /// </summary>
    /// <param name="npc">NPC in question.</param>
    /// <param name="schedulePoint">SchedulePoint to assign. Null to skip.</param>
    private void AssignSchedulePoint(NPC npc, SchedulePoint schedulePoint)
    {
        this.usedPoints.Add(schedulePoint.Point);
        if (schedulePoint.Animation is not null)
        {
            this.animations[npc] = schedulePoint.Animation;
        }
        this.assignments[npc] = schedulePoint;
    }

    /// <summary>
    /// Generates a list of possible activities.
    /// </summary>
    /// <returns>List of PossibleIslandActivities.</returns>
    [Pure]
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Reviewed")]
    private static List<PossibleIslandActivity> GenerateIslandActivtyList()
    {
        return new List<PossibleIslandActivity>()
        {
            // towel lounging
            new PossibleIslandActivity(
                new List<Point> { new Point(14, 27), new Point(17, 28), new Point(20, 27), new Point(23, 28) },
                basechance: 0.5,
                dialogueKey: "Resort_Towel",
                animation_required: true,
                animation: "beach_towel"),
            // dancing
            new PossibleIslandActivity(
                new List<Point> { new Point(22, 21), new Point(23, 21) },
                basechance: 0.3,
                chanceMap: (NPC npc) => npc.Name.Equals("Emily", StringComparison.OrdinalIgnoreCase) ? 0.3 : 1,
                animation: "beach_dance",
                animation_required: true),
            // wandering
            new PossibleIslandActivity(
                new List<Point> { new Point(7, 16), new Point(31, 24), new Point(18, 13) },
                basechance: 0.4,
                dialogueKey: "Resort_Wander",
                animation: "square_3_3"),
            // under umberella
            new PossibleIslandActivity(
                new List<Point> { new Point(26, 26), new Point(28, 29), new Point(10, 27) },
                basechance: 0.1,
                chanceMap: (NPC npc) => npc.Name.Equals("Abigail", StringComparison.OrdinalIgnoreCase) ? 0.5 : 0.1,
                dialogueKey: "Resort_Umbrella"),
            // sitting on chair
            new PossibleIslandActivity(
                new List<Point> { new Point(20, 24), new Point(30, 29) },
                basechance: 0.3,
                chanceMap: (NPC npc) => npc.Age == 0 ? 0.4 : 0,
                animation: "beach_chair",
                animation_required: false),
            // antisocial point
            new PossibleIslandActivity(
                new List<Point> { new Point(3, 29) },
                basechance: 0,
                chanceMap: (NPC npc) => npc.SocialAnxiety == 1 && npc.Optimism == 1 ? 0.3 : 0,
                map: "IslandSouthEast"),
            // shore points
            new PossibleIslandActivity(
                new List<Point> { new Point(9, 33), new Point(13, 33), new Point(17, 33), new Point(24, 33), new Point(28, 32), new Point(32, 31) },
                basechance: 0.5),
            // pier points
            new PossibleIslandActivity(
                new List<Point> { new Point(22, 43), new Point(22, 41) },
                direction: 1),
        };
    }
}