using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InvoiceStatus
{
    Pending,
    ProofSubmitted,
    Paid,
    Rejected
}
