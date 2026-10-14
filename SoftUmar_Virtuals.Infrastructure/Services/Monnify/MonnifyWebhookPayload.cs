using System.Text.Json.Serialization;

namespace SoftUmar_Virtuals.Infrastructure.Services.Monnify
{
    // Shape of what Monnify POSTs to your webhook URL. Based on their
    // published docs for reserved-account transfer notifications - double
    // check against your actual sandbox test payload once you get one, since
    // webhook shapes occasionally get extra fields added over time.
    public class MonnifyWebhookPayload
    {
        [JsonPropertyName("eventType")]
        public string? EventType { get; set; } // e.g. "SUCCESSFUL_TRANSACTION"

        [JsonPropertyName("eventData")]
        public MonnifyWebhookEventData? EventData { get; set; }
    }

    public class MonnifyWebhookEventData
    {
        [JsonPropertyName("transactionReference")]
        public required string TransactionReference { get; set; } // Monnify's own
                                                                  // unique ID for this transaction - THIS is what you check for
                                                                  // idempotency, not paymentReference

        [JsonPropertyName("paymentReference")]
        public string? PaymentReference { get; set; }

        [JsonPropertyName("amountPaid")]
        public decimal AmountPaid { get; set; }

        [JsonPropertyName("paidOn")]
        public string? PaidOn { get; set; }

        [JsonPropertyName("paymentStatus")]
        public string? PaymentStatus { get; set; } // "PAID", "PENDING", "FAILED", etc.

        [JsonPropertyName("customer")]
        public MonnifyWebhookCustomer? Customer { get; set; }

        [JsonPropertyName("destinationAccountInformation")]
        public MonnifyDestinationAccount? DestinationAccountInformation { get; set; }
    }

    public class MonnifyWebhookCustomer
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class MonnifyDestinationAccount
    {
        [JsonPropertyName("accountNumber")]
        public string? AccountNumber { get; set; } // matches Wallets.ReservedAccountNumber -
                                                   // this is how you find WHICH wallet to credit
    }
}