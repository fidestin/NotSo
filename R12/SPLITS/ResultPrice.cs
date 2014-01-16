using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOPSS.Common;
using TOPSS.Price;
using TOPSS.ProdData;
using TOPSS.Utility;
using Upfgs50Typelib;

namespace TOPSS.Pricing
{
	/// <summary>
	/// 
	/// </summary>
	public class ResultPricing : IPriceGenerator
	{
		Logger _logger = LogManager.GetCurrentClassLogger();
		TOPSSPrice _price;

		public ResultPricing(string pricingDataPath)
		{
			_price = new TOPSSPrice();
			_price.TopssPath = pricingDataPath;
		}

		/// <summary>
		/// Certain (existing Voycher/Precedent) products require checking of certain options settings to restrict/enable Ordering-TT8360
		/// </summary>
		/// <param name="set"></param>
		/// <param name="productData"></param>
		/// <returns></returns>
		private bool ModelOrderRestricted(SettingSet set, ProductDataJSON productData)
		{
			bool orderModelRestricted = false;
			foreach (KeyValuePair<FieldMetadata, OptionMetadata> fieldRestriction in productData.ModelNonOrderMap)
			{
				var setting = set.GetValue(fieldRestriction.Key.Id);
				if (fieldRestriction.Value != null)
				{
					if (setting.Value == fieldRestriction.Value.Id)
					{
						orderModelRestricted = true;
						break;
					}
				}
			}
			return orderModelRestricted;
		}

