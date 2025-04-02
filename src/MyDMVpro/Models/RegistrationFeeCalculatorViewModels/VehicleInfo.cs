namespace MyDMVpro.Models.RegistrationFeeCalculatorViewModels
{
    public class VehicleInfo
    {
        public string Cylinders { get; set; }
        public string Jurisdiction { get; set; }
        public string Lien { get; set; }
        public string PlateClass { get; set; }
        public string Title { get; set; }
        public string Propulsion { get; set; }
        public string Year { get; set; }
        public int Mpg { get; set; } = 0;
        public int Mgw { get; set; } = 0;
        public int UnladenWeight { get; set; } = 0;
        public string StateCode { get; set; }
    }

}
