namespace MyDMVpro.Models
{
    public class RequestDTO
    {
        public string id { get; set; }
        public decimal? dmvFee { get; set; }
        public decimal? svcFee { get; set; }
        public decimal? otherFee { get; set; }
        public string otherDesc { get; set; }
        public decimal? totalFee { get; set; }
        public string glCode { get; set; }

        // Secondary invoice
        public decimal? svcFee2 { get; set; } = 0;
        public decimal? otherFee2 { get; set; } = 0;
        public string otherDesc2 { get; set; }
        public decimal? totalFee2 { get; set; } = 0;

    }
}
