using System.ComponentModel.DataAnnotations;
using PetShop.Payments.Api.Domain;

namespace PetShop.Payments.Api.Contracts;

public sealed class CreatePaymentRequest
{
    [Required] public Guid OrderId { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.COD;
}

public sealed record PaymentResponse(Guid Id, Guid OrderId, Guid CustomerId, decimal Amount,
    PaymentMethod Method, PaymentStatus Status, string? TransactionCode,
    DateTime CreatedAt, DateTime? PaidAt, DateTime? RefundedAt);
