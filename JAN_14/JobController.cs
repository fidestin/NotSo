using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Configuration;
using System.Xml.Linq;
using System.Web.Hosting;
using System.Diagnostics;
using System.Web.UI;
using System.Text;

using TOPSS.Models;
using TOPSS.Utility.XMLGeneration;
using TOPSS.Utility;
using TOPSS.Units;
//using TOPSS.Rules;
using TOPSS.ProdData;
using Newtonsoft.Json;
using MongoDB.Bson;
using AutoMapper;
using TOPSS.Common;
using NLog;
using TOPSS.Common.DataContext;
using TOPSS.Pricing;
//using TOPSS.ExportImportTof;


//using Oracle.DataAccess;
//using Oracle.DataAccess.Client;
//using Oracle.DataAccess.Types;


namespace TOPSS.Controllers
{
	public class JobController : TOPSSBaseController
	{
		static Logger _logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Index()
		{
			return View(GetCurrentJobModel());
		}

		[HttpGet]
		public ActionResult Details()
		{
			return View(GetCurrentJobModel());
		}

		[HttpPost]
		public ActionResult Load(ObjectId id)
		{
			sessionManager.CurrentJobId = id;

			JobDataContext db = new JobDataContext();
			Job job = db.GetJobById(sessionManager.CurrentJobId);
			var units = db.GetUnits(sessionManager.CurrentJobId);

			return ViewJobData(id, job, units);
		}

		[HttpGet]
		public ActionResult PriceView(string costCenter)
		{
			JobDataContext db = new JobDataContext();
			var units = db.GetUnits(sessionManager.CurrentJobId);

			// All marked in job results.
			var results = (from unit in units
								from module in unit.Modules
								where module.MarkedResultId != ObjectId.Empty
								let result = module.Results.FirstOrDefault(r => r.Id == module.MarkedResultId)
								where result != null
								select new Tuple<Unit, Result>(unit, result)).ToList();
			var disabledProductList = results.Where(pair =>
			{
				Unit unit = pair.Item1;
				ProductDataJSON productData = DataCache.GetProductData(unit.Product);
				return !productData.IsPriceAllowed;
			}).Select(pair => pair.Item1.Product).ToList();
			//don't want duplicate products.
			HashSet<string> disabledProductsHash = new HashSet<string>();
			disabledProductList.ForEach(name => disabledProductsHash.Add(name));

			results = results.Where(pair =>
			{
				Unit unit = pair.Item1;
				ProductDataJSON productData = DataCache.GetProductData(unit.Product);
				return productData.IsPriceAllowed;
			}).ToList();

			var query = PricePCG.GetCostCenterList().Select(c => new { costCenterCode = c.Key, costCenterName = c.Key + " " + c.Value });

			PricingState pricingState = new PricingState()
			{
				CostCenter = string.IsNullOrEmpty(costCenter) ? sessionManager.User.CostCenter.ToString() : costCenter,
				Username = sessionManager.User.Username,
                UseSMARTPricing=sessionManager.User.SMARTPriceSource
			};

			SelectList listCostCenter = new SelectList(query.AsEnumerable(), "costCenterCode", "costCenterName", pricingState.CostCenter);

			JobPricingModel jobPriceModel = PricingUtility.GetJobPricingModel(results, pricingState, sessionManager.User.IsPriceAdmin);
			jobPriceModel.IncludeHeader = true;
			jobPriceModel.CostCenterList = listCostCenter;

			string disabledProductsString = disabledProductsHash.Aggregate("", (message, name) => message + name + ", ");
			if (disabledProductsString.Any())
			{
				disabledProductsString = disabledProductsString.SubstringOrRest(0, disabledProductsString.Length - 2);
			}
			ViewBag.DisabledProducts = disabledProductsString;
			ViewBag.NoPriceRequiredFields = jobPriceModel.Results.Count() != results.Count();

			ViewBag.NavigationBreadcrumb = new NavigationBreadcrumbModel()
			{
				BreadcrumbItems = new List<NavigationBreadcrumbItem>()
					{
						new NavigationBreadcrumbItem()
						{
							PartialViewName = "NavigationBreadcrumbItem",
							TextValue = GetCurrentJob().Name,
							CanEdit = false,
							IconClass = "icon-home",
							OnClickHandler = "breadcrumbHandlers.onClickNavToJob(this)"
						},
						new NavigationBreadcrumbItem()
						{
							PartialViewName = "NavigationBreadcrumbItem",
							TextValue = "Price Equipment",
							CanEdit = false,
							IconClass = "icon-th-list",
							Active = true
						}
					}
			};
			return View("JobPrice", jobPriceModel);
		}

