namespace MyDMVpro.Models.Tax
{
    public class UpdatedRuleDto
    {
        /// <summary>
        /// Gets or sets the item ID.
        /// </summary>
        public int ItemID { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the rule is applicable.
        /// </summary>
        public bool IsApplicable { get; set; }

        /// <summary>
        /// Gets or sets the taxable value.
        /// </summary>
        public decimal? Taxable { get; set; }

        /// <summary>
        /// Gets or sets the tax rate.
        /// </summary>
        public decimal? TaxRate { get; set; }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        public decimal? MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the state ID.
        /// </summary>
        public string StateID { get; set; }

        /// <summary>
        /// Gets or sets the jurisdiction ID.
        /// </summary>
        public int? JurisdictionID { get; set; }

        public bool RequiresTaxRateFetch { get; set; }
    }

    public class UpdatedFormulaDto
    {

        public string StateID { get; set; }

        public int? JurisdictionID { get; set; }

        public string Formula { get; set; }

        public string Id { get; set; }
    }
}
