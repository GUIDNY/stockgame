using System;
using Echobound.World;
using Echobound.Quests;

namespace Echobound.Core
{
    /// <summary>
    /// Global event bus. Systems communicate through these events instead of referencing each other directly.
    /// Everything here is pure C# so it can be raised from the simulation core and observed by Unity components.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>The player performed a recorded action (talk, threaten, search, attack...).</summary>
        public static event Action<PlayerAction> PlayerActed;
        /// <summary>An NPC died. Argument: npc id, killer id ("PLAYER", faction id, or "UNKNOWN").</summary>
        public static event Action<string, string> NpcDied;
        /// <summary>Faction reputation changed. Args: faction id, new value, delta.</summary>
        public static event Action<string, int, int> ReputationChanged;
        /// <summary>A quest was added, updated, mutated, completed or failed.</summary>
        public static event Action<Quest> QuestChanged;
        /// <summary>World time advanced. Argument: total elapsed minutes.</summary>
        public static event Action<int> TimeAdvanced;
        /// <summary>A new game hour started. Argument: hour of day (0-23).</summary>
        public static event Action<int> HourStarted;
        /// <summary>A director event was applied to the world.</summary>
        public static event Action<DirectorEvent> DirectorEventApplied;
        /// <summary>A fact became known to an NPC. Args: fact id, npc id.</summary>
        public static event Action<string, string> FactLearned;
        /// <summary>The player discovered a piece of world information (shown in the World tab).</summary>
        public static event Action<string> WorldInfoDiscovered;
        /// <summary>A short message to show the player on the HUD.</summary>
        public static event Action<string> Notification;
        /// <summary>An NPC's simulated location changed. Args: npc id, location id.</summary>
        public static event Action<string, string> NpcLocationChanged;
        /// <summary>The director wants enemies spawned. Args: location id, count, faction id.</summary>
        public static event Action<string, int, string> SpawnEnemiesRequested;
        /// <summary>The director wants an item placed in a search spot. Args: location id, item type, label.</summary>
        public static event Action<string, string, string> SpawnItemRequested;
        /// <summary>A quest reached its decision point and needs a resolution choice from the player.</summary>
        public static event Action<Quest> QuestDecisionReady;

        public static void RaisePlayerActed(PlayerAction a) => PlayerActed?.Invoke(a);
        public static void RaiseNpcDied(string npcId, string killer) => NpcDied?.Invoke(npcId, killer);
        public static void RaiseReputationChanged(string f, int v, int d) => ReputationChanged?.Invoke(f, v, d);
        public static void RaiseQuestChanged(Quest q) => QuestChanged?.Invoke(q);
        public static void RaiseTimeAdvanced(int minutes) => TimeAdvanced?.Invoke(minutes);
        public static void RaiseHourStarted(int hour) => HourStarted?.Invoke(hour);
        public static void RaiseDirectorEventApplied(DirectorEvent e) => DirectorEventApplied?.Invoke(e);
        public static void RaiseFactLearned(string factId, string npcId) => FactLearned?.Invoke(factId, npcId);
        public static void RaiseWorldInfoDiscovered(string s) => WorldInfoDiscovered?.Invoke(s);
        public static void RaiseNotification(string s) => Notification?.Invoke(s);
        public static void RaiseNpcLocationChanged(string npc, string loc) => NpcLocationChanged?.Invoke(npc, loc);
        public static void RaiseSpawnEnemies(string loc, int count, string faction) => SpawnEnemiesRequested?.Invoke(loc, count, faction);
        public static void RaiseSpawnItem(string loc, string itemType, string label) => SpawnItemRequested?.Invoke(loc, itemType, label);
        public static void RaiseQuestDecisionReady(Quest q) => QuestDecisionReady?.Invoke(q);

        /// <summary>Clears all subscribers. Used when a new world is created or the game is torn down.</summary>
        public static void ClearAll()
        {
            PlayerActed = null; NpcDied = null; ReputationChanged = null; QuestChanged = null;
            TimeAdvanced = null; HourStarted = null; DirectorEventApplied = null; FactLearned = null;
            WorldInfoDiscovered = null; Notification = null; NpcLocationChanged = null;
            SpawnEnemiesRequested = null; SpawnItemRequested = null; QuestDecisionReady = null;
        }
    }
}
