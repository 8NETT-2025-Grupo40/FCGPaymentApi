using System.Text.Json;
using System.Text.Json.Serialization;
using Fcg.Payment.API.Models.Enums;

namespace Fcg.Payment.API.Models;

public class ApiStructuredLog()
{
    [JsonIgnore] public DateTime RequestStart { get; } = DateTime.Now;

    [JsonIgnore]
    public FcgLogLevel FcgLogLevel { set => this.LogLevel = value.ToString(); }

    [JsonPropertyName("Response status code")]
    public string ResponseStatusCode { get; set; } = string.Empty;

    [JsonPropertyName("Log level")]
    public string LogLevel { get; set; } = string.Empty;

    [JsonPropertyName("Request start time")]
    public string RequestStartTime { get; set; } = string.Empty;

    [JsonPropertyName("Request end time")]
    public string RequestEndTime { get; set; } = string.Empty;

    [JsonPropertyName("Elapsed milliseconds to process request")]
    public string ElapsedTime { get; set; } = string.Empty;

    [JsonPropertyName("Information message")]
    public string InformationMessage { get; set; } = string.Empty;

    [JsonPropertyName("Error message")]
    public string ErrorMessage { get; set; } = string.Empty;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    public override string ToString() =>
        JsonSerializer.Serialize(this, this._jsonSerializerOptions);
}