		[HttpPost]
		public ActionResult UpdateCostCenter(string costCenter)
		{
			JobDataContext db = new JobDataContext();
			var units = db.GetUnits(sessionManager.CurrentJobId);

			var results = (from unit in units
								from module in unit.Modules
								where module.MarkedResultId != ObjectId.Empty
								let result = module.Results.FirstOrDefault(r => r.Id == module.MarkedResultId)
								where result != null
								select new Tuple<Unit, Result>(unit, result)).ToList();

			PricingState pricingState = new PricingState()
			{
				CostCenter = string.IsNullOrEmpty(costCenter) ? sessionManager.User.CostCenter.ToString() : costCenter,
				Username = sessionManager.User.Username,
                UseSMARTPricing=sessionManager.User.SMARTPriceSource
			};

			JobPricingModel jobPriceModel = PricingUtility.GetJobPricingModel(results, pricingState, sessionManager.User.IsPriceAdmin);

			return Json(new
			{
				baseNetPrice = string.Format("{0:C0}", jobPriceModel.NetPrice),
				netPrice = string.Format("{0:C0}", jobPriceModel.NetPrice),
				results = jobPriceModel.Results.Select(r => new
				{
					id = r.Result.Id.ToString(),
					netPrice = string.Format("{0:C0}", r.PriceInfo.TotalNetBuyPrice),
					baseNetPrice = string.Format("{0:C0}", r.PriceInfo.BaseNetBuyPrice),
					fields = r.Groups.SelectMany(g => g.Fields)
					.Where(f => f.PricingData != null)
					.Select(f => new
					{
						id = f.PricingData.VpcId,
						netPrice = string.Format("{0:C0}", f.PricingData.NetPrice)
					})
				})
			});
		}

		[HttpPost]
		public ActionResult AddNewJob(AddNewJobInput jobInput)
		{
			var creationDate = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now, TimeZoneInfo.Local);

			Job job = new Job()
			{
				UserId = sessionManager.User.Id,
				ProductOrdering = new List<ProductInfo>(),
				CreatedOn = creationDate,
				LastModifiedOn = creationDate
			};

			Mapper.Map(jobInput, job);

			JobDataContext db = new JobDataContext();
			db.SaveJob(job);

			sessionManager.CurrentJobId = job.Id;

			return ViewJobData(sessionManager.CurrentJobId, job, new List<Unit>());
		}

		//[HttpPost]
		//public ActionResult SearchOrderNumber(string orderNumber, string modelNumber)
		//{
		//	string ordering = orderNumber.Trim().ToUpper();
		//	string modeln = modelNumber.Trim().ToUpper();

		//	List<KeyValuePair<string, string>> data = PricePCG.FindMatchingOrderingOrModelNumber(ordering, modeln);

		//	string returnedOrdNum = string.Empty;
		//	string returnedModNum = string.Empty;

		//	if(data != null && data.Count > 0)
		//	{
		//		//process list return data here
		//		foreach(KeyValuePair<string, string> pair in data)
		//		{
		//			returnedOrdNum = pair.Key;
		//			returnedModNum = pair.Value;
		//		}
		//	}

		//	if(returnedOrdNum != string.Empty)
		//	{
		//		return Json(new
		//		{
		//			//returnedText = string.Format("<p style='padding-left:1em;padding-right:1em;'><b>Order Number: </b>{0} <b>Model Number: </b><a id='link{1}' href='default.aspx?action=searchordnum&modnum={1}' onclick='BlockUIWithImage();'>{1}</a></p>", returnedOrdNum, returnedModNum)
		//			retOrderNumber = returnedOrdNum,
		//			retModelNumber = returnedModNum,
		//			//returnedText = string.Format("Order Number: {0} Model Number: {1}", returnedOrdNum, returnedModNum)
		//			returnedText = "Success - Click Continue to create a job with the selected order number"
		//		});
		//	}
		//	else
		//	{
		//		return Json(new
		//		{
		//			returnedText = "No matches found"
		//		});
		//	}
		//}

