using System;
using System.Collections.Generic;
using System.Linq;
using Echobound.Core;
using Echobound.NPC;
using Echobound.World;

namespace Echobound.AI
{
    /// <summary>
    /// Deterministic consequence generator. Used by the mock provider and as the last-resort fallback so the world
    /// always reacts even when no AI is reachable. Rules are simple but grounded in who could plausibly know.
    /// </summary>
    public static class FallbackEventFactory
    {
        public static DirectorEvent Create(DirectorEventPayload p, SeededRandom rng)
        {
            var s = p.State;
            switch (p.Trigger)
            {
                case "NPC_DEATH": return ForNpcDeath(s, p.DeadNpc, p.Killer, rng);
                case "PLAYER_ACTION": return ForPlayerAction(s, p.Action, rng);
                default: return Ambient(s, rng);
            }
        }

        public static DirectorEvent ForNpcDeath(WorldState s, string deadId, string killer, SeededRandom rng)
        {
            var dead = s.GetNpc(deadId);
            string name = s.NpcName(deadId);
            var ev = new DirectorEvent { EventType = "FACTION_RETALIATION", Urgency = 8, Location = dead?.CurrentLocation ?? "TOWN_SQUARE", Target = "PLAYER" };
            bool playerDidIt = killer == WorldBible.PlayerId;
            string faction = dead?.Faction ?? WorldBible.NoFaction;
            var witnesses = s.Facts.Facts.Where(f => f.AboutNpc == deadId && f.AboutPlayer).SelectMany(f => s.AliveNpcs.Where(n => n.Knows(f.Id))).Select(n => n.NpcId).Distinct().ToList();

            if (playerDidIt && faction != WorldBible.NoFaction && witnesses.Count > 0)
            {
                ev.SourceFaction = faction;
                ev.Description = $"{WorldBible.FactionName(faction)} learned that the stranger killed {name}. They are sending people.";
                ev.PlayerNotification = $"You feel eyes on you. {WorldBible.FactionName(faction)} knows about {name}.";
                ev.WorldChanges.Add(WorldChange.Reputation(faction, -35));
                ev.WorldChanges.Add(WorldChange.Tension(15));
                ev.WorldChanges.Add(WorldChange.SpawnEnemies(rng.Pick(new[] { s.Player.Location, "TOWN_SQUARE", "MARKET" }), 2, faction));
                ev.WorldChanges.Add(WorldChange.Rumor($"The stranger killed {name}. {WorldBible.FactionName(faction)} wants blood."));
                foreach (var mate in s.NpcsOfFaction(faction).Take(2)) ev.WorldChanges.Add(WorldChange.SetMood(mate.NpcId, "ANGRY"));
            }
            else if (playerDidIt && witnesses.Count > 0)
            {
                ev.EventType = "ARREST_ATTEMPT";
                ev.SourceFaction = "TOWN_GUARD";
                ev.Location = "GUARD_STATION";
                ev.Description = $"Witnesses told the guard that the stranger killed {name}. The commander wants the stranger brought in.";
                ev.PlayerNotification = "The guards are asking about you.";
                ev.WorldChanges.Add(WorldChange.Reputation("TOWN_GUARD", -25));
                ev.WorldChanges.Add(WorldChange.Tension(10));
                var commander = s.Npcs.FirstOrDefault(n => n.Role == "GUARD_COMMANDER" && n.Alive);
                if (commander != null) ev.WorldChanges.Add(WorldChange.Goal(commander.NpcId, $"Question the stranger about {name}'s death"));
            }
            else if (playerDidIt)
            {
                ev.EventType = "RUMOR_SPREAD";
                ev.Description = $"{name} is dead and nobody saw who did it. Fear spreads faster than facts.";
                ev.PlayerNotification = "";
                ev.WorldChanges.Add(WorldChange.Tension(10));
                ev.WorldChanges.Add(WorldChange.Rumor($"{name} was found dead. Nobody knows who did it. Yet."));
            }
            else
            {
                ev.EventType = "REVELATION";
                ev.SourceFaction = killer != null && WorldBible.IsValidFaction(killer) ? killer : "NONE";
                ev.Description = $"{name} was killed. The town is frightened and the guard is looking for someone to blame.";
                ev.PlayerNotification = $"Word spreads: {name} is dead.";
                ev.WorldChanges.Add(WorldChange.Tension(15));
                ev.WorldChanges.Add(WorldChange.Rumor($"{name} was killed last night."));
                var priest = s.Npcs.FirstOrDefault(n => n.Role == "PRIEST" && n.Alive);
                if (priest != null) ev.WorldChanges.Add(WorldChange.SetMood(priest.NpcId, "GRIEVING"));
            }
            return ev;
        }

