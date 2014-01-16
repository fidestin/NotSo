using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using TOPSS.Common;
using TOPSS.ProdData;

namespace TOPSS.Pricing
{
	public static class PriceMatrixUtility
	{
		public static Tuple<SimplePriceMatrixModel, DetailedPriceMatrixModel> CreatePriceMatrixModels(ModuleMetadata moduleData, Result result, ResultPriceInfo resultPriceInfo,
			string costCenter,bool useSMARTPricing)
		{
			var productData = moduleData.Parent;

			// Base unit entry
			var comp = new[] 
			{
				new 
				{
					OrderNumber = resultPriceInfo.OrderNumber,
					ListPrice = resultPriceInfo.BaseListPrice,
					NetPrice = resultPriceInfo.BaseNetBuyPrice
				}
			}.ToList();

			// Accessory entries
			comp.AddRange(resultPriceInfo.PricedFieldValues.Select(pfv =>
			{
				var field = productData.VpcIdToFieldMap[pfv.VpcId];
				var setting = result.Set.GetValue(field.Id);
				var option = field.Options.FirstOrDefault(o => o.Id == setting.Value);

				return new
				{
					Field = field,
					Price = pfv,
					Option = option
				};
			}).Where(p => !p.Field.IsModelNumberField()).Select(p => new 
			{
				OrderNumber = p.Price.OrderNumber,
				ListPrice = p.Price.ListPrice,
				NetPrice = p.Price.NetPrice
			}));

			// Total unit entry
			var components = comp.AsParallel()
				.AsOrdered()
                .Select(c => CreateModelPriceMatrixEntry(PricePCG.loadMatrix(costCenter, c.OrderNumber, useSMARTPricing), c.OrderNumber,
					c.ListPrice, c.NetPrice, costCenter)).ToList();

			var totalPriceMap = components.SelectMany(c => c.MarginValues)
				.GroupBy(mr => mr.MultiplierType)
				.Select(g => new
				{
					Type = g.Key,
					MarginPrices = g.SelectMany(m => m.MarginInfos)
						.Where(m => m.Margin != 0)
						.GroupBy(m => m.Margin)
						.Select(m => new 
						{
							Margin = m.Key,
							Price = m.Aggregate<MarginInfo, float>(0.0f, (acc, elem) => acc + (float)elem.Price),
							Type = g.Key
						})
				}).ToList();

			totalPriceMap.Add(new 
			{
				Type = MultiplierType.Rebate,
				MarginPrices = totalPriceMap.First(t => t.Type == MultiplierType.Buy).MarginPrices.Select(mp => new 
				{
					Margin = mp.Margin,
					Price = Math.Max(resultPriceInfo.TotalNetBuyPrice - mp.Price, 0),
					Type = MultiplierType.Rebate
				})
			});

			components.Add(new PriceMatrixEntry()
			{
				IsTotalUnit = true,
				ListPrice = resultPriceInfo.TotalListPrice,
				Name = "Total",
				MarginValues = totalPriceMap.Select(tp => new MarginRow()
				{
					MultiplierType = tp.Type,
					MarginInfos = tp.MarginPrices.Select(i => new MarginInfo()
					{
						Margin = i.Margin,
						Multiplier = "",
						Price = i.Price
					})
				})
			});

			DetailedPriceMatrixModel detailedModel = new DetailedPriceMatrixModel()
			{
				Product = productData.Name,
				CostCenter = costCenter,
				Margins = _margins,
				Components = components
			};

			SimplePriceMatrixModel simpleModel = new SimplePriceMatrixModel()
			{
				Margins = totalPriceMap.SelectMany(tp => tp.MarginPrices).GroupBy(mp => mp.Margin).OrderBy(g => g.Key).Select(g => new SimplePriceMatrixMarginEntry()
				{
					Margin = g.Key,
					ListPrice = resultPriceInfo.TotalListPrice,
					SellPrice = g.First(m => m.Type == MultiplierType.Sell).Price,
					BuyPrice = g.First(m => m.Type == MultiplierType.Buy).Price,
					Rebate = g.First(m => m.Type == MultiplierType.Rebate).Price
				})
			};

			return new Tuple<SimplePriceMatrixModel,DetailedPriceMatrixModel>(simpleModel, detailedModel);
		}

		static int[] _margins = new int[]
		{
			10, 11, 12, 13, 14
		};

		static PriceMatrixEntry CreateModelPriceMatrixEntry(DataTable table, string modelNumber, float listPrice, float netBuyPrice, string costCenter)
		{
			var elems = table.Rows.Cast<DataRow>().Select(dr => new
			{
				MultGroupCode = dr.Field<string>("MULT_GROUP_CODE"),
				Margin = dr.Field<short>("MARGIN_PERCENT"),
				BuyMultiplier = dr.Field<float>("BUY_MULT"),
				SellMultiplier = dr.Field<float>("SELL_MULT")
			});

			var margins = from margin in _margins
							join elem in elems on margin equals elem.Margin into g
							from item in g.DefaultIfEmpty(new { MultGroupCode = "", Margin = new short(), BuyMultiplier = 0.0f, SellMultiplier = 0.0f })
							select item;

			return new PriceMatrixEntry()
			{
				IsTotalUnit = false,
				ListPrice = listPrice,
				Name = modelNumber,
				MarginValues = new List<MarginRow>()
				{
					new MarginRow()
					{
						MultiplierType = MultiplierType.Sell,
						MarginInfos = margins.Select(m =>
						{
							float calculatedPrice = m.SellMultiplier * listPrice;
							
							return new MarginInfo()
							{
								Margin = m.Margin,
								Multiplier = m.SellMultiplier.ToString(),
								Price = calculatedPrice,
							};
						}).ToList()
					},
					new MarginRow()
					{
						MultiplierType = MultiplierType.Buy,
						MarginInfos = margins.Select(m => 
						{
							float calculatedPrice = m.BuyMultiplier * listPrice;
							
							return new MarginInfo()
							{
								Margin = m.Margin,
								Multiplier = calculatedPrice > netBuyPrice ? "NetBuy" : m.BuyMultiplier.ToString(),
								Price = calculatedPrice > netBuyPrice ? netBuyPrice : calculatedPrice,
							};
						}).ToList()
					},
				}
			};
		}
	}
}