		//[HttpPost]
		//public ActionResult SearchOrderNumberContinue(string orderNumber, string modelNumber)
		//{
		//	//figure out the product
		//	string productXml = Path.Combine(Properties.Settings.Default.TopssPath, "Products.xml");
		//	long productId = -1;
		//	string dllkey = string.Empty;
		//	TOPSS.Utility.ModelNumber.getIDFromModelNumber(modelNumber, productXml, Properties.Settings.Default.TopssPath, out productId, out dllkey);

		//	//create the criteria
		//	ProductData productData = SessionManager.ProductData(dllkey);
		//	ProductDataJSON productDataJSON = DataCache.GetProductData(dllkey);
		//	long modId = productData.BaseModuleData.Id;
		//	long critId = -1;
		//	RuleBase rules = SessionManager.RuleData(dllkey);
		//	Product newProduct = UtilityJSON.CreateDefaultCriteria(productData, modId, ref rules, out critId, productDataJSON);

		//	//Parse the model number
		//	Criteria criteria = newProduct.FindCriteria(-1, critId);
		//	if(criteria != null)
		//	{
		//		SettingSet setCrit = criteria as SettingSet;
		//		TOPSS.Utility.ModelNumber.ParseModelNumber(ref setCrit, rules, productData.BaseModuleData, modelNumber);
		//		criteria = setCrit as Criteria;
		//	}

		//	Job job = new Job();
		//	job.Name = orderNumber;
		//	job.Notes = "Autommatically generated from order number search";
		//	job.Products.Add(newProduct);

		//	JobModel model = JobHelper.CreateJobModel(job);

		//	sessionManager.JobModel = model;
		//	return ViewJobData();
		//}

		[HttpGet]
		public ActionResult ImportPSD(bool isAppend)
		{
			TopssDialogModel model = new TopssDialogModel()
			{
				RightButton = new ButtonInfo()
				{
					Text = "Import",
					ClickFunction = "onClickSubmitImportPSD()"
				},
				TitleText = "Import PSD File",
				DescriptionText = "Choose a file to upload:",
				Fields = new List<Models.EditField>()
        		  {
        				new Models.EditField()
        				{
        					 DialogType = Models.DialogType.PartialView,
        					 PartialViewName = "../Job/ImportPSD",
        					 PartialViewModel = isAppend
        				}
        		  }
			};
			return View("Dialog", model);
		}

		[HttpPost]
		public ActionResult ImportPSDAction(HttpPostedFileBase file)
		{
			JobUnits jobUnits = CommonXml.GetJobFromPsd(file.InputStream, file.FileName);
			JobXmlImport(jobUnits);
			return new EmptyResult();
		}
		[HttpPost]
		public ActionResult LoadJobData()
		{
			JobDataContext db = new JobDataContext();
			Job job = db.GetJobById(sessionManager.CurrentJobId);
			var units = db.GetUnits(sessionManager.CurrentJobId);
			return ViewJobData(sessionManager.CurrentJobId, job, units);
		}

		[HttpPost]
		public ActionResult AppendPSDToJobAction(HttpPostedFileBase file)
		{
			JobDataContext db = new JobDataContext();
			Job job = db.GetJobById(sessionManager.CurrentJobId);
			var units = db.GetUnits(sessionManager.CurrentJobId).ToList();
			JobUnits jobUnits = CommonXml.GetJobFromPsd(file.InputStream, file.FileName);
			units.AddRange(jobUnits.Units);
			int lastDisaply = 0;
			if (job.ProductOrdering.Any())
			{
				lastDisaply = job.ProductOrdering.OrderBy(p => p.DisplayOrder).Last().DisplayOrder + 1;
			}
			List<string> productsAlreadyInJob = job.ProductOrdering.Select(p => p.Product).ToList();
			foreach (ProductInfo prodInfo in jobUnits.Job.ProductOrdering)
			{
				if (productsAlreadyInJob.Contains(prodInfo.Product))
				{
					List<Unit> unitsInProd = jobUnits.Units.Where(u => u.Product == prodInfo.Product).ToList();
					ProductInfo jobProdInfo = job.ProductOrdering.FirstOrDefault(pi => pi.Product == prodInfo.Product);
					//job.ProductOrdering.Remove(jobProdInfo);
					for (int i = 0; i < unitsInProd.Count(); i++)
					{
						Unit unit = unitsInProd[i];
						unit.DisplayOrder = jobProdInfo.NextUnitDisplayOrder;
						jobProdInfo.NextUnitDisplayOrder++;
					}
					//job.ProductOrdering.Add(jobProdInfo);
				}
				else
				{
					prodInfo.DisplayOrder = lastDisaply++;
					job.ProductOrdering.Add(prodInfo);
				}
			}

			db.SaveJob(job);
			foreach (Unit unit in jobUnits.Units)
			{
				unit.JobId = job.Id;
				db.SaveUnit(unit);
			}
			//GetCurrentJobModel() = JobHelper.CreateJobModel(GetCurrentJob().AppendJob(JobUtility.GetJobFromPSD(file.InputStream,
			//	 file.FileName)));

			return JobDataAndTreeHtml(job, units);
		}