        public static DirectorEvent ForPlayerAction(WorldState s, PlayerAction a, SeededRandom rng)
        {
            var target = a?.TargetNpcId != null ? s.GetNpc(a.TargetNpcId) : null;
            string faction = target?.Faction ?? WorldBible.NoFaction;
            var ev = new DirectorEvent { Urgency = Math.Min(10, (a?.Importance ?? 3) + 1), Location = a?.LocationId ?? s.Player.Location };
            switch (a?.Type)
            {
                case PlayerActionType.THREATENED:
                case PlayerActionType.ATTACKED_NPC:
                    if (faction != WorldBible.NoFaction)
                    {
                        ev.EventType = "FACTION_RETALIATION"; ev.SourceFaction = faction;
                        ev.Description = $"{target.DisplayName} told {WorldBible.FactionName(faction)} how the stranger treated them.";
                        ev.PlayerNotification = $"{WorldBible.FactionName(faction)} has heard about {target.DisplayName}.";
                        ev.WorldChanges.Add(WorldChange.Reputation(faction, a.Type == PlayerActionType.ATTACKED_NPC ? -25 : -10));
                        ev.WorldChanges.Add(WorldChange.Tension(5));
                        if (a.Type == PlayerActionType.ATTACKED_NPC) ev.WorldChanges.Add(WorldChange.SpawnEnemies(rng.Pick(new[] { "TOWN_SQUARE", "MARKET", a.LocationId }), 1, faction));
                    }
                    else
                    {
                        ev.EventType = "RUMOR_SPREAD";
                        ev.Description = $"{target?.DisplayName ?? "Someone"} is telling people the stranger is dangerous.";
                        ev.WorldChanges.Add(WorldChange.Rumor($"The newcomer roughed up {target?.DisplayName ?? "someone"}."));
                        ev.WorldChanges.Add(WorldChange.Tension(3));
                    }
                    break;
                case PlayerActionType.FOUND_EVIDENCE:
                    ev.EventType = "NPC_RELOCATE";
                    var villain = s.GetNpc(s.Seed.VillainNpcId);
                    if (villain != null && villain.Alive && villain.CurrentLocation == a.LocationId)
                    {
                        ev.SourceNpc = villain.NpcId;
                        ev.Description = $"{villain.DisplayName} noticed the stranger searching and is getting nervous.";
                        ev.WorldChanges.Add(WorldChange.SetMood(villain.NpcId, "SUSPICIOUS"));
                        ev.WorldChanges.Add(WorldChange.Goal(villain.NpcId, "Find out what the stranger has found"));
                        ev.WorldChanges.Add(WorldChange.Tension(8));
                    }
                    else
                    {
                        ev.EventType = "QUIET";
                        ev.Description = "The search went unnoticed, for now.";
                        ev.WorldChanges.Add(WorldChange.Tension(3));
                    }
                    break;
                case PlayerActionType.BRIBED:
                    ev.EventType = "RUMOR_SPREAD";
                    ev.Description = $"Coin changed hands with {target?.DisplayName}. Coin talks.";
                    ev.WorldChanges.Add(WorldChange.Rumor($"The newcomer is paying people for answers."));
                    if (faction != WorldBible.NoFaction) ev.WorldChanges.Add(WorldChange.Reputation(faction, 5));
                    break;
                case PlayerActionType.STOLE:
                    ev.EventType = "PRICE_CHANGE"; ev.SourceFaction = "MERCHANT_CIRCLE";
                    ev.Description = "Something went missing from the market. The merchants tighten their purses.";
                    ev.PlayerNotification = "Prices at the market have gone up.";
                    ev.WorldChanges.Add(new WorldChange { Type = "PRICE_MODIFIER", Faction = "MERCHANT_CIRCLE", Change = 25 });
                    ev.WorldChanges.Add(WorldChange.Tension(5));
                    break;
                default:
                    ev.EventType = "QUIET";
                    ev.Description = "The town takes note and moves on.";
                    ev.WorldChanges.Add(WorldChange.Tension(1));
                    break;
            }
            return ev;
        }

        public static DirectorEvent Ambient(WorldState s, SeededRandom rng)
        {
            var ev = new DirectorEvent { Urgency = 3, Location = "TOWN_SQUARE" };
            var villain = s.GetNpc(s.Seed.VillainNpcId);
            int roll = rng.Next(0, 4);
            if (roll == 0 && villain != null && villain.Alive && s.WorldTension >= 50)
            {
                ev.EventType = "AMBUSH"; ev.SourceNpc = villain.NpcId; ev.SourceFaction = villain.Faction; ev.Urgency = 7;
                ev.Location = rng.Pick(new[] { "FOREST", "RUINS", "WAREHOUSE", "UNDERGROUND" });
                ev.Description = $"{villain.DisplayName} has decided the stranger is a problem worth paying to remove.";
                ev.PlayerNotification = "Someone has been asking where you sleep.";
                ev.WorldChanges.Add(WorldChange.SpawnEnemies(ev.Location, 2, villain.Faction));
                ev.WorldChanges.Add(WorldChange.Tension(5));
            }
            else if (roll == 1)
            {
                var talker = rng.Pick(s.AliveNpcs.ToList());
                ev.EventType = "RUMOR_SPREAD"; ev.SourceNpc = talker.NpcId; ev.Location = "TAVERN";
                ev.Description = $"{talker.DisplayName} has been talking about the newcomer at the tavern.";
                ev.WorldChanges.Add(WorldChange.Rumor($"{talker.DisplayName} says the newcomer asks too many questions."));
            }
            else if (roll == 2)
            {
                var mover = rng.Pick(s.AliveNpcs.ToList());
                var dest = rng.Pick(WorldBible.Locations);
                ev.EventType = "NPC_RELOCATE"; ev.SourceNpc = mover.NpcId; ev.Location = dest;
                ev.Description = $"{mover.DisplayName} has business at the {WorldBible.LocationName(dest)} for the next few hours.";
                ev.WorldChanges.Add(WorldChange.MoveNpc(mover.NpcId, dest));
            }
            else
            {
                ev.EventType = "QUIET";
                ev.Description = "A quiet hour. The town holds its breath.";
                ev.WorldChanges.Add(WorldChange.Tension(rng.Next(-2, 3)));
            }
            return ev;
        }
    }
}
