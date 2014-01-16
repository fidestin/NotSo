using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;
using System.Collections;
using TOPSS.Common;
using TOPSS.ProdData;
using System.Data;
using System.Web.Mvc;


namespace TOPSS.Pricing
{
	/// <summary>
	/// Represents an entire job's pricing information
	/// </summary>
	public class JobPricingModel
	{
		public List<ResultPricingModel> Results { get; set; }
		public double ListPrice { get; set; }
		public double NetPrice { get; set; }
		public bool PriceAdmin { get; set; }
		public bool IncludeHeader { get; set; }
        public bool UseSMART { get; set; }
		public SelectList CostCenterList { get; set; }
		//public NavigationBreadcrumbModel NavigationBreadcrumb { get; set; }
	}

	/// <summary>
	/// Represents a single results pricing information
	/// </summary>
	public class ResultPricingModel
	{
		public Result Result { get; set; }
		public IEnumerable<PricingGroup> Groups { get; set; }
		public ResultPriceInfo PriceInfo { get; set; }
		public List<FieldMetadata> EmptyFields { get; set; }
		public SimplePriceMatrixModel SimpleMatrixModel { get; set; }
		public DetailedPriceMatrixModel DetailedMatrixModel { get; set; }
		public String CostCenter { get; set; }
		public int Quantity { get; set; }
	}

	public class PricingGroup
	{
		public string Name { get; set; }
		public bool IncludesModelNumber { get; set; }
		public List<PricingField> Fields { get; set; }
		public long DisplayOrder { get; set; }
	}

	public class PricingField
	{
		public FieldMetadata Field { get; set; }
		public OptionMetadata Option { get; set; }
		public PricedFieldValue PricingData { get; set; }
	}
}
