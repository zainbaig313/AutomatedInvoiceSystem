using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Bank
{
    HBL,
    ASKARI,
    ALFALAH,
    UBL,
    MCB
}
