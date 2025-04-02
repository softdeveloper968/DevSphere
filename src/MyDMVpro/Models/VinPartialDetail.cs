using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace MyDMVpro.Models
{
    public partial class VinPartialDetail
    {
        [JsonIgnore]
        public int VinId { get; set; }
        public string VinPattern { get; set; }
        public string Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Cylinders { get; set; }
        public string Doors { get; set; }
        public string FuelType { get; set; }
        public string CurbWeight { get; set; }

        [NotMapped]
        public Dictionary<string, string> FieldMappings { get; set; } = new();

        public void InitFieldMappings()
        {
            AddFieldValue(FieldMappings, "Vehicle Year", Year);
            AddFieldValue(FieldMappings, "Vehicle Make", Make);
            AddFieldValue(FieldMappings, "Vehicle Model", Model);
            AddFieldValue(FieldMappings, "Cylinders", Cylinders);
            AddFieldValue(FieldMappings, "Power Type", FuelType);
            AddFieldValue(FieldMappings, "Unladen Weight", CurbWeight);
            AddFieldValue(FieldMappings, "Doors", Doors);

            // Lookup WV specific field values and add here
            AddFieldValue(FieldMappings, "WV-FuelType", FuelType);
        }
        private void AddFieldValue(Dictionary<string, string> fields, string fieldName, string fieldValue)
        {
            if (string.IsNullOrWhiteSpace(fieldValue)) return;
            if (fields.ContainsKey(fieldName))
            {
                fields[fieldName] = fieldValue;
            }
            else
            {
                fields.Add(fieldName, fieldValue);
            }
        }
    }
}
