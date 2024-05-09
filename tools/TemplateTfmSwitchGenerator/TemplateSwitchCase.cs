using System.Text.Json.Serialization;

namespace TemplateTfmSwitchGenerator;

public record TemplateSwitchCase([property: JsonPropertyName("condition")]string Condition, [property: JsonPropertyName("value")] string Value);
