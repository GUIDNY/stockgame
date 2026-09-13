using System.Collections.Generic;

namespace Echobound.AI.Schemas
{
    public class ValidationResult<T>
    {
        public bool Ok;
        public T Value;
        public List<string> Errors = new List<string>();
        public List<string> Corrections = new List<string>();

        public static ValidationResult<T> Success(T value, List<string> corrections = null) =>
            new ValidationResult<T> { Ok = true, Value = value, Corrections = corrections ?? new List<string>() };

        public static ValidationResult<T> Failure(string error) =>
            new ValidationResult<T> { Ok = false, Errors = new List<string> { error } };

        public string ErrorText => string.Join("; ", Errors);
    }

    /// <summary>Validates raw model text into a typed object, applying automatic corrections when safe.</summary>
    public interface IResponseValidator<T>
    {
        string SchemaName { get; }
        /// <summary>JSON schema string for providers that support constrained decoding; may be null.</summary>
        string JsonSchema { get; }
        ValidationResult<T> Validate(string rawText);
    }
}