		[HttpGet]
		public ActionResult ExportPSDAction(bool desireByteArray = false)
		{
			JobDataContext db = new JobDataContext();
			Job job = db.GetJobById(sessionManager.CurrentJobId);
			var units = db.GetUnits(sessionManager.CurrentJobId).ToList();
			JobUnits jobUnits = new JobUnits()
			{
				Job = job,
				Units = units
			};

			//send the finished list of xmls to TOPSSXMLPsd
			PSDGenerator psd = new PSDGenerator();
			string fileName = GetFileNameFromCurrentJob();
			fileName = fileName.Substring(0, fileName.Length - 4); // fileName.zip -> fileName
			var data = psd.GetPsdFromXml(jobUnits.ToXml(), fileName);
			FinishedDownload(true);
			if (!desireByteArray)
				return File(data, "application/x-zip-compressed", fileName + ".psd");
			return Json(data.Select(b => (int)b).ToArray()); //try int array...
		}

		public ActionResult ExportMultiplePSDAction(string[] jobIDs)
		{
			JobDataContext db = new JobDataContext();
			List<string> astrXml = new List<string>();
			foreach (string jobid in jobIDs)
			{
				ObjectId jobObject = new ObjectId(jobid);
				Job job = db.GetJobById(jobObject);
				var units = db.GetUnits(jobObject).ToList();
				JobUnits jobUnits = new JobUnits()
				{
					Job = job,
					Units = units
				};
				astrXml.AddRange(jobUnits.ToXml());
			}

			//send the finished list of xmls to TOPSSXMLPsd
			PSDGenerator psd = new PSDGenerator();
			string fileName = "Downloaded Jobs";

			var data = psd.GetPsdFromXml(astrXml, fileName);

			FinishedDownload(true);
			return File(data, "application/x-zip-compressed", fileName + ".psd");
		}

		[HttpPost]
		public ActionResult DeleteJob(ObjectId id)
		{
			JobDataContext db = new JobDataContext();
			db.DeleteJob(id);

			return new EmptyResult();
		}

		[HttpPost]
		public ActionResult DeleteCurrentJob()
		{
			DeleteJob(sessionManager.CurrentJobId);
			return CloseJob();
		}

		[HttpPost]
		public ActionResult DeleteMultipleJobs(string[] jobs)
		{
			foreach (string id in jobs)
			{
				DeleteJob(new ObjectId(id));
			}
			return new EmptyResult();
		}

		[HttpPost]
		public ActionResult LoadJobsDuplicate(string[] jobs)
		{
			List<Job> newJobs = Duplicate(jobs);

			string rowsHtml = "";

			foreach (Job job in newJobs)
			{
				rowsHtml += RenderPartialViewToString("LoadJobsTableRow", job);
			}

			return Json(rowsHtml);
		}

		List<Job> Duplicate(string[] jobs)
		{
			JobDataContext db = new JobDataContext();
			var creationDate = DateTime.Now;
			List<Job> newJobs = new List<Job>();

			foreach (string id in jobs)
			{
				Job oldJob = db.GetJobById(new ObjectId(id));
				var oldunits = db.GetUnits(oldJob.Id);

				Job newJob = new Job()
				{
					Id = ObjectId.GenerateNewId(),
					UserId = sessionManager.User.Id,
					ProductOrdering = oldJob.ProductOrdering,
					CreatedOn = creationDate,
					LastModifiedOn = creationDate,
					Name = oldJob.Name + " copy",
					Description = oldJob.Description,
					Customer = oldJob.Customer,
					Notes = oldJob.Notes,
					Project = oldJob.Project
				};

				foreach (Unit unit in oldunits)
				{
					Unit newUnit = (Unit)unit.Clone();
					newUnit.JobId = newJob.Id;
					db.SaveUnit(newUnit);
				}

				newJobs.Add(newJob);
				db.SaveJob(newJob);
			}
			return newJobs;
		}


