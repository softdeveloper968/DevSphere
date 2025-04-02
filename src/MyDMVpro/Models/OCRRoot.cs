using System.ComponentModel.DataAnnotations.Schema;

namespace MyDMVpro.Models
{
    public class OCRRoot
    {
        [Column("DocumentTitle")]
        public string DocumentTitle { get; set; }

        [Column("PrimaryRegistrantSign")]
        public string PrimaryRegistrantSign { get; set; }

        [Column("CoRegistrantSign")]
        public string CoRegistrantSign { get; set; }

        [Column("OwnerSign")]
        public string OwnerSign { get; set; }

        [Column("CoOwnerSign")]
        public string CoOwnerSign { get; set; }

        [Column("SellerSignature")]
        public string SellerSignature { get; set; }

        [Column("LienHolderSignature")]
        public string LienHolderSignature { get; set; }

        [Column("SignOfAuthorizedRepresentative")]
        public string SignOfAuthorizedRepresentative { get; set; }

        [Column("Vehicle Vin")]
        public string VehicleVin { get; set; }

        [Column("Vehicle Year")]
        public string VehicleYear { get; set; }

        [Column("Vehicle Make")]
        public string VehicleMake { get; set; }

        [Column("Vehicle Model")]
        public string VehicleModel { get; set; }

        [Column("Vehicle Body Type")]
        public string BodyType { get; set; }

        [Column("Vehicle Color")]
        public string Color { get; set; }

        [Column("Unladen Weight")]
        public string UnladenWeight { get; set; }

        [Column("Max Gross Weight")]
        public string MaxGrossWeight { get; set; }

        [Column("Power Type")]
        public string TypeOfPower { get; set; }

        [Column("Cylinders")]
        public string NumberOfCylinder { get; set; }

        [Column("Adult Seating Capacity")]
        public string SeatingCapacity { get; set; }

        [Column("Vehicle Odometer")]
        public string OdometerReading { get; set; }

        [Column("REG-Reg-Name")]
        public string PrimaryRegistrant { get; set; }

        [Column("REG-CoReg-Name")]
        public string CoRegistrant { get; set; }

        [Column("REG-Reg-Gender")]
        public string GenderPrimaryRegistrant { get; set; }

        [Column("REG-CoReg-Gender")]
        public string GenderCoRegistrant { get; set; }

        [Column("REG-Own-Name")]
        public string Owner { get; set; }

        [Column("REG-Own-Gender")]
        public string GenderOfOwner { get; set; }

        [Column("REG-Own-DOB")]
        public string OwnerDOB { get; set; }

        [Column("REG-Own-IdentificationNumber")]
        public string OwnerId { get; set; }

        [Column("REG-CoOwn-Name")]
        public string CoOwner { get; set; }

        [Column("REG-Own-Address-Street")]
        public string OwnerStreet { get; set; }

        [Column("REG-Own-Address-City")]
        public string OwnerCity { get; set; }

        [Column("REG-Own-Address-State")]
        public string OwnerState { get; set; }

        [Column("REG-Own-Address-ZIP")]
        public string OwnerZip { get; set; }

        [Column("REG-Reg-IdentificationNumber")]
        public string FirstRegistrantID { get; set; }

        [Column("FirstLicensorDOB")]
        public string FirstLicensorDOB { get; set; }

        [Column("REG-CoReg-IdentificationNumber")]
        public string SecondRegistrantID { get; set; }

        [Column("SecondLicensorDOB")]
        public string SecondLicensorDOB { get; set; }

        [Column("REG-Reg-Address-Street")]
        public string PrimaryRegistrantStreet { get; set; }

        [Column("REG-Reg-Address-City")]
        public string PrimaryRegistrantCity { get; set; }

        [Column("REG-Reg-Address-State")]
        public string PrimaryRegistrantState { get; set; }

        [Column("REG-Reg-Address-ZIP")]
        public string PrimaryRegistrantZip { get; set; }

        [Column("REG-Reg-Mail-Address-Street")]
        public string PrimaryRegistrantMailingAddressStreet { get; set; }

        [Column("REG-Reg-Mail-Address-City")]
        public string PrimaryRegistrantMailingAddressCity { get; set; }

