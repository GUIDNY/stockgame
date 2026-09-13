using System;
using System.Collections.Generic;
using System.Linq;

namespace Echobound.World
{
    /// <summary>
    /// The World Bible: the closed set of everything that physically exists in the game.
    /// The AI may only reference identifiers listed here. Validators reject anything else.
    /// STATIC content lives here; DYNAMIC narrative state lives in WorldSeed / WorldState.
    /// </summary>
    public static class WorldBible
    {
        public const string PlayerId = "PLAYER";
        public const string NoFaction = "NONE";

        // ---------------- Locations ----------------
        public static readonly string[] Locations =
        {
            "TOWN_SQUARE", "TAVERN", "GUARD_STATION", "MARKET", "RESIDENTIAL",
            "WAREHOUSE", "FOREST", "RUINS", "UNDERGROUND", "FACTION_BASE"
        };

        public static readonly Dictionary<string, string> LocationNames = new Dictionary<string, string>
        {
            {"TOWN_SQUARE", "Town Square"}, {"TAVERN", "The Lantern Tavern"}, {"GUARD_STATION", "Guard Station"},
            {"MARKET", "Market"}, {"RESIDENTIAL", "Residential Quarter"}, {"WAREHOUSE", "Abandoned Warehouse"},
            {"FOREST", "Blackpine Forest"}, {"RUINS", "Old Ruins"}, {"UNDERGROUND", "Underground Tunnels"},
            {"FACTION_BASE", "Faction Headquarters"}
        };

        // ---------------- NPCs (fixed slots, dynamic personalities) ----------------
        public static readonly string[] NpcIds =
        {
            "NPC_01", "NPC_02", "NPC_03", "NPC_04", "NPC_05", "NPC_06", "NPC_07", "NPC_08", "NPC_09", "NPC_10"
        };

        public static readonly string[] NpcRoles =
        {
            "MAYOR", "GUARD_COMMANDER", "TAVERN_OWNER", "MERCHANT", "DOCTOR",
            "CRIMINAL", "SMUGGLER", "PRIEST", "STRANGER", "FACTION_LEADER"
        };

        /// <summary>Slot -> role. The role is fixed per slot so the town always has the same social shape.</summary>
        public static readonly Dictionary<string, string> NpcRoleBySlot = new Dictionary<string, string>
        {
            {"NPC_01", "MAYOR"}, {"NPC_02", "GUARD_COMMANDER"}, {"NPC_03", "TAVERN_OWNER"}, {"NPC_04", "MERCHANT"},
            {"NPC_05", "DOCTOR"}, {"NPC_06", "CRIMINAL"}, {"NPC_07", "SMUGGLER"}, {"NPC_08", "PRIEST"},
            {"NPC_09", "STRANGER"}, {"NPC_10", "FACTION_LEADER"}
        };

        /// <summary>Workplace per role. Used for default schedules and for "where would this person be".</summary>
        public static readonly Dictionary<string, string> RoleWorkplace = new Dictionary<string, string>
        {
            {"MAYOR", "TOWN_SQUARE"}, {"GUARD_COMMANDER", "GUARD_STATION"}, {"TAVERN_OWNER", "TAVERN"},
            {"MERCHANT", "MARKET"}, {"DOCTOR", "RESIDENTIAL"}, {"CRIMINAL", "UNDERGROUND"},
            {"SMUGGLER", "WAREHOUSE"}, {"PRIEST", "RUINS"}, {"STRANGER", "TAVERN"}, {"FACTION_LEADER", "FACTION_BASE"}
        };

        public static readonly Dictionary<string, string> RoleHome = new Dictionary<string, string>
        {
            {"MAYOR", "RESIDENTIAL"}, {"GUARD_COMMANDER", "GUARD_STATION"}, {"TAVERN_OWNER", "TAVERN"},
            {"MERCHANT", "RESIDENTIAL"}, {"DOCTOR", "RESIDENTIAL"}, {"CRIMINAL", "UNDERGROUND"},
            {"SMUGGLER", "WAREHOUSE"}, {"PRIEST", "RUINS"}, {"STRANGER", "FOREST"}, {"FACTION_LEADER", "FACTION_BASE"}
        };