		//[HttpPost]
		//public ActionResult Edit(JobDB jobToEdit)
		//{
		//    try
		//    {
		//        var origJob = (from j in _jobexplorer.JobDBs
		//                       where j.Job_Id == jobToEdit.Job_Id
		//                       select j).First();

		//        origJob.JobName = jobToEdit.JobName;
		//        //origJob.Insert_Date = jobToEdit.Insert_Date;
		//        origJob.Last_Modified_Date = DateTime.Now;
		//        origJob.Customer = jobToEdit.Customer;
		//        origJob.Description = jobToEdit.Description;
		//        origJob.Notes = jobToEdit.Notes;
		//        origJob.Project = jobToEdit.Project;
		//        origJob.Users_Id = jobToEdit.Users_Id;
		//        _jobexplorer.SubmitChanges();

		//        return RedirectToAction("Index");
		//    }
		//    catch (Exception ex)
		//    {
		//        return View();
		//    }
		//}

		//[HttpPost]
		//public ActionResult Delete(JobDB jobToDelete)
		//{
		//    try
		//    {
		//        var origJob = (from j in _jobexplorer.JobDBs
		//                       where j.Job_Id == jobToDelete.Job_Id
		//                       select j).First();

		//        _jobexplorer.JobDBs.DeleteOnSubmit(origJob);
		//        _jobexplorer.SubmitChanges();

		//        return RedirectToAction("Index");
		//    }
		//    catch
		//    {
		//        return View();
		//    }
		//}

		protected JobViewModel GetLoadJobModel(string filter = "")
		{
			var userId = sessionManager.User.Id;
			var db = new JobDataContext();
			bool isAdmin = false;
			if (new List<string> { "lbojn", "lahrk", "lbhra", "ircaej" }.Contains(sessionManager.User.Username))
				isAdmin = true;

			var model = new JobViewModel()
			{
				Jobs = db.GetJobsForUser(userId),
				ImagePathMap = ProductUtility.GetProductImageCache(),
				IsPriceEnabled = sessionManager.User.HasPriceAccess(),
				IsGenericEnabled = UserPermissionManager.GetUserRole(sessionManager.User.RoleId).Name.EqualsIgnoreCase("BU"),
				IsPriceAdmin = sessionManager.User.IsPriceAdmin,
				IsAdmin = isAdmin,
				TimeZone = MiscExtensions.GetTimeZoneById(sessionManager.User.Permissions.SelectedTimeZoneId)
			};

			return model;
		}


		[HttpGet]
		public ActionResult LoadJobs()
		{
			JobViewModel jobModel = GetLoadJobModel();
			ViewBag.TimeZone = jobModel.TimeZone;
			return View(jobModel);
		}

		//[HttpGet]
		//public ActionResult Filter(string filter)
		//{
		//	return View("LoadJobs", GetLoadJobModel(filter));
		//}

		[HttpGet]
		public ActionResult ViewJob()
		{
			return GetJob((job, units) =>
			{
				return new ContentResult()
				{
					Content = ViewJobHtml(sessionManager.CurrentJobId, job, units)
				};
			});
		}

		[HttpPost]
		public ActionResult CloseJob()
		{
			sessionManager.CurrentJobId = ObjectId.Empty;

			return new EmptyResult();  //twiga 11/19/13 - don't send the html, that will be handled by LoadJobs
		}

		//[HttpGet]
		//public ActionResult Edit(int id)
		//{
		//   var job = (from j in _jobexplorer.JobDBs
		//              where j.Job_Id == id
		//              select j).First();

		//   return View(job);
		//}

		//[HttpGet]
		//public ActionResult Delete(int id)
		//{
		//   var job = (from j in _jobexplorer.JobDBs
		//              where j.Job_Id == id
		//              select j).First();

		//   return View(job);
		//}

		[HttpGet]
		[AllowAnonymous]
		public ActionResult JobTree()
		{
			JobModel jobModel = GetCurrentJobModel();
			User user = sessionManager.User;
			if (jobModel != null && user != null)
			{
				jobModel.UserModel = new UserModel()
				{
					permissions = user.Permissions,
					User = user,
					Role = UserPermissionManager.GetUserRole(user.RoleId)
				};
			}
			return View(jobModel);
		}

