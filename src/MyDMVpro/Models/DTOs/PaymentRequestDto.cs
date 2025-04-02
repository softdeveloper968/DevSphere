using System;

namespace MyDMVpro.Models.DTOs
{
    public class PaymentRequestDto
    {
        public decimal Amount { get; set; }
        public decimal ServiceCharge { get; set; }
        public string Currency { get; set; } = "USD";
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
        public Guid RequestId { get; set; }
        public string Vin { get; set; }
        public string PaymentMethodType { get; set; }
    }
}