        /// <summary>Default daily schedule per role: hour -> location. Entries are sorted by hour.</summary>
        public static readonly Dictionary<string, (int hour, string location)[]> RoleSchedules =
            new Dictionary<string, (int, string)[]>
            {
                {"MAYOR", new[]{(8,"TOWN_SQUARE"),(13,"TAVERN"),(15,"TOWN_SQUARE"),(20,"RESIDENTIAL")}},
                {"GUARD_COMMANDER", new[]{(7,"GUARD_STATION"),(12,"TOWN_SQUARE"),(14,"GUARD_STATION"),(22,"GUARD_STATION")}},
                {"TAVERN_OWNER", new[]{(9,"MARKET"),(11,"TAVERN"),(23,"TAVERN")}},
                {"MERCHANT", new[]{(8,"MARKET"),(19,"TAVERN"),(22,"RESIDENTIAL")}},
                {"DOCTOR", new[]{(8,"RESIDENTIAL"),(12,"MARKET"),(14,"RESIDENTIAL"),(21,"TAVERN"),(23,"RESIDENTIAL")}},
                {"CRIMINAL", new[]{(10,"MARKET"),(16,"TAVERN"),(20,"UNDERGROUND")}},
                {"SMUGGLER", new[]{(9,"WAREHOUSE"),(17,"FOREST"),(21,"TAVERN"),(1,"WAREHOUSE")}},
                {"PRIEST", new[]{(6,"RUINS"),(11,"TOWN_SQUARE"),(16,"RESIDENTIAL"),(19,"RUINS")}},
                {"STRANGER", new[]{(10,"FOREST"),(18,"TAVERN"),(2,"RUINS")}},
                {"FACTION_LEADER", new[]{(9,"FACTION_BASE"),(15,"TOWN_SQUARE"),(18,"FACTION_BASE")}}
            };

        // ---------------- Factions ----------------
        public static readonly string[] FactionIds = { "TOWN_GUARD", "IRON_HAND", "MERCHANT_CIRCLE" };
        public static readonly Dictionary<string, string> FactionNames = new Dictionary<string, string>
        {
            {"TOWN_GUARD", "The Town Guard"}, {"IRON_HAND", "The Iron Hand"}, {"MERCHANT_CIRCLE", "The Merchant Circle"}
        };
        public static readonly string[] FactionStances = { "LAWFUL", "CRIMINAL", "NEUTRAL" };

        // ---------------- Items / enemies ----------------
        public static readonly string[] ItemTypes =
        {
            "LETTER", "EVIDENCE", "KEY", "MEDICINE", "WEAPON", "COIN_PURSE", "CONTRABAND", "LEDGER"
        };
        public static readonly string[] EnemyTypes = { "THUG" };

        // ---------------- Quest vocabulary ----------------
        public static readonly string[] QuestActions =
        {
            "TALK", "INVESTIGATE", "STEAL", "DELIVER", "FOLLOW", "FIGHT", "ESCAPE", "PROTECT", "SEARCH", "BRIBE", "THREATEN"
        };

        public static readonly string[] QuestStates = { "ACTIVE", "COMPLETED", "FAILED", "MUTATED" };

        // ---------------- Director vocabulary ----------------
        public static readonly string[] EventTypes =
        {
            "FACTION_RETALIATION", "RUMOR_SPREAD", "NPC_RELOCATE", "NPC_DEATH", "ITEM_PLANTED",
            "QUEST_OFFER", "AMBUSH", "ARREST_ATTEMPT", "REVELATION", "PRICE_CHANGE", "QUIET"
        };

        public static readonly string[] WorldChangeTypes =
        {
            "REPUTATION", "NPC_RELATIONSHIP", "NPC_MOOD", "NPC_LOCATION", "NPC_KNOWLEDGE", "NPC_GOAL",
            "SPAWN_ENEMIES", "SPAWN_ITEM", "TENSION", "RUMOR", "LOCK_LOCATION", "UNLOCK_LOCATION",
            "PRICE_MODIFIER", "NPC_DEATH", "NPC_HOSTILE"
        };

        public static readonly string[] Moods =
        {
            "CALM", "NERVOUS", "ANGRY", "AFRAID", "FRIENDLY", "SUSPICIOUS", "GRIEVING", "HOPEFUL"
        };

        public static readonly string[] RelationshipTypes =
        {
            "ALLY", "RIVAL", "LOVER", "DEBTOR", "BLACKMAIL", "FAMILY", "ENEMY", "EMPLOYER"
        };

        /// <summary>Dialogue intents the player can express. Deterministic mechanical effects are attached in DialogueManager.</summary>
        public static readonly string[] DialogueIntents =
        {
            "GREET", "ASK_TOPIC", "ASK_RUMORS", "THREATEN", "BRIBE", "ACCUSE", "GIVE_ITEM", "OFFER_HELP", "LIE", "LEAVE", "FREE_TEXT"
        };

