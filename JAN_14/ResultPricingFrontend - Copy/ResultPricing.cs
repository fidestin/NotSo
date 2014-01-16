using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TOPSS.Common;
using TOPSS.Common.DataContext;
using TOPSS.ProdData;
using System.Data;
using TOPSS.Utility;
using System.Threading;

namespace TOPSS.Pricing
{
	public static class PricingUtility
	{
		const string PSEUDO_GROUP_NAME = "Other";

		public static JobPricingModel GetJobPricingModel(List<Tuple<Unit, Result>> results, PricingState pricingState, bool isPriceAdmin)
		{
			var resultPricingModels = results.Select(r => GetResultPricingModel(r.Item1, r.Item2, pricingState)).Where(m => m != null).ToList();
			JobPricingModel jobModel = new JobPricingModel()
			{
				ListPrice = resultPricingModels.Select(p => p.PriceInfo).Aggregate<ResultPriceInfo, double>(0.0, (acc, price) => acc + price.TotalListPrice * price.Quantity),
				NetPrice = resultPricingModels.Select(p => p.PriceInfo).Aggregate<ResultPriceInfo, double>(0.0, (acc, price) => acc + price.TotalNetBuyPrice * price.Quantity),
				Results = resultPricingModels,
				PriceAdmin = isPriceAdmin,
                UseSMART=pricingState.UseSMARTPricing
			};

			return jobModel;
		}

		/// <summary>
		/// Returns a populated ResultPricingModel for that Unit and Result
		/// </summary>
		/// <returns></returns>
		public static ResultPricingModel GetResultPricingModel(Unit unit, Result result, PricingState pricingState)
		{
			var productData = DataCache.GetProductData(unit.Product);
			var moduleData = productData.GetModule(unit.Modules[0].StaticModuleId);

			if (!moduleData.PriceRequiredFields.Aggregate<FieldMetadata, bool>(true, (val, field) => !result.Set.IsFieldEmpty(field.Id) && val))
			{
				return null;
			}

			var priceInfo = UnitPricingManager.GetResultPriceInfo(result, moduleData, pricingState);

			// Create pseudo-groups
			var pseudoGroups = priceInfo.PricedFieldValues
				.Select(p => productData.VpcIdToFieldMap[p.VpcId])
				.Where(f => !f.IsInOutputGroup)
				.GroupBy(f => f.IsModelNumberField())
				.Select(g => new PricingGroup()
				{
					IncludesModelNumber = g.Key,
					Name = PSEUDO_GROUP_NAME,
					DisplayOrder = Int64.MaxValue,
					Fields = g.Select(f => new PricingField()
					{
						Field = f,
						Option = GetOption(result.Set, f),
						PricingData = priceInfo.PricedFieldValues.FirstOrDefault(fv => fv.VpcId == f.VpcId),
					}).ToList()
				});

			List<PricingGroup> groups = (from g in moduleData.AllGroups
												  where g.IsOutputGroup && g.Fields.Any(f => f.Field.IsPriceSensitive)
												  select new PricingGroup()
												  {
													  Name = ProductDataResourceManager.GetGroupName(g, Thread.CurrentThread.CurrentUICulture),
													  DisplayOrder = g.DisplaySequence,
													  IncludesModelNumber = g.Fields.Any(f => f.Field.IsModelNumberField()),
													  Fields = GetPricingFields(g.Fields.Select(gf => gf.Field), result.Set, priceInfo)
												  }).Concat(pseudoGroups).ToList();

			var priceSensitiveFields = moduleData.Fields
				.Where(f => f.IsPriceSensitive)
				.ToArray();
			var grouplessPricingFields = priceSensitiveFields
				.Except(groups.SelectMany(g => g.Fields.Select(f => f.Field)))
				.ToArray();

			if (grouplessPricingFields.Any())
			{
				var fields = GetPricingFields(grouplessPricingFields, result.Set, priceInfo);
				PricingGroup otherGroup = groups.FirstOrDefault(g => g.Name.Equals(PSEUDO_GROUP_NAME));
				if (otherGroup == null)
				{
					otherGroup = new PricingGroup()
					{
						DisplayOrder = groups.Select(g => g.DisplayOrder).Max() + 1,
						Fields = new List<PricingField>(),
						IncludesModelNumber = grouplessPricingFields.Any(f => f.IsModelNumberField()),
						Name = PSEUDO_GROUP_NAME
					};
					groups.Add(otherGroup);
				}
				otherGroup.Fields.AddRange(fields);
			}

			var pricingModels = PriceMatrixUtility.CreatePriceMatrixModels(moduleData, result, priceInfo, pricingState.CostCenter,pricingState.UseSMARTPricing);

			ResultPricingModel model = new ResultPricingModel()
			{
				Result = result,
				PriceInfo = priceInfo,
				EmptyFields = priceInfo.EmptyVpcs.Select(vpcid => productData.VpcIdToFieldMap[vpcid]).ToList(),
				Groups = groups.OrderBy(pg => !pg.IncludesModelNumber).ThenBy(pg => pg.DisplayOrder),
				SimpleMatrixModel = pricingModels.Item1,
				DetailedMatrixModel = pricingModels.Item2,
				Quantity = result.Quantity
			};

			return model;
		}

		static List<PricingField> GetPricingFields(IEnumerable<FieldMetadata> fields, SettingSet set, ResultPriceInfo priceInfo)
		{
			return (from field in fields
					  let pricingData = priceInfo.PricedFieldValues.FirstOrDefault(fv => fv.VpcId == field.VpcId)
					  where field.IsPriceSensitive && (field.Id >= 0 || (pricingData != null && pricingData.ListPrice > 0))
					  select new PricingField()
					  {
						  Field = field,
						  Option = GetOption(set, field),
						  PricingData = pricingData
					  }).ToList();
		}

		static OptionMetadata GetOption(SettingSet set, FieldMetadata field)
		{
			var setting = set.GetValue(field.Id);
			return setting == null ? null : field.Options.FirstOrDefault(o => o.Id == Convert.ToInt64(setting.Value));
		}
	}
}
