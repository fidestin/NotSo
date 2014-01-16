using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOPSS.Common;
using TOPSS.ProdData;

namespace TOPSS.Pricing
{
	public static class PricingExtensions
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="productPricing"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static Dictionary<string, string> GetProductCodes(this ProductPricing productPricing, Result result)
		{
			return productPricing.TypePricing.ToDictionary(p => p.Key, pair =>
				{
					string productCode;
					if (pair.Value.ProductCode != "")
					{
						productCode = pair.Value.ProductCode;
					}
					else
					{
						// Hardcoded Precdent product code generation - TODO: put the VPCID of the field(s) that must be compared to generate this
						var voltage = pair.Value.FieldPricing.First(p => p.Value.Description == "Voltage"); // side effect, throw exception
						var value = result.Set.GetValue(voltage.Key.Id);

						if (value == null)
						{
							throw new InvalidOperationException("Must have the voltage field set");
						}

						var option = voltage.Value.Options.First(o => o.Option.Id == Convert.ToInt64(value.Value)); // side effect, throw exception

						productCode = option.ProductCode;
					}

					return productCode;
				});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="productPricing"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static Dictionary<string, string> GetMultGroupCodeMap(this ProductPricing productPricing, Result result)
		{
			return productPricing.GetProductCodes(result).ToDictionary(p => p.Key, pair =>
				{
					foreach (var fieldPricing in productPricing.TypePricing[pair.Key].FieldPricing)
					{
						if (fieldPricing.Value.IsMultGroupSearch)
						{
							var value = result.Set.GetValue(fieldPricing.Key.Id);
							var option = fieldPricing.Value.Options.FirstOrDefault(o => o.Option.Id == Convert.ToInt64(value.Value) && o.ProductCode == pair.Value); // side effect, throw exception

							if (option == null)
								return "";

							return option.MultGroupCode;
						}
					}

					return "";
				});
		}
	}
}
