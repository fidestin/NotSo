using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TOPSS.Models;
using TOPSS.Utility;
//using TOPSS.Rules;
using System.IO;
using System.Web.Hosting;
using System.Xml.Linq;
using System.Configuration;
using System.Security.Cryptography;
using TOPSS.Rules;
using MongoDB.Bson;
using TOPSS.Common;
using TOPSS.DecisionTree.DTO;

namespace TOPSS.Utility
{
	public class SessionManager
	{
		private static class SessionKey
		{
			public static readonly string TopssUser = "TopssUser";
			public static readonly string TopssJobModel = "TopssJobModel";
			public static readonly string CurrentCriteria = "CurrentCriteria";
			public static readonly string UomUnits = "UomUnits";
			public static readonly string ProductDataJSON = "ProductDataJSON";
			public static readonly string Rules = "Rules";
			public static readonly string UOMScheme = "UOMScheme";
			public static readonly string Defibrillator = "Defibrillator";
			public static readonly string Products = "Products";
			public static readonly string Comparison = "Comparison";
			public static readonly string ComparisonChartModel = "ComparisonChartModel";
			public static readonly string MFUModels = "MFUModels";
			public static readonly string Permissions = "Permissions";
         public static readonly string ConfigQSS = "ConfigQSS";
			public static readonly string Shortlist = "Shortlist";
			public static readonly string RuleOutput = "RuleOutput";
			public static readonly string BIMTool = "BIMTool";
			public static readonly string DecisionTree = "DecisionTree";
			public static readonly string DecisionTreeUnitSolutions = "DecisionTreeUnitSolutions";
            public static readonly string PriceSource = "PriceSource";
		}

		public SessionManager(ISessionCache sessionCache)
		{
			_sessionCache = sessionCache;
		}

		/// <summary>
		/// Never returns null
		/// </summary>
		public ObjectId CurrentJobId
		{
			get
			{
				ObjectId currentJobId = _sessionCache.Get<ObjectId>(SessionKey.TopssJobModel);
				return currentJobId;
			}
			set
			{
				_sessionCache.Set(SessionKey.TopssJobModel, value);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public User User
		{
			get
			{
				var user = _sessionCache.Get<User>(SessionKey.TopssUser);
				return user;
			}
			set
			{
				_sessionCache.Set(SessionKey.TopssUser, value);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public DecisionTreeAnswers DecisionTreeAnswers
		{
			get 
			{
                return _sessionCache.Get<DecisionTreeAnswers>(SessionKey.DecisionTree); ; 
			}
			set 
			{
				_sessionCache.Set(SessionKey.DecisionTree, value);
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		public DTUnitSolutionsRepository DecisionTreeUnitSolutions
		{
			get 
			{
				return _sessionCache.Get<DTUnitSolutionsRepository>(SessionKey.DecisionTreeUnitSolutions); ; 
			}
			set 
			{
				_sessionCache.Set(SessionKey.DecisionTreeUnitSolutions, value);
			}
		}
		
		/// <summary>
		/// 
		/// </summary>
		public bool BIMTool
		{
			get
			{
				return _sessionCache.Get<bool>(SessionKey.BIMTool);
			}
			set
			{
				_sessionCache.Set<bool>(SessionKey.BIMTool, value);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		//public static Units.Units Units
		//{
		//	get
		//	{
		//		TOPSS.Units.Units units = _applicationCache.Get<TOPSS.Units.Units>(SessionKey.UomUnits);
		//		if (units == null)
		//		{
		//			units = ProductDetails.LoadUnits();
		//			_applicationCache.Set(SessionKey.UomUnits, units);
		//		}

		//		return units;
		//	}
		//	private set
		//	{

		//	}
		//}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="product"></param>
		/// <returns></returns>
		//private static string ProductDataPath(string product)
		//{
		//	return ProductDetails.TopssPath + product + Path.DirectorySeparatorChar;
		//}

		//public static UserPermissions UserPermissions(string userRole)
		//{
		//	string key = userRole + SessionKey.Permissions;
		//	UserPermissions userPermissions = _applicationCache.Get<UserPermissions>(key);
		//	if (userPermissions == null)
		//	{
		//		UserDetails userDetails = new UserDetails();
		//		var availibleProductKeys = new HashSet<string>(userDetails.GetAvailibleProductsForRole(userRole,
		//			ConfigurationManager.ConnectionStrings["TOPSSConnectionString"].ConnectionString));
				
		//		userPermissions = new UserPermissions()
		//		{
		//			AvailibleProducts = availibleProductKeys
		//		};

		//		_applicationCache.Set(key, userPermissions);
		//	}

		//	return userPermissions;
		//}

		public DateTime Defibrilator
		{
			get
			{
				return _sessionCache.Get<DateTime>(SessionKey.Defibrillator);
			}
			set
			{
				_sessionCache.Set<DateTime>(SessionKey.Defibrillator, value);
			}
		}

		/// <summary>
		/// The users current configured unit
		/// </summary>
		//public ConfigurableQSSearchModel ConfigurableQSSearch 
		//{ 
		//	get
		//	{
		//		return _sessionCache.Get<ConfigurableQSSearchModel>(SessionKey.ConfigQSS);
		//	}
		//	set
		//	{
		//		_sessionCache.Set<ConfigurableQSSearchModel>(SessionKey.ConfigQSS, value);
		//	}
		//}
				
		//public static IEnumerable<TOPSS.Arch.ComparisonData> ComparisonData()
		//{
		//	return null;
		//}
		
		
		/// <summary>
		/// TODO: remove the topss path initialization from here, put it somewhere else
		/// </summary>
		/// <param name="sessionCache"></param>
		/// <param name="applicationCache"></param>
		/// <param name="topssPath"></param>
		public static void Initialize(ISessionCache applicationCache, string topssPath)
		{
			_applicationCache = applicationCache;
			//ProductDetails.TopssPath = topssPath;

			// Initialize the faminfo data
		}

		public static string CacheFilePath
		{
			get
			{
				return _cacheFilePath;
			}
			set
			{
				_cacheFilePath = value;
			}
		}

		private static string _cacheFilePath;
		private ISessionCache _sessionCache;
		private static ISessionCache _applicationCache;
	}
}