		public ResultPriceInfo GetResultPriceInfo(Result result, ModuleMetadata moduleData, PricingState state)
		{
			//PricePCG.ConnectionString = Properties.Settings.Default.PricePCGConnectionString;
			ProductDataJSON productData = moduleData.Parent;
			ProductPricing productPricing = PricingCache.GetProductPricing(productData.Name);
			ResultPriceInfo output = new ResultPriceInfo()
			{
				EmptyVpcs = new List<long>(),
				PricedFieldValues = new List<PricedFieldValue>(),
				Quantity = result.Quantity
			};

			bool isResidentialPricingMode = !string.IsNullOrEmpty(state.CostCenter);

			// Ensure that the result has all of the pricing required fields set


			// Call the pricing routine
			Dictionary<long, float> priceMap = null;
			Dictionary<long, float> netPricedMap = null;
			Dictionary<long, string> orderNumberMap = null;
			List<long> emptyFields = null;

			_price.PriceNonConfigItem(productData, result, ref priceMap, ref netPricedMap, ref emptyFields, ref orderNumberMap);

			if (emptyFields != null && emptyFields.Any())
			{
				output.EmptyVpcs = emptyFields;
			}
			else if (priceMap == null || priceMap.Count() == 0)
			{
				_logger.Error("ERROR in ResultPrice.cs::GetResultPriceInfo, missing idt and prt files in product: " + productData.Name);
			}
			else
			{
				// Generate the model number
				string modelNumber = "";
				if (productPricing.OrderNumberGenerationType == OrderNumberGenType.ModelNumber)
				{
					orderNumberMap.TryGetValue(productPricing.TypePricing.First().Value.OrderNumberField.VpcId, out modelNumber);
				}

				if (modelNumber == "")
				{
					modelNumber = UnitUtility.GenerateModelNumber(moduleData, result.Set).TrimEnd('*');
				}
				modelNumber = modelNumber.PadRight(40, '*'); // pad the model number with *s for the UPG system
				modelNumber = modelNumber.SubstringOrRest(0, 40);

				// Get the buy price multiplier
				Dictionary<long, BuyPriceInfo> buyPriceMap = null;
				if (isResidentialPricingMode)
				{
					buyPriceMap = GetBuyPriceInfoMap(state.CostCenter, modelNumber, priceMap, productData, productPricing, result, output);
				}

				// Generate priced field values
				//var fields = moduleData.Fields.Where(f => f.IsPriceSensitive).Aggregate<FieldMetadata, string>("The fields: ", (s, f) => s + f.Name + "; ");
				foreach (FieldMetadata field in moduleData.Fields.Where(f => f.IsPriceSensitive))
				{
					float price, netPrice = 0.0f;

					string orderNumber = "";
					orderNumberMap.TryGetValue(field.VpcId, out orderNumber);

					if (priceMap.TryGetValue(field.VpcId, out price))
					{
						if (isResidentialPricingMode)
						{
							// If in residential prcing mode, use the buy price multiplier for the net price
							float buyPriceMultiplier = 0.0f;
							BuyPriceInfo buyPriceInfo = buyPriceMap[field.VpcId];

							if (UnitUtility.IsModelNumberField(moduleData, field)) // Base unit field
							{
								buyPriceMultiplier = buyPriceInfo.BuyPriceMultiplier;
							}
							else // accessory field
							{
								try
								{
									string accessoryMultCode = PricingCache.GetAccessoryMultiplierCode(orderNumber);
									buyPriceMultiplier = PricePCG.AccessoryBuyPriceMultiplier(
										state.CostCenter,
										accessoryMultCode,
										productData.ProductFamilyId.ToString()
									);
								}
								catch (InvalidOperationException e)
								{
									// Log error that accy doesnt exist
								}
							}

							float percentDollarValue = buyPriceInfo.NetBuyPriceType == BuyPriceType.Calculated ? price * buyPriceMultiplier : buyPriceMultiplier;
							netPrice = PricePCG.Effective_Price(percentDollarValue, buyPriceInfo.PriceRounding);
						}
						else if (netPricedMap.TryGetValue(field.VpcId, out netPrice))
						{
						}

						output.PricedFieldValues.Add(new PricedFieldValue()
						{
							ListPrice = price,
							NetPrice = netPrice,
							OrderNumber = orderNumber,
							VpcId = field.VpcId
						});
					}
				}

				// Generate the list prices
				output.BaseListPrice = output.PricedFieldValues.Where(fv => IsModelNumberField(moduleData, fv.VpcId)).Aggregate(0.0f, (price, fv) => price += fv.ListPrice);
				output.TotalListPrice = output.PricedFieldValues.Aggregate(0.0f, (price, fv) => price += fv.ListPrice);

				// Generate net buy prices
				output.BaseNetBuyPrice = output.PricedFieldValues.Where(fv => IsModelNumberField(moduleData, fv.VpcId)).Aggregate(0.0f, (price, fv) => price += fv.NetPrice);
				output.TotalNetBuyPrice = output.PricedFieldValues.Aggregate(0.0f, (price, fv) => price += fv.NetPrice);

				string nonOrderCaption = "Ordering Information not available";
				//TT8360 - (New)Product/Configuration not orderable because of specific configuration

				// Set order number
				if (ModelOrderRestricted(result.Set, productData))
				{
					output.OrderNumber = nonOrderCaption;
				}
				else if (productPricing.OrderNumberGenerationType == OrderNumberGenType.ModelNumber)
				{
					output.OrderNumber = modelNumber.TrimEnd('*'); //remove the padding of the model number
				}
				else if (productPricing.OrderNumberGenerationType == OrderNumberGenType.Upfgs)
				{
					var upfgsCOM = new PfgCallOnGenerator();
					output.OrderNumber = GenerateUpfgsOrderNumber(productData, result, modelNumber, productPricing.GetProductCodes(result).First().Value,
						state, upfgsCOM);

					if (!string.IsNullOrEmpty(output.OrderNumber))
					{
						// Insert the OrderNumber (if required) into Product, ASSOC and Price
						string multGroupCode = productPricing.GetMultGroupCodeMap(result).First().Value;
						PricePCG.UpdateTOPSSWithPricingData(output.OrderNumber, modelNumber, productPricing.BusinessSegment, multGroupCode, state.Username, output.BaseListPrice);
					}
				}
				else if (productPricing.OrderNumberGenerationType == OrderNumberGenType.NotCurrentlyOrderable)
				{
					output.OrderNumber = nonOrderCaption;   //TT # 8360 
				}
				else
				{
					output.OrderNumber = "";
				}
			}

			return output;
		}

