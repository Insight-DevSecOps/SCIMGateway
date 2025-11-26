// ==========================================================================
// T088: TransformationExample Model
// ==========================================================================
// Model for example inputs/outputs used for testing transformation rules
// ==========================================================================

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SCIMGateway.Core.Models.Transformations;

/// <summary>
/// Example input/output for testing transformation rules.
/// </summary>
public class TransformationExample
{
    /// <summary>
    /// Example input (SCIM Group displayName).
    /// </summary>
    [JsonPropertyName("input")]
    [JsonProperty("input")]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Expected output (entitlement name or ID).
    /// Null means no match expected.
    /// </summary>
    [JsonPropertyName("expectedOutput")]
    [JsonProperty("expectedOutput")]
    public string? ExpectedOutput { get; set; }

    /// <summary>
    /// Whether this example passed last test.
    /// Null if never tested.
    /// </summary>
    [JsonPropertyName("passed")]
    [JsonProperty("passed")]
    public bool? Passed { get; set; }

    /// <summary>
    /// Description of what this example tests.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonProperty("description")]
    public string? Description { get; set; }
}
