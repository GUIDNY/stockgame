using System;
using System.IO;
using Newtonsoft.Json;
using Echobound.Core;
using Echobound.World;

namespace Echobound.SaveSystem
{
    [Serializable]
    public class SaveData
    {
        [JsonProperty("version")] public int Version = 1;
        [JsonProperty("saved_utc")] public string SavedUtc = "";
        [JsonProperty("world")] public WorldState World;
        [JsonProperty("opportunity_delivered")] public bool OpportunityDelivered;
    }

    /// <summary>
    /// Saves and loads the whole playthrough: seed, world state, NPC memory, relationships, factions, quests,
    /// player, time. Loading reconstructs the story instead of regenerating it.
    /// </summary>
    public static class SaveManager
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public static string Serialize(WorldState state, bool opportunityDelivered)
        {
            var data = new SaveData { World = state, SavedUtc = DateTime.UtcNow.ToString("o"), OpportunityDelivered = opportunityDelivered };
            return JsonConvert.SerializeObject(data, Settings);
        }

        public static SaveData Deserialize(string json)
        {
            var data = JsonConvert.DeserializeObject<SaveData>(json, Settings);
            if (data?.World == null) throw new InvalidDataException("Save file has no world state");
            return data;
        }

        public static bool Save(string path, WorldState state, bool opportunityDelivered)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                File.WriteAllText(path, Serialize(state, opportunityDelivered));
                GameLog.Info("Saved to " + path);
                return true;
            }
            catch (Exception e) { GameLog.Error("Save failed: " + e.Message); return false; }
        }

        public static SaveData Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                return Deserialize(File.ReadAllText(path));
            }
            catch (Exception e) { GameLog.Error("Load failed: " + e.Message); return null; }
        }

        public static bool Exists(string path) => File.Exists(path);
    }
}
