using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected
}
