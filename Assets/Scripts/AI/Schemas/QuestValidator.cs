using System.Collections.Generic;
using Echobound.Quests;

namespace Echobound.AI.Schemas
{
    public class QuestValidator : IResponseValidator<Quest>
    {
        public string SchemaName => "Quest";
        public string JsonSchema => QuestSchema.JsonSchemaText;

        public ValidationResult<Quest> Validate(string rawText)
        {
            var o = JsonHelper.ExtractObject(rawText, out var err);
            if (o == null) return ValidationResult<Quest>.Failure(err);
            var errors = new List<string>();
            var corrections = new List<string>();
            var quest = QuestSchema.ParseQuest(o, errors, corrections);
            if (quest == null) return new ValidationResult<Quest> { Ok = false, Errors = errors, Corrections = corrections };
            // Objectives that were dropped are tolerated as long as at least one remains; hard errors only when nothing usable.
            return ValidationResult<Quest>.Success(quest, corrections);
        }
    }

    /// <summary>Validates a "quest mutation narration": new title/descriptions for an already mutated quest.</summary>
    public class QuestMutationValidator : IResponseValidator<QuestMutationText>
    {
        public string SchemaName => "QuestMutationText";
        public string JsonSchema => @"{""type"":""object"",""additionalProperties"":false,""properties"":{""title"":{""type"":""string""},""narrative_reason"":{""type"":""string""},""objective_descriptions"":{""type"":""array"",""items"":{""type"":""string""}},""player_notification"":{""type"":""string""}},""required"":[""title"",""narrative_reason"",""objective_descriptions"",""player_notification""]}";

        public ValidationResult<QuestMutationText> Validate(string rawText)
        {
            var o = JsonHelper.ExtractObject(rawText, out var err);
            if (o == null) return ValidationResult<QuestMutationText>.Failure(err);
            var m = new QuestMutationText
            {
                Title = JsonHelper.Str(o, "title").Trim(),
                NarrativeReason = JsonHelper.Str(o, "narrative_reason").Trim(),
                PlayerNotification = JsonHelper.Str(o, "player_notification").Trim()
            };
            foreach (var t in JsonHelper.Arr(o, "objective_descriptions")) m.ObjectiveDescriptions.Add(t.ToString().Trim());
            if (string.IsNullOrEmpty(m.Title) && m.ObjectiveDescriptions.Count == 0) return ValidationResult<QuestMutationText>.Failure("nothing usable in mutation text");
            return ValidationResult<QuestMutationText>.Success(m);
        }
    }

    public class QuestMutationText
    {
        public string Title = "";
        public string NarrativeReason = "";
        public List<string> ObjectiveDescriptions = new List<string>();
        public string PlayerNotification = "";
    }
}
