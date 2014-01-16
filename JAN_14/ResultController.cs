using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using TOPSS.Common;
using TOPSS.Models;
using TOPSS.Pricing;
using TOPSS.ProdData;
using TOPSS.Utility;


namespace TOPSS.Controllers
{
	[SessionState(SessionStateBehavior.ReadOnly)]
	public class ResultController : TOPSSBaseController
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="unitId"></param>
		/// <param name="resultId"></param>
		/// <returns></returns>
		[HttpGet]
		public ActionResult PriceView(ObjectId unitId, ObjectId resultId, string costCenter)
		{
			return GetResult(unitId, resultId, (unit, module, result) =>
			{
				PricingState pricingState = new PricingState()
				{
					CostCenter = string.IsNullOrEmpty(costCenter) ? sessionManager.User.CostCenter.ToString() : costCenter,
					Username = sessionManager.User.Username,
                    UseSMARTPricing=sessionManager.User.SMARTPriceSource
				};

				List<Tuple<Unit, Result>> results = new List<Tuple<Unit, Result>>() { new Tuple<Unit, Result>(unit, result) };
				JobPricingModel jobPriceModel = PricingUtility.GetJobPricingModel(results, pricingState, sessionManager.User.IsPriceAdmin);
				jobPriceModel.IncludeHeader = false;

				ProductDataJSON productData = DataCache.GetProductData(unit.Product);
				ModuleMetadata moduleData = productData.GetModule(module.StaticModuleId);

				var query = PricePCG.GetCostCenterList().Select(c => new { costCenterCode = c.Key, costCenterName = c.Key + " " + c.Value });

				jobPriceModel.CostCenterList = new SelectList(query.AsEnumerable(), "costCenterCode", "costCenterName", pricingState.CostCenter);
				foreach (var resultModel in jobPriceModel.Results)
				{
					resultModel.CostCenter = costCenter;
				}

				return View("JobPrice", jobPriceModel);
			});
		}



		/// <summary>
		/// Renames a result
		/// </summary>
		/// <param name="unitId"></param>
		/// <param name="resultId"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Rename(ObjectId unitId, ObjectId resultId, string value)
		{
			return ModifyUnitBase(unitId, resultId, (unit, module, result) =>
			{
				unit.Name = value;
				result.Name = value;

				return new EmptyResult();
			});
		}

		/// <summary>
		/// Sets a result's quantity
		/// </summary>
		/// <param name="unitId"></param>
		/// <param name="resultId"></param>
		/// <param name="quantity"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Quantity(ObjectId unitId, ObjectId resultId, int quantity)
		{
			return ModifyUnitBase(unitId, resultId, (unit, module, result) =>
			{
				result.Quantity = quantity;

				return new EmptyResult();
			});
		}

		/// <summary>
		/// Deletes a result
		/// </summary>
		/// <param name="unitId"></param>
		/// <param name="resultId"></param>
		/// <returns></returns>
		[HttpDelete]
		public ActionResult Delete(ObjectId unitId, ObjectId resultId)
		{
			return ModifyUnitBase(unitId, resultId, (unit, module, result) =>
			{
				module.IsDirty = true;
				module.Results.Remove(result);

				return new EmptyResult();
			});
		}

		/// <summary>
		/// Marks a result in job
		/// </summary>
		/// <param name="unitId"></param>
		/// <param name="resultId"></param>
		/// <param name="isMarked"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult MarkInJob(ObjectId unitId, ObjectId resultId, bool isMarked)
		{
			return ModifyUnitBase(unitId, resultId, (unit, module, result) =>
			{
				module.MarkedResultId = isMarked ? resultId : ObjectId.Empty;

				return PartialView("ResultTree", new Tuple<Unit, Module, Result>(unit, module, result));
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="product"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Reorder(ObjectId unitId, ObjectId resultId, int index)
		{
			return ModifyUnitBase(unitId, resultId, (unit, module, result) =>
			{
				try
				{
					DisplayOrderSortableOperations ops = new DisplayOrderSortableOperations();
					var sortedResults = module.Results.OrderBy(r => r.DisplayOrder).ToList();
					HashSet<int> hs = new HashSet<int>();
					bool forceSave = !sortedResults.Select(r => r.DisplayOrder).All(hs.Add);
					if (forceSave)
						sortedResults.Aggregate(0, (ndx, r) => { r.DisplayOrder = ndx; return ndx + 1; });
					var sourceIndex = sortedResults.IndexOf(result);

					ops.ModifySortOrder(sortedResults, sourceIndex, index);
				}
				catch (InvalidOperationException e)
				{
					return Error("Exception in /Result/Reorder: " + e.Message);
				}

				return new EmptyResult();
			});
		}

		/// <summary>
		/// Returns a small preview of this result containing an image, model number, price, name, and important fields
		/// </summary>
		/// <param name="unitId"></param>
		/// <param name="resultId"></param>
		/// <returns></returns>
		public ActionResult Preview(ObjectId unitId, ObjectId resultId)
		{
			return GetResult(unitId, resultId, (unit, module, result) =>
			{
				ProductDataJSON productData = DataCache.GetProductData(unit.Product);
				if (productData == null)
				{
					throw new InvalidOperationException("Invalid product");
				}

				ModuleMetadata moduleData = productData.GetModule(module.StaticModuleId);
				UomScheme scheme = productData.GetUomScheme(sessionManager.User.Permissions.SelectedUomScheme);
				var fields = ResultAggregateUtility.GetPreviewFields(moduleData);

				var model = new ResultOverviewModel(result, fields, scheme, productData.Name);

				return View("Preview", model);
			});
		}
	}
}
