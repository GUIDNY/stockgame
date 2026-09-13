using System;
using Newtonsoft.Json;
using Echobound.World;

namespace Echobound.Inventory
{
    /// <summary>An item instance. Type must be one of WorldBible.ItemTypes; Label is the narrative name.</summary>
    [Serializable]
    public class Item
    {
        [JsonProperty("type")] public string Type = "EVIDENCE";
        [JsonProperty("label")] public string Label = "";
        [JsonProperty("description")] public string Description = "";
        [JsonProperty("fact_id")] public string FactId = "";
        [JsonProperty("value")] public int Value = 0;

        public static Item Create(string type, string label, string description = "", string factId = "", int value = 0)
        {
            return new Item
            {
                Type = WorldBible.IsValidItem(type) ? type : "EVIDENCE",
                Label = string.IsNullOrWhiteSpace(label) ? type : label,
                Description = description ?? "",
                FactId = factId ?? "",
                Value = value
            };
        }
    }
}
