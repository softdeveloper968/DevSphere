using System;
using System.ComponentModel.DataAnnotations;

namespace MyDMVpro.Models.RequestsViewModels
{
    public class RequestsDetailsViewModel
    {
        public dynamic Request { get; set; }
        public Guid RequestId { get; set; }
        public Guid UserId { get; set; }
        public Guid VendorId { get; set; }
    }
}
