using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOPSS.Common;
using TOPSS.ProdData;

namespace TOPSS.Pricing
{
	public enum OutputViewType
	{
		Output = 0,
		Graph,
		Pricing
	};
	/// <summary>
	/// 
	/// </summary>
	public interface IPriceGenerator
	{
		ResultPriceInfo GetResultPriceInfo(Result result, ModuleMetadata moduleData, PricingState state);
	}

	/// <summary>
	/// 
	/// </summary>
	public enum BuyPriceType
	{
		Specific = 0,
		Calculated = 1
	}

	public class PricingState
	{
		public string CostCenter { get; set; }
        public string Username { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public class ResultPriceInfo
	{
		public float BaseListPrice { get; set; }
		public float BaseNetBuyPrice { get; set; }
		public float TotalListPrice { get; set; }
		public float TotalNetBuyPrice { get; set; }
		public string OrderNumber { get; set; }
		public List<PricedFieldValue> PricedFieldValues { get; set; }
		public List<long> EmptyVpcs { get; set; }
		public int Quantity { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public class PricedFieldValue
	{
		public long VpcId { get; set; }
		public float ListPrice { get; set; }
		public float NetPrice { get; set; }
		public string OrderNumber { get; set; }
	}

	/// <summary>
	/// 
	/// </summary>
	public class BuyPriceInfo
	{
		public float BuyPriceMultiplier { get; set; }
		public BuyPriceType NetBuyPriceType { get; set; }
		public int PriceRounding { get; set; }
		public string MultGroupCode { get; set; }
	}

	//private  _pricedGroups = null;
	//	private List<int> _emptyPriceFields = null;
	//	private List<string> _emptyPriceFieldDescs = null;
	//	private List<KeyValuePair<int, string>> _orderingNumbers = null;
	//	private string _orderNumber = string.Empty;
	//	private List<string> _emptyOrderFieldDescs = null;
	//	private double _buyPriceMultiplier = 0.0;
	//	private BuyPriceType _neyBuyPriceType = 0;
	//	private Int32 _priceRounding = 0;
	//	private double _baseListPrice = 0;
	//	private double _baseNetBuyPrice = 0;
	//	private double _totalListPrice = 0;
	//	private double _totalNetBuyPrice = 0;
	//	private List<PriceMatrixData> _priceMatrixDatas = null;
	//	private List<PriceMatrixDetailsData> _priceMatrixDetailsDatas = null;
}