		[HttpGet]
		public ActionResult TreeNode()
		{
			return View(GetCurrentJobModel());
		}

		//[HttpGet]
		//public ActionResult ModelNumber()
		//{
		//	//var jobModel = GetCurrentJobModel();

		//	//return View(jobModel);

		//	TopssDialogModel model = new TopssDialogModel()
		//	{
		//		RightButton = new Models.ButtonInfo()
		//		{
		//			Text = "Add",
		//			ClickFunction = "ParseModelNumber(document.getElementById('popupModelNumber').value);"
		//		},
		//		Fields = new List<Models.EditField>(){
		//			 new Models.EditField()
		//			 {
		//				  DialogType = Models.DialogType.TextField,
		//				  Text = "",
		//				  Label = "Enter Model Number",
		//				  EnterFunction = "ParseModelNumber(document.getElementById('popupModelNumber').value);",
		//				  ID = "popupModelNumber",
		//				  PlaceHolder = "Enter Model Number"
		//			 }
		//		  },
		//		TitleText = "Enter Model Number"
		//	};
		//	return PartialView("Dialog", model);
		//}

		[HttpGet]
		public ActionResult CreateJob()
		{

			TopssDialogModel model = new TopssDialogModel()
			{
				RightButton = new Models.ButtonInfo()
				{
					Text = "Continue",
					ID = "createJobContinue",
					ClickFunction = "createJobContinue();"
				},
				Fields = new List<Models.EditField>(){
                new Models.EditField()
                {
                    DialogType = Models.DialogType.TextField,
                    Text = "",
                    Label = "Job Name",
                    EnterFunction = "createJobContinue();",
                    ID = "createJobName",
                    PlaceHolder = "Enter Job Name"
                },
                new Models.EditField()
                {
                    DialogType = Models.DialogType.TextField,
                    Label = "Project",
                    ID = "createJobProject",
                    EnterFunction = "createJobContinue();",
                    PlaceHolder = "Enter Project"
                },
                new Models.EditField()
                {
                    DialogType = Models.DialogType.TextField,
                    Label = "Customer",
                    ID = "createJobCustomer",
                    EnterFunction = "createJobContinue();",
                    PlaceHolder = "Enter Customer"
                },
                new Models.EditField()
                {
                    DialogType = Models.DialogType.TextArea,
                    Label = "Description",
                    ID = "createJobDescription",
                    PlaceHolder = "Enter Description"
                },
                new Models.EditField()
                {
                    DialogType = Models.DialogType.TextArea,
                    Label = "Notes",
                    ID = "createJobNotes",
                    PlaceHolder = "Enter Notes"
                }
              },
				TitleText = "Job Details",
				FocusTabId = "createJobName",
				FirstTabId = "createJobName",
				LastTabId = "createJobContinue"
			};
			return PartialView("Dialog", model);
		}

		[HttpPost]
		public ActionResult UpdateJobProperty(ObjectId id, string propertyName, string value)
		{
			if (propertyName == null)
				return Error("Must provide propertyName");

			JobDataContext db = new JobDataContext();
			var job = db.GetJobById(id);
			var property = job.GetType().GetProperty(propertyName);

			if (property == null)
				return Error(string.Format("Invalid property name: {0}", propertyName));

			property.SetValue(job, value);

			var stop = Stopwatch.StartNew();

			db.SaveJob(job);

			stop.Stop();
			_logger.Trace("UpdateJobProperty saveJob call time: {0}", stop.ElapsedMilliseconds);

			return new EmptyResult();
		}

		[HttpGet]
		public ActionResult CreateOrderNumberSearch()
		{

			TopssDialogModel model = new TopssDialogModel()
			{
				LeftButton = new Models.ButtonInfo()
				{
					Text = "Search",
					ID = "leftButton"
					//ClickFunction = "onSubmitCreateJob();",
				},
				RightButton = new Models.ButtonInfo()
				{
					Text = "Continue",
					ID = "rightButton"
					//ClickFunction = "onSubmitOrderNumberSearchContinue(data.retOrderNumber, data.retModelNumber);"
				},
				Fields = new List<Models.EditField>(){
                new Models.EditField()
                {
                    DialogType = Models.DialogType.TextField,
                    Text = "",
                    Label = "Order Number",
                    EnterFunction = "",
                    ID = "createOrderNumber",
                    PlaceHolder = "Enter 15 Digit Order Number"
                },
                new Models.EditField()
                {
                    DialogType = Models.DialogType.TextField,
                    Label = "Model Number",
                    ID = "createModelNumber",
                    PlaceHolder = "Enter 40 Digit Model Number"
                },
                new Models.EditField()
                {
                   DialogType = Models.DialogType.TextArea,
                   Label = "Return",
                   ID = "returnText"
                }
              },
				TitleText = "Search Order/Model Number"
			};
			return PartialView("Dialog", model);
		}

