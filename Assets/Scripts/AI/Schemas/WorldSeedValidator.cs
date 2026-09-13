using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Echobound.World;

namespace Echobound.AI.Schemas
{
    /// <summary>
    /// Validates a complete generated world. Strict where it matters (ids must exist, every NPC slot filled,
    /// a villain and an opening quest must exist) and forgiving on prose.
    /// </summary>
    public class WorldSeedValidator : IResponseValidator<WorldSeed>
    {
        public string SchemaName => "WorldSeed";
        public string JsonSchema => null; // Too large for constrained decoding; the prompt carries the contract.

        public ValidationResult<WorldSeed> Validate(string rawText)
        {
            var o = JsonHelper.ExtractObject(rawText, out var err);
            if (o == null) return ValidationResult<WorldSeed>.Failure(err);
            var errors = new List<string>();
            var corrections = new List<string>();
            var seed = new WorldSeed
            {
                WorldId = JsonHelper.Str(o, "world_id"),
                Title = JsonHelper.Str(o, "title"),
                IntroText = JsonHelper.Str(o, "intro_text"),
                MainConflict = JsonHelper.Str(o, "main_conflict"),
                CentralMystery = JsonHelper.Str(o, "central_mystery"),
                VillainNpcId = JsonHelper.Str(o, "villain_npc_id").ToUpperInvariant(),
                VictimNpcId = JsonHelper.Str(o, "victim_npc_id").ToUpperInvariant(),
                WorldTension = JsonHelper.Clamp(JsonHelper.Int(o, "world_tension", 20), 0, 100)
            };
            if (string.IsNullOrWhiteSpace(seed.MainConflict)) errors.Add("main_conflict missing");
            if (string.IsNullOrWhiteSpace(seed.CentralMystery)) errors.Add("central_mystery missing");
            if (string.IsNullOrWhiteSpace(seed.IntroText)) errors.Add("intro_text missing");
            if (!WorldBible.IsValidNpc(seed.VillainNpcId))
            {
                var v = WorldBible.TryCorrect(seed.VillainNpcId, WorldBible.NpcIds);
                if (v == null) errors.Add("villain_npc_id invalid: " + seed.VillainNpcId); else seed.VillainNpcId = v;
            }
            if (!string.IsNullOrEmpty(seed.VictimNpcId) && !WorldBible.IsValidNpc(seed.VictimNpcId))
                seed.VictimNpcId = WorldBible.TryCorrect(seed.VictimNpcId, WorldBible.NpcIds) ?? "";

            // Factions
            foreach (var t in JsonHelper.Arr(o, "factions"))
            {
                var f = t as JObject; if (f == null) continue;
                var fs = new FactionSeed
                {
                    Id = JsonHelper.Str(f, "id").ToUpperInvariant(),
                    Stance = JsonHelper.Str(f, "stance", "NEUTRAL").ToUpperInvariant(),
                    Goal = JsonHelper.Str(f, "goal"),
                    PublicFace = JsonHelper.Str(f, "public_face"),
                    InitialPlayerReputation = JsonHelper.Clamp(JsonHelper.Int(f, "initial_player_reputation"), -50, 50)
                };
                if (!WorldBible.IsValidFaction(fs.Id) || fs.Id == WorldBible.NoFaction)
                {
                    var id = WorldBible.TryCorrect(fs.Id, WorldBible.FactionIds);
                    if (id == null) { errors.Add("invalid faction id " + fs.Id); continue; }
                    fs.Id = id;
                }
                if (System.Array.IndexOf(WorldBible.FactionStances, fs.Stance) < 0) fs.Stance = "NEUTRAL";
                foreach (var a in JsonHelper.Arr(f, "allied_with")) { var id = WorldBible.TryCorrect(a.ToString(), WorldBible.FactionIds); if (id != null && id != fs.Id) fs.AlliedWith.Add(id); }
                foreach (var h in JsonHelper.Arr(f, "hostile_to")) { var id = WorldBible.TryCorrect(h.ToString(), WorldBible.FactionIds); if (id != null && id != fs.Id && !fs.AlliedWith.Contains(id)) fs.HostileTo.Add(id); }
                if (seed.Factions.Any(x => x.Id == fs.Id)) continue;
                seed.Factions.Add(fs);
            }
            foreach (var fid in WorldBible.FactionIds)
                if (seed.Factions.All(f => f.Id != fid)) { seed.Factions.Add(new FactionSeed { Id = fid, Goal = "Survive and keep influence." }); corrections.Add("added missing faction " + fid); }

            // NPCs: every slot must be filled exactly once.
            foreach (var t in JsonHelper.Arr(o, "npc_states"))
            {
                var n = t as JObject; if (n == null) continue;
                var ns = new NpcSeed
                {
                    NpcId = JsonHelper.Str(n, "npc_id").ToUpperInvariant(),
                    DisplayName = JsonHelper.Str(n, "display_name"),
                    Role = JsonHelper.Str(n, "role").ToUpperInvariant(),
                    Faction = JsonHelper.Str(n, "faction", "NONE").ToUpperInvariant(),
                    Personality = JsonHelper.Str(n, "personality"),
                    SpeechStyle = JsonHelper.Str(n, "speech_style"),
                    PublicGoal = JsonHelper.Str(n, "public_goal"),
                    Secret = JsonHelper.Str(n, "secret"),
                    SecretIsCrime = JsonHelper.Bool(n, "secret_is_crime"),
                    Mood = JsonHelper.Str(n, "mood", "CALM").ToUpperInvariant(),
                    Alive = JsonHelper.Bool(n, "alive", true),
                    InitialLocation = JsonHelper.Str(n, "initial_location").ToUpperInvariant(),
                    InitialPlayerRelationship = JsonHelper.Clamp(JsonHelper.Int(n, "initial_player_relationship"), -40, 40)
                };
                if (!WorldBible.IsValidNpc(ns.NpcId)) { var id = WorldBible.TryCorrect(ns.NpcId, WorldBible.NpcIds); if (id == null) { errors.Add("invalid npc_id " + ns.NpcId); continue; } ns.NpcId = id; }
                if (seed.NpcStates.Any(x => x.NpcId == ns.NpcId)) { corrections.Add("duplicate npc " + ns.NpcId + " dropped"); continue; }
                ns.Role = WorldBible.RoleOf(ns.NpcId); // role is fixed per slot
                if (!WorldBible.IsValidFaction(ns.Faction)) { ns.Faction = WorldBible.TryCorrect(ns.Faction, WorldBible.FactionIds) ?? "NONE"; }
                if (!WorldBible.IsValidMood(ns.Mood)) ns.Mood = "CALM";
                if (!string.IsNullOrEmpty(ns.InitialLocation) && !WorldBible.IsValidLocation(ns.InitialLocation)) ns.InitialLocation = WorldBible.TryCorrect(ns.InitialLocation, WorldBible.Locations) ?? "";
                if (string.IsNullOrWhiteSpace(ns.DisplayName)) { ns.DisplayName = "Unnamed " + ns.Role.ToLowerInvariant(); corrections.Add("named " + ns.NpcId); }
                foreach (var k in JsonHelper.Arr(n, "knowledge")) ns.Knowledge.Add(k.ToString());
                foreach (var i in JsonHelper.Arr(n, "important_inventory")) { var it = WorldBible.TryCorrect(i.ToString(), WorldBible.ItemTypes); if (it != null) ns.ImportantInventory.Add(it); }
                seed.NpcStates.Add(ns);
            }
            foreach (var slot in WorldBible.NpcIds)
                if (seed.NpcStates.All(n => n.NpcId != slot)) errors.Add("missing npc slot " + slot);

            // Relationships
            foreach (var t in JsonHelper.Arr(o, "relationships"))
            {
                var r = t as JObject; if (r == null) continue;
                var rs = new RelationshipSeed
                {
                    A = WorldBible.TryCorrect(JsonHelper.Str(r, "a"), WorldBible.NpcIds),
                    B = WorldBible.TryCorrect(JsonHelper.Str(r, "b"), WorldBible.NpcIds),
                    Type = JsonHelper.Str(r, "type", "ALLY").ToUpperInvariant(),
                    Summary = JsonHelper.Str(r, "summary"),
                    IsSecret = JsonHelper.Bool(r, "is_secret")
                };
                if (rs.A == null || rs.B == null || rs.A == rs.B) { corrections.Add("dropped relationship with invalid npcs"); continue; }
                if (!WorldBible.IsValidRelationshipType(rs.Type)) rs.Type = WorldBible.TryCorrect(rs.Type, WorldBible.RelationshipTypes) ?? "ALLY";
                seed.Relationships.Add(rs);
            }

            // Hidden truths
            int truthIndex = 1;
            foreach (var t in JsonHelper.Arr(o, "hidden_truths"))
            {
                var h = t as JObject; if (h == null) continue;
                var ht = new HiddenTruth
                {
                    Id = JsonHelper.Str(h, "id"),
                    Summary = JsonHelper.Str(h, "summary"),
                    EvidenceLocation = JsonHelper.Str(h, "evidence_location").ToUpperInvariant(),
                    EvidenceItem = JsonHelper.Str(h, "evidence_item", "EVIDENCE").ToUpperInvariant(),
                    EvidenceLabel = JsonHelper.Str(h, "evidence_label"),
                    IsCore = JsonHelper.Bool(h, "is_core")
                };
                if (string.IsNullOrWhiteSpace(ht.Summary)) continue;
                if (string.IsNullOrWhiteSpace(ht.Id) || seed.HiddenTruths.Any(x => x.Id == ht.Id)) ht.Id = "TRUTH_" + (truthIndex).ToString("00");
                truthIndex++;
                if (!WorldBible.IsValidLocation(ht.EvidenceLocation)) ht.EvidenceLocation = WorldBible.TryCorrect(ht.EvidenceLocation, WorldBible.Locations) ?? "WAREHOUSE";
                if (!WorldBible.IsValidItem(ht.EvidenceItem)) ht.EvidenceItem = WorldBible.TryCorrect(ht.EvidenceItem, WorldBible.ItemTypes) ?? "EVIDENCE";
                if (string.IsNullOrWhiteSpace(ht.EvidenceLabel)) ht.EvidenceLabel = "Evidence: " + Shorten(ht.Summary, 40);
                foreach (var k in JsonHelper.Arr(h, "known_by")) { var id = WorldBible.TryCorrect(k.ToString(), WorldBible.NpcIds); if (id != null) ht.KnownBy.Add(id); }
                seed.HiddenTruths.Add(ht);
            }
            if (!seed.HiddenTruths.Any(h => h.IsCore))
            {
                if (seed.HiddenTruths.Count > 0) { seed.HiddenTruths[0].IsCore = true; corrections.Add("first hidden truth marked core"); }
                else errors.Add("no hidden truths");
            }

            // Quests
            if (o["opening_quest"] is JObject oq)
            {
                var qErrors = new List<string>();
                seed.OpeningQuest = QuestSchema.ParseQuest(oq, qErrors, corrections);
                if (seed.OpeningQuest == null) errors.Add("opening_quest invalid: " + string.Join("; ", qErrors));
                else seed.OpeningQuest.IsMain = true;
            }
            else errors.Add("opening_quest missing");
            if (o["opening_opportunity"] is JObject oo)
            {
                var qErrors = new List<string>();
                seed.OpeningOpportunity = QuestSchema.ParseQuest(oo, qErrors, corrections);
                if (seed.OpeningOpportunity == null) corrections.Add("opening_opportunity dropped: " + string.Join("; ", qErrors));
            }

            foreach (var t in JsonHelper.Arr(o, "possible_endings"))
            {
                var e = t as JObject; if (e == null) continue;
                var es = new EndingSeed { Id = JsonHelper.Str(e, "id"), Summary = JsonHelper.Str(e, "summary"), ConditionHint = JsonHelper.Str(e, "condition_hint") };
                if (string.IsNullOrWhiteSpace(es.Summary)) continue;
                if (string.IsNullOrWhiteSpace(es.Id)) es.Id = "ENDING_" + (seed.PossibleEndings.Count + 1);
                seed.PossibleEndings.Add(es);
            }
            if (seed.PossibleEndings.Count < 2) errors.Add("need at least 2 possible endings");

            if (o["player_reputation"] is JObject pr)
                foreach (var p in pr.Properties())
                {
                    var f = WorldBible.TryCorrect(p.Name, WorldBible.FactionIds);
                    if (f != null && int.TryParse(p.Value.ToString(), out var v)) seed.PlayerReputation[f] = JsonHelper.Clamp(v, -50, 50);
                }

            if (errors.Count > 0) return new ValidationResult<WorldSeed> { Ok = false, Errors = errors, Corrections = corrections };
            return ValidationResult<WorldSeed>.Success(seed, corrections);
        }

        private static string Shorten(string s, int max) => s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }
}