        [Column("REG-Reg-Mail-Address-State")]
        public string PrimaryRegistrantMailingAddressState { get; set; }

        [Column("REG-Reg-Mail-Address-ZIP")]
        public string PrimaryRegistrantMailingAddressZip { get; set; }

        [Column("Plate Number")]
        public string PlateNumber { get; set; }

        [Column("REG-Dlr-Name")]
        public string Seller { get; set; }

        [Column("REG-Dlr-Address-Street")]
        public string SellerStreet { get; set; }

        [Column("REG-Dlr-Address-City")]
        public string SellerCity { get; set; }

        [Column("REG-Dlr-Address-State")]
        public string SellerState { get; set; }

        [Column("REG-Dlr-Address-ZIP")]
        public string SellerZip { get; set; }

        [Column("REG-Dlr-IdentificationNumber")]
        public string DealerLicenseNumber { get; set; }

        [Column("SalesTaxRegistrationNumber")]
        public string SalesTaxRegistrationNumber { get; set; }

        [Column("REG-LH-Name")]
        public string LienHolder { get; set; }

        [Column("REG-LH-IdentificationNumber")]
        public string LienFilingCode { get; set; }

        [Column("REG-LH-Address-Street")]
        public string LienHolderStreet { get; set; }

        [Column("REG-LH-Address-City")]
        public string LienHolderCity { get; set; }


        [Column("REG-LH-Address-State")]
        public string LienHolderState { get; set; }

        [Column("REG-LH-Address-ZIP")]
        public string LienHolderZip { get; set; }

        [Column("Insurance Company Name")]
        public string InsuranceCompany { get; set; }

        [Column("Insurer")]
        public string Insurer { get; set; }

        [Column("IsReplacement")]
        public string IsReplacement { get; set; }

        [Column("Insurance Effective Date")]
        public string EffectiveDate { get; set; }

        [Column("Insurance Expiration Date")]
        public string ExpirationDate { get; set; }

        [Column("DateOfPurchase")]
        public string DateOfPurchase { get; set; }

        [Column("Legal Sale Date")]
        public string DateOfSale { get; set; }

        [Column("Vehicle Sale Price")]
        public string PurchasePrice { get; set; }

        [Column("PreDeliveryServiceCharge")]
        public string PreDeliveryServiceCharge { get; set; }

        [Column("ElectronicRegistrationFilingFee")]
        public string ElectronicRegistrationFilingFee { get; set; }

        [Column("CCFAdvantage12MO")]
        public string CCFAdvantage12MO { get; set; }

        [Column("WindowTint")]
        public string WindowTint { get; set; }

        [Column("AppearanceProtection36Mos")]
        public string AppearanceProtection36Mos { get; set; }

        [Column("Freight")]
        public string Freight { get; set; }

        [Column("PREP")]
        public string PREP { get; set; }

        [Column("PrepaidMaintenance")]
        public string PrepaidMaintenance { get; set; }

        [Column("RoadHazard")]
        public string RoadHazard { get; set; }

        [Column("ServiceContract")]
        public string ServiceContract { get; set; }

        [Column("TradeInAllowance")]
        public string TradeInAllowance { get; set; }

        [Column("Rebate")]
        public string Rebate { get; set; }

        [Column("REG-Reg-Address-County")]
        public string CountyPrimaryRegistrant { get; set; }

        [Column("HasSixDigit")]
        public string HasSixDigit { get; set; }

        [Column("HasFiveDigit")]
        public string HasFiveDigit { get; set; }

        [Column("Damage Disclosure")]
        public string HasVehicleBeenWrecked_1 { get; set; }

        [Column("PersonalVSCommercial")]
        public string IsVehicleRegisteredForPersonalUse_2 { get; set; }

        [Column("Modified")]
        public string HasVehicleBeenModified_3 { get; set; }

        [Column("Altered")]
        public string WasVehicleAltered_4 { get; set; }

        [Column("IsAPickUpTruck_5")]
        public string IsAPickUpTruck_5 { get; set; }

        [Column("Security Contract Date")]
        public string DateOfSecurityAgreement { get; set; }
    }
}