		[HttpGet]
		//[OutputCache(NoStore = true, Location = OutputCacheLocation.None, Duration = 60, VaryByParam = "*")]
		/// See http://stackoverflow.com/questions/914027/disabling-browser-caching-for-all-browsers-from-asp-net
		public ActionResult JobData()
		{
			return Json(base.JobData());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="currentJobId"></param>
		/// <param name="job"></param>
		/// <param name="units"></param>
		/// <returns></returns>
		protected string ViewJobHtml(ObjectId currentJobId, Job job, IEnumerable<Unit> units)
		{
			var jobModel = job.ToJobModel(units);

			// Transform the UTC dates to local dates
			TimeZoneInfo timeZone = MiscExtensions.GetTimeZoneById(sessionManager.User.Permissions.SelectedTimeZoneId);
			jobModel.Job.CreatedOn = TimeZoneInfo.ConvertTimeFromUtc(jobModel.Job.CreatedOn, timeZone);
			jobModel.Job.LastModifiedOn = TimeZoneInfo.ConvertTimeFromUtc(jobModel.Job.LastModifiedOn, timeZone);

			// This is for the QS only users.
			if (sessionManager.User != null)
			{
				jobModel.UserModel = new UserModel()
				{
					Role = UserPermissionManager.GetUserRole(sessionManager.User.RoleId)
				};
			}

			return RenderPartialViewToString("ViewJob", jobModel);
		}

		protected string TreeHtml(Job job, IEnumerable<Unit> units)
		{
			JobModel jobModel = job.ToJobModel(units);
			User user = sessionManager.User;
			if (jobModel != null && user != null)
			{
				jobModel.UserModel = new UserModel()
				{
					permissions = user.Permissions,
					User = user,
					Role = UserPermissionManager.GetUserRole(user.RoleId)
				};
			}
			return RenderPartialViewToString("JobTree", jobModel);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected ActionResult ViewJobData(ObjectId currentJobId, Job job, IEnumerable<Unit> units)
		{
			return Json(new
			{
				jobData = base.JobData(),
				jobHtml = ViewJobHtml(currentJobId, job, units),
				treeHtml = TreeHtml(job, units)
			});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected ActionResult JobDataAndTreeHtml(Job job, IEnumerable<Unit> units)
		{
			return Json(new
			{
				jobData = base.JobData(),
				treeHtml = TreeHtml(job, units)
			});
		}

		[HttpPost]
		public ActionResult JobXmlImport(JobUnits jobUnits)
		{
			JobDataContext db = new JobDataContext();
			jobUnits.Job.UserId = sessionManager.User.Id;
			db.SaveJob(jobUnits.Job);
			sessionManager.CurrentJobId = jobUnits.Job.Id;
			foreach (Unit unit in jobUnits.Units)
			{
				db.SaveUnit(unit);
			}
			return ViewJobData(sessionManager.CurrentJobId, jobUnits.Job, jobUnits.Units);
		}
		[HttpPost]
		public ActionResult StartOrderNumber()
		{
			var partialModel = new DecisionTreeSolutions(new List<DecisionTreeSolution>())
			{
				DoNotShowDetails = true,
				EmptyMessage = "Type 6 or more digits of the Order Number to see results.",
			};
			TopssDialogModel model = new TopssDialogModel()
			{
				Fields = new List<EditField>(){
					new EditField(){
							  DialogType = DialogType.TextField,
							  PlaceHolder = "Enter Order Number",
							  Label = "Enter Order Number",
							  ID = "orderNumberText"
					},
					new EditField(){
									 DialogType = DialogType.PartialView,
									 PartialViewModel = partialModel,
									 PartialViewName = "../DecisionTree/SolutionList",
									 ID = "orderNumberView"
}
				},
				ModalSize = ModalSize.Medium,
				TitleText = "Order Number Finder"
			};

			return PartialView("Dialog", model);
		}
		[HttpPost]
		public ActionResult UpdateOrderNumber(string orderNumber)
		{
			List<PricePCG.MatchedPrice> orderModelnums = PricePCG.FindMatchingModelNumbers(orderNumber);
			List<DecisionTreeSolution> decisions = new List<DecisionTreeSolution>();
			string emptyMessage = "No order numbers match your search";
			if (orderNumber.Count() >= 6 && !orderNumber.Contains("%"))
			{
				foreach (PricePCG.MatchedPrice orderModelnum in orderModelnums)
				{
					decisions.Add(new DecisionTreeSolution()
					{
						Id = orderModelnum.ModelNumber,
						Name = orderModelnum.OrderNumber,
						Groups = new List<DecisionTreeSolutionValueGroup>(){
							new DecisionTreeSolutionValueGroup(){
								Name = "Model Number",
								Values = new List<DecisionTreeValue>(){
									new DecisionTreeValue(){
										DisplayPrecedence=1,
										Name = "Model Number",
										Value = orderModelnum.ModelNumber
									},
									new DecisionTreeValue(){
										DisplayPrecedence=2,
										Name = "Price",
										Value = orderModelnum.ListPrice < 0 ? "???" : String.Format("{0:C0}", orderModelnum.ListPrice)
									}
								}
							}
						}
					});
				}
			}
			else
			{
				if (orderNumber.Contains("%") && orderNumber.Count() < 6)
					emptyMessage += ", remember that you need to type at least 6 digits and cannot use '%' in your Order Number";
				else if (orderNumber.Count() < 6)
					emptyMessage += ", remember that you need to type at least 6 digits";
				else
					emptyMessage += ", you cannot use '%' in your Order Number";
			}
			var model = new DecisionTreeSolutions(decisions)
			{
				DoNotShowDetails = true,
				MethodName = "selectModelNumber",
				EmptyMessage = emptyMessage + "."
			};
			return PartialView("../DecisionTree/SolutionList", model);
		}

		public ActionResult SelectModelNumber(string modelNumber, bool newJob)
		{
			ActionResult jobData = newJob
				? AddNewJob(new AddNewJobInput()
				{
					Name = modelNumber.Substring(0, 11),
					Description = modelNumber,
					Customer = "",
					Notes = "",
					Project = ""
				})
				: GetJob((job, units) => { return ViewJobData(sessionManager.CurrentJobId, job, units); });

			List<string> products = UnitUtility.GetProductsForModelNumber(modelNumber, '*', true).ToList();
			if (products.Count() == 1)
			{
				Job job = GetCurrentJob();
				ProductDataJSON productData = DataCache.GetProductData(products.First());
				Unit newUnit = UnitUtility.CreateDefaultUnit(productData);
				ModuleMetadata moduleData = productData.DefaultModule;
				Module module = newUnit.Modules[0];
				module.SetFindResultType(FindResultType.Drawing, false);
				module.SetFindResultType(FindResultType.Performance, false);
				module.SetFindResultType(FindResultType.Price, true);
				UnitUtility.ApplyModelNumber(moduleData, module.Set, modelNumber);
				return Json(new { jobData = jobData, productData = PerformAddUnit(job.Id, newUnit) });
			}

			return PartialView("Dialog", new TopssDialogModel()
			{
				TitleText = "Could Not Find Matching Model Number",
			});
		}

		/// <summary>
		/// Webtopss Spectrum Integration 
		/// To Export Currently opened Webtopss job to .tof file
		/// </summary>
		/// <returns></returns>
		//[HttpGet]
		//public ActionResult ExportTOFAction()
		//{
		//   try
		//   {
		//      //Get the Currently opened JobId from Session Manager
		//      ObjectId objJobId = sessionManager.CurrentJobId;

		//      //Get the attribute list of string from TOPSSExportImportTof
		//      TOPSSExportImportTof tof = new TOPSSExportImportTof();
		//      var data = tof.GetTOFFromJob(objJobId);

		//      string fileName = GetFileNameFromCurrentJob();
		//      fileName = fileName.Substring(0, fileName.Length - 4);	// fileName.tof -> fileName	
		//      byte[] byteArray = Encoding.UTF8.GetBytes(data);			// convert string to stream
		//      MemoryStream memStream = new MemoryStream(byteArray);
		//      return File(memStream, "text/plain", fileName + ".tof");

		//   }
		//   catch (Exception ex)
		//   {
		//      throw ex;
		//   }



		//}

	}
}