		public bool IsModelNumberField(ModuleMetadata moduleData, long vpcId)
		{
			FieldMetadata field;
			if (!moduleData.Parent.VpcIdToFieldMap.TryGetValue(vpcId, out field))
			{
				return false;
			}

			return field.ModelNumberEnd >= 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="costCenter"></param>
		/// <param name="productPricing"></param>
		/// <param name="modelNumber"></param>
		/// <param name="priceMap"></param>
		/// <param name="productData"></param>
		/// <param name="result"></param>
		/// <param name="priceInfo"></param>
		public Dictionary<long, BuyPriceInfo> GetBuyPriceInfoMap(string costCenter, string modelNumber,
			Dictionary<long, float> priceMap, ProductDataJSON productData, ProductPricing productPricing, Result result, ResultPriceInfo priceInfo)
		{
			Dictionary<string, BuyPriceInfo> typeToBuyPriceInfoMap = productPricing.GetMultGroupCodeMap(result).ToDictionary(p => p.Key, p =>
			{
				BuyPriceType buyPriceType = 0;
				Int32 roundingRule = 0;
				//PricePCG.ConnectionString = Properties.Settings.Default.PricePCGConnectionString;
				float buyPriceMultiplier = PricePCG.NetBuyPriceMultiplier(costCenter, modelNumber, productData.ProductFamilyId, p.Value,
					out buyPriceType, out roundingRule);

				return new BuyPriceInfo()
				{
					BuyPriceMultiplier = buyPriceMultiplier,
					MultGroupCode = p.Value,
					NetBuyPriceType = buyPriceType,
					PriceRounding = roundingRule
				};
			});

			if (productPricing.OrderNumberGenerationType == OrderNumberGenType.Split)
			{
				var map = productPricing.TypePricing.Join(typeToBuyPriceInfoMap, a => a.Key, a => a.Key, (a, b) => new
				{
					FieldId = a.Value.OrderNumberField.VpcId,
					BuyPriceInfo = b.Value
				}).ToDictionary(a => a.FieldId, a => a.BuyPriceInfo);

				return priceMap.Select(p =>
				{
					BuyPriceInfo info;
					if (!map.TryGetValue(p.Key, out info) && !map.TryGetValue(TryGetCorrespondingVpcId(p.Key).GetValueOrDefault(), out info))
					{
						info = new BuyPriceInfo()
						{
							NetBuyPriceType = BuyPriceType.Calculated,
							PriceRounding = 2,
							MultGroupCode = "",
							BuyPriceMultiplier = 0
						};
					}

					return new
					{
						Id = p.Key,
						Info = info
					};
				}).ToDictionary(p => p.Id, p => p.Info);
			}
			else
			{
				var buyPriceInfo = typeToBuyPriceInfoMap.First().Value;
				return priceMap.ToDictionary(on => on.Key, on => buyPriceInfo);
			}
		}

		static Dictionary<long, long> _vpcTranslationMap = new Dictionary<long, long>()
		{
			{ 2014031, 2014030 }, // SSC2 TWE
			{ 2014018, 2014017 }, // SSC2 TTA
			{ 2014023, 2014022 }, // SSC2 TWA
		};

		/// <summary>
		/// TODO: this function is a hack. this mapping needs to be done in product data
		/// </summary>
		/// <param name="vpcId"></param>
		/// <returns></returns>
		protected long? TryGetCorrespondingVpcId(long vpcId)
		{
			long outputVpcId;
			if (!_vpcTranslationMap.TryGetValue(vpcId, out outputVpcId))
			{
				return null;
			}

			return outputVpcId;
		}

		/// <summary>
		/// Calls the UPFGS50 COM component for the order number
		/// </summary>
		/// <returns></returns>
		protected string GenerateUpfgsOrderNumber(ProductDataJSON productData, Result result, string modelNumber, string productCode,
			PricingState state, PfgCallOnGenerator upfgsCOM)
		{
			string orderNumber = "";
			// TODO: Ensure that there are no order req fields
			if (/*result.EmptyOrderFieldDescs.Count == 0 &&*/ state.CostCenter != string.Empty && upfgsCOM != null)
			{
				try
				{
					long prodfamilyId = productData.ProductFamilyId;
					upfgsCOM.IOrderingNumberLynxServiceModelNumber = modelNumber;
					upfgsCOM.IOrderingNumberProdCode = productCode;            //0514 etc
					upfgsCOM.IOrderingNumberProdFamilyId = (int)prodfamilyId;

					//Collect params before calling remote COM object for better error messages
					string paramValues = "\nCOM ONumber PARAMS:\nprodFamilyID: " + prodfamilyId.ToString() + "\nModelNumber:" + modelNumber + "\nproductCode:" + productCode;

					upfgsCOM.Execute();

					orderNumber = upfgsCOM.EOrderingNumberOrderingNumber;

					paramValues += orderNumber.ToString();
				}
				catch (Exception ex)
				{
					_logger.Error("UPFGS com component failure: {0}", ex.Message);
				}
			}

			return orderNumber;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public static class UnitPricingManager
	{
		static IPriceGenerator _priceGenerator;

		public static void Initialize(string pricingPath)
		{
			_priceGenerator = new ResultPricing(pricingPath);
			PricingCache.Initialize(pricingPath);
		}

		public static ResultPriceInfo GetResultPriceInfo(Result result, ModuleMetadata moduleData, PricingState state)
		{
			return _priceGenerator.GetResultPriceInfo(result, moduleData, state);
		}
	}
}
