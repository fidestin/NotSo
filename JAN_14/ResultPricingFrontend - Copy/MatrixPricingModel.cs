using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Security;
using TOPSS.Common;

namespace TOPSS.Pricing
{
	public class SimplePriceMatrixModel
	{
		public IEnumerable<SimplePriceMatrixMarginEntry> Margins { get; set; }
	}

	public class SimplePriceMatrixMarginEntry
	{
		public int Margin { get; set; }
		public double SellPrice { get; set; }
		public double BuyPrice { get; set; }
		public double Rebate { get; set; }
		public double ListPrice { get; set; }
	}

	public class DetailedPriceMatrixModel
	{
		public string CostCenter { get; set; }
		public string Product { get; set; }
		public IEnumerable<PriceMatrixEntry> Components { get; set; }
		public IEnumerable<int> Margins { get; set; }
	}

	public class PriceMatrixEntry
	{
		public string Name { get; set; }
		public double ListPrice { get; set; }
		public IEnumerable<MarginRow> MarginValues { get; set; }
		public bool IsTotalUnit { get; set; }
	}

	public class MarginRow
	{
		public MultiplierType MultiplierType { get; set; }
		public IEnumerable<MarginInfo> MarginInfos { get; set; }
	}

	public class MarginInfo
	{
		public string Multiplier { get; set; }
		public double Price { get; set; }
		public int Margin { get; set; }
	}

	public enum MultiplierType
	{
		Buy,
		Sell,
		Rebate
	}
}
