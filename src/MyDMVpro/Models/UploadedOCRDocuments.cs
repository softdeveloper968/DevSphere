using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using MyDMVpro.Models.RegistrationFeeCalculatorViewModels;
using MyDMVpro.Models.Tax;
using System;
using System.Collections.Generic;

namespace MyDMVpro.Models
{
    public class UploadedOCRDocuments
    {
        public int Id { get; set; }
        public string RequestId { get; set; }
        public byte[] PdfData { get; set; }
        public string JsonData { get; set; }
        public string Status { get; set; }
        public DateTime UploadTime { get; set; } = DateTime.UtcNow;
        public byte[] SummaryData { get; set; }
    }

    public class DocumentUploadRequestModel
    {
        public string RequestId { get; set; }
        public string JsonData { get; set; }
        public IFormFile File { get; set; }
        public string Status { get; set; }
        public string Documents { get; set; }
        public string Situations { get; set; }
        public string TaxItems { get; set; }
        public string TotalTaxItems { get; set; }
        public string RegItems { get; set; }
        public string DataTags { get; set; }
        public IFormFile Summary { get; set; }
        public string StateId { get; set; }
        public string JurisdictionId { get; set; }
        public string RegInputs { get; set; }
        public string TaxBreakDown { get; set; }
    }

    public class RequestIdWrapper
    {
        public Guid RequestId { get; set; }
    }

    public class FileResponse
    {
        public string FileContent { get; set; }
        public string Filename { get; set; }
        public string RequestId { get; set; }
        public string AuthNumber { get; set; }
        public string BaseUrl { get; set; }
        public string Cookies { get; set; }
        public string BackendBaseUrl { get; set; }
        public string WebBaseUrl { get; set; }
    }

    public class TaxAndRegFeeModel
    {
        public string StateId { get; set; }
        public int? JurisdictionId { get; set; }
        public CalculatedTaxResult CalculatedTaxResult { get; set; }
        public VehicleInfo RegItems { get; set; }
        public List<CalculatedFeeValue> CalculatedFeeResult { get; set; }
    }
    public class CalculatedFeeValue
    {
        public string TableName { get; set; }
        public double ShippingCost { get; set; }
    }

    public class SaveCalculatedTax {
        public string RequestId { get; set; }
        public CalculatedTaxResult CalculatedTaxResult { get; set; }
    }
    public class SaveRegistrationModel
    {
        public string RequestId { get; set; }
        public VehicleInfo RegItems { get; set; }
        public List<CalculatedFeeValue> CalculatedFeeResult { get; set; }
    }
}