        /// <summary>Interactive object kinds that the greybox builder places and the AI may reference.</summary>
        public static readonly string[] InteractiveObjects = { "SEARCH_SPOT", "DOOR", "NOTICE_BOARD", "BED" };

        /// <summary>Gameplay mechanics exposed to the AI (documentation only; the AI cannot invent new ones).</summary>
        public static readonly string[] Mechanics =
        {
            "WALK", "RUN", "INTERACT", "MELEE_ATTACK", "TAKE_DAMAGE", "PICK_UP_ITEM", "PAY_COINS",
            "TALK", "SEARCH_SPOT", "WAIT_TIME", "SLEEP"
        };

        // ---------------- Validation helpers ----------------
        public static bool IsValidLocation(string id) => id != null && Array.IndexOf(Locations, id) >= 0;
        public static bool IsValidNpc(string id) => id != null && Array.IndexOf(NpcIds, id) >= 0;
        public static bool IsValidFaction(string id) => id != null && (id == NoFaction || Array.IndexOf(FactionIds, id) >= 0);
        public static bool IsValidItem(string id) => id != null && Array.IndexOf(ItemTypes, id) >= 0;
        public static bool IsValidQuestAction(string id) => id != null && Array.IndexOf(QuestActions, id) >= 0;
        public static bool IsValidEventType(string id) => id != null && Array.IndexOf(EventTypes, id) >= 0;
        public static bool IsValidWorldChange(string id) => id != null && Array.IndexOf(WorldChangeTypes, id) >= 0;
        public static bool IsValidMood(string id) => id != null && Array.IndexOf(Moods, id) >= 0;
        public static bool IsValidRelationshipType(string id) => id != null && Array.IndexOf(RelationshipTypes, id) >= 0;
        public static bool IsValidIntent(string id) => id != null && Array.IndexOf(DialogueIntents, id) >= 0;

        public static string LocationName(string id) => id != null && LocationNames.TryGetValue(id, out var n) ? n : id;
        public static string FactionName(string id) => id != null && FactionNames.TryGetValue(id, out var n) ? n : id;
        public static string RoleOf(string npcId) => npcId != null && NpcRoleBySlot.TryGetValue(npcId, out var r) ? r : "STRANGER";

        /// <summary>
        /// Attempts to map an invalid identifier to a valid one from the given set (case, spaces, near matches).
        /// Returns null when no confident match exists.
        /// </summary>
        public static string TryCorrect(string value, IEnumerable<string> valid)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string norm = value.Trim().ToUpperInvariant().Replace(' ', '_').Replace('-', '_');
            var validList = valid.ToList();
            foreach (var v in validList) if (v == norm) return v;
            foreach (var v in validList) if (norm.Contains(v) || v.Contains(norm)) return v;
            return null;
        }

        /// <summary>Compact text description of the bible used inside AI prompts.</summary>
        public static string ToPromptText()
        {
            return
                "VALID_LOCATIONS: " + string.Join(", ", Locations) + "\n" +
                "VALID_NPCS (slot=role): " + string.Join(", ", NpcIds.Select(i => i + "=" + RoleOf(i))) + "\n" +
                "VALID_FACTIONS: " + string.Join(", ", FactionIds) + " (or NONE)\n" +
                "VALID_FACTION_STANCES: " + string.Join(", ", FactionStances) + "\n" +
                "VALID_ITEM_TYPES: " + string.Join(", ", ItemTypes) + "\n" +
                "VALID_ENEMY_TYPES: " + string.Join(", ", EnemyTypes) + "\n" +
                "VALID_QUEST_ACTIONS: " + string.Join(", ", QuestActions) + "\n" +
                "VALID_EVENT_TYPES: " + string.Join(", ", EventTypes) + "\n" +
                "VALID_WORLD_CHANGE_TYPES: " + string.Join(", ", WorldChangeTypes) + "\n" +
                "VALID_MOODS: " + string.Join(", ", Moods) + "\n" +
                "VALID_RELATIONSHIP_TYPES: " + string.Join(", ", RelationshipTypes) + "\n" +
                "VALID_MECHANICS: " + string.Join(", ", Mechanics) + "\n" +
                "RULE: Use ONLY these identifiers. Do not invent locations, NPC ids, factions, items or mechanics.";
        }
    }
}
