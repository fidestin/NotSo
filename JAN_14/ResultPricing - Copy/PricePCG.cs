﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oracle.DataAccess;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;
using System.Data;
using System.Configuration;
using System.Xml.Linq;
using System.Net.Mail;
using TOPSS.Common;
using TOPSS.ProdData;
using NLog;
using System.Globalization;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Diagnostics;



namespace TOPSS.Pricing
{
    

    public class Multiplier
    {
        public MongoDB.Bson.ObjectId _id { get; set; }
        public string costCenter { get; set; }

        public string multGroupCode { get; set; }

        public double tenBuyMult { get; set; }
        public double tenSellMult { get; set; }

        public double elevenBuyMult { get; set; }
        public double elevenSellMult { get; set; }

        public double twelveBuyMult { get; set; }
        public double twelveSellMult { get; set; }

        public double thirteenBuyMult { get; set; }
        public double thirteenSellMult { get; set; }

        public double fourteenBuyMult { get; set; }
        public double fourteenSellMult { get; set; }

        public string userUpdated { get; set; }
        public string dateUpdated { get; set; }

    }


	/// <summary>
	/// 
	/// </summary>
	public class PriceModelNumber
	{
		public string OrderNumber
		{
			get;
			set;
		}
		public string ModelNumber
		{
			get;
			set;
		}
		public string ProdCode
		{
			get;
			set;
		}
		public string DllKey
		{
			get;
			set;
		}
		public long ProductFamilyId
		{
			get;
			set;
		}
		public string Error
		{
			get;
			set;
		}
		public string MultGroupCode
		{
			get;
			set;
		}
		public double ExisintgListPrice
		{
			get;
			set;
		}
		public double CalculatedListPrice
		{
			get;
			set;
		}

		/// <summary>
		/// 
		/// </summary>
		public PriceModelNumber()
		{
			OrderNumber = string.Empty;
			ModelNumber = string.Empty;
			ProdCode = string.Empty;
			DllKey = string.Empty;
			ProductFamilyId = -1;
			Error = string.Empty;
			MultGroupCode = string.Empty;
			ExisintgListPrice = -1.0;
			CalculatedListPrice = -1.0;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public static class PricePCG
	{

		/// <summary>
		/// 
		/// </summary>
		static Logger _logger = LogManager.GetCurrentClassLogger();
        /// <summary>
		/// default to see if this helps the errors with timeouts
		/// </summary>
      private static string _connectionString;
      private static string _ddiaConnectionString;

      static bool useSMART = false;

      /// <summary>
      /// 
      /// </summary>
      public enum OrderNumberGenType
      {
         None,
         ModelNumber,
         Upfgs,
         Split
      };

		/// <summary>
		/// 
		/// </summary>

		public static void Initialize(string connectionString)
		{
			_connectionString = connectionString;
            _ddiaConnectionString = "User Id=diaread;Password=diareadpw;Data Source=ddia;";
		}

		/// <summary>
		/// 
		/// </summary>
		public static string webTOPSSServerId
		{
			get;
			set;
		}

		// 1. 
		/// <summary>
		/// 
		/// </summary>
		/// <param name="costCenter"></param>
		/// <param name="modNumber"></param>
		/// <param name="prodFamilyID"></param>
		/// <param name="multGroupCode"></param>
		/// <param name="priceCode"></param>
		/// <param name="roundingRule"></param>
		/// <returns></returns>
		public static float NetBuyPriceMultiplier(string costCenter, string modNumber, long prodFamilyID, string multGroupCode, out BuyPriceType priceCode, out  Int32 roundingRule)
		{
			priceCode = 0;
			roundingRule = 0;
			string paramValues = "PARAMS:\ncostCenter : " + costCenter + "\nmodNumber:" + modNumber + "\nprodFamilyID:" + prodFamilyID.ToString() + "\nmultGroupCode:" + multGroupCode;

            Debug.WriteLine(paramValues);

			OracleConnection cn = null;
			OracleCommand cmd = null;
			OracleDataReader reader = null;

			try
			{
				cn = new OracleConnection(_connectionString);

				double netBuyPrice_multiplier = 0.00;

				cmd = new OracleCommand();

				cmd.Connection = cn;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = "toppsproc.topssgetprice.netbuyprice";
				cmd.BindByName = true;
				cmd.CommandTimeout = 300;

				cn.Open();

				cmd.Parameters.Add("InCostCenter", OracleDbType.Varchar2, costCenter, ParameterDirection.Input);
				cmd.Parameters.Add("InModelNumber", OracleDbType.Varchar2, modNumber, ParameterDirection.Input);
				cmd.Parameters.Add("InProdFamilyID", OracleDbType.Varchar2, prodFamilyID.ToString(), ParameterDirection.Input);
				cmd.Parameters.Add("InMult_Group_Code", OracleDbType.Varchar2, multGroupCode, ParameterDirection.Input);

				//Change outputs from CURSOR to basic output vars
				OracleParameter p_netBuy = new OracleParameter("net_buy_price_multiplier", OracleDbType.Varchar2, 50, netBuyPrice_multiplier, ParameterDirection.Output);
				OracleParameter p_price = new OracleParameter("price_basis_code", OracleDbType.Varchar2, 50, priceCode, ParameterDirection.Output);
				OracleParameter p_rounding = new OracleParameter("rounding_rule_code", OracleDbType.Varchar2, 50, roundingRule, ParameterDirection.Output);

				cmd.Parameters.Add(p_netBuy);
				cmd.Parameters.Add(p_price);
				cmd.Parameters.Add(p_rounding);

				cmd.ExecuteNonQuery();

				priceCode = p_price.Value.ToString().Equals("SPECIFIC") ? BuyPriceType.Specific : BuyPriceType.Calculated;
				roundingRule = Convert.ToInt32(p_rounding.Value.ToString());

                Debug.WriteLine("\nBuyPrice Multiplier from PROA - " + p_netBuy.Value.ToString());
				return Convert.ToSingle(p_netBuy.Value.ToString());

			}
			catch (Exception ex)
			{
				if (costCenter == string.Empty || costCenter.ToLower() == "test" || costCenter.ToLower() == "wxyz" || costCenter.ToLower() == "abcd")
				{
					return 0.00f;
				}
				else
				{
					EmailException(ex, paramValues);
					return 0.00f;
				}
			}
			finally
			{
				if (reader != null)
					reader.Dispose();

				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}
		}


		// 2
		/// <summary>
		/// 
		/// </summary>
		/// <param name="modNumber"></param>
		/// <param name="configNum"></param>
		/// <param name="businessSegment"></param>
		/// <returns></returns>
		public static Int32 InsertProductLvl(string modNumber, string configNum, string businessSegment)
		{
			string paramValues = "PARAMS:\nmodNumber:" + modNumber + "\nconfigNum:" + configNum + "\nbusinessSegment:" + businessSegment;

			OracleConnection cn = null;
			OracleCommand cmd = null;
			OracleDataReader reader = null;

			try
			{
				cn = new OracleConnection(_connectionString);

				cmd = new OracleCommand();

				cmd.Connection = cn;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = "toppsproc.topssgetprice.insert_prodlvl";
				cmd.CommandTimeout = 300;

				cn.Open();

				var outRefCursor = new OracleParameter("OutInserted_ID", OracleDbType.RefCursor, ParameterDirection.Output);
				var inModel_Number = new OracleParameter("InModel_Number", OracleDbType.Varchar2, modNumber, ParameterDirection.Input);
				var inConfig_Number = new OracleParameter("InConfig_Product_Number", OracleDbType.Varchar2, configNum, ParameterDirection.Input);
				var inBusSegCode = new OracleParameter("InBusiness_Segment_Code", OracleDbType.Varchar2, businessSegment, ParameterDirection.Input);

				cmd.BindByName = true;

				cmd.Parameters.Add(outRefCursor);
				cmd.Parameters.Add(inModel_Number);
				cmd.Parameters.Add(inConfig_Number);
				cmd.Parameters.Add(inBusSegCode);

				Int32 newID = 0;
				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					//Console.WriteLine("level : {0}", reader["NN"].ToString());
					newID = Convert.ToInt32(reader["NN"]);

				}

				cmd.Parameters.Clear();

				outRefCursor.Dispose();
				inModel_Number.Dispose();
				inConfig_Number.Dispose();
				inBusSegCode.Dispose();

				return Convert.ToInt32(newID);
			}
			catch (Exception ex)
			{
				EmailException(ex, paramValues);
				return 0;
			}
			finally
			{
				if (reader != null)
					reader.Dispose();

				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}

		}

		// 3.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelId"></param>
		/// <param name="multGroupCode"></param>
		/// <param name="userID"></param>
		public static void InsertProdAssoc(Int32 modelId, string multGroupCode, string userID)
		{
			string paramValues = "PARAMS:\nmodelId : " + modelId.ToString() + "\nmultGroupCode:" + multGroupCode + "\nuserID:" + userID.ToString();

			OracleConnection cn = null;
			OracleCommand cmd = null;

			try
			{
				cn = new OracleConnection(_connectionString);

				cmd = new OracleCommand();

				cmd.Connection = cn;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = "toppsproc.topssgetprice.insert_prodassoc";
				cmd.CommandTimeout = 300;

				cn.Open();

				cmd.Parameters.Add("InProduct_lvl_id", OracleDbType.Int32, modelId, ParameterDirection.Input);
				cmd.Parameters.Add("InMult_Group_Code", OracleDbType.Varchar2, multGroupCode, ParameterDirection.Input);
				cmd.Parameters.Add("InMaint_user_code", OracleDbType.Varchar2, userID, ParameterDirection.Input);

				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				EmailException(ex, paramValues);
			}
			finally
			{
				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}

		}

		// 4.
		/// <summary>
		/// compares and only if necessary inserts...if there is a price for the same date (today) then STATUS_INDICATOR must be reset for older one(s).
		/// </summary>
		/// <param name="modelId"></param>
		/// <param name="listPice"></param>
		/// <param name="userID"></param>
		/// <param name="orgID"></param>
		public static void InsertOrUpdateListPrice(Int32 modelId, double listPice, string userID, Int32 orgID)
		{
			string paramValues = "PARAMS:\nmodelId : " + modelId.ToString() + "\nlistPice:" + listPice.ToString() + "\norgID:" + orgID.ToString();

			OracleConnection cn = null;
			OracleCommand cmd = null;

			try
			{
				cn = new OracleConnection(_connectionString);

				cmd = new OracleCommand();

				cmd.Connection = cn;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = "toppsproc.topssgetprice.insert_pricelvl";
				cmd.CommandTimeout = 300;

				cn.Open();

				cmd.Parameters.Add("InProduct_lvl_id", OracleDbType.Int32, modelId, ParameterDirection.Input);
				cmd.Parameters.Add("InTotalListPrice", OracleDbType.Double, listPice, ParameterDirection.Input);
				cmd.Parameters.Add("InMaint_user_code", OracleDbType.Varchar2, userID, ParameterDirection.Input);
				cmd.Parameters.Add("InOrg_id", OracleDbType.Int32, orgID, ParameterDirection.Input);

				cmd.ExecuteNonQuery();

			}
			catch (Exception ex)
			{
				EmailException(ex, paramValues);
			}
			finally
			{
				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}

		}




		// 5. 
		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelID"></param>
		/// <param name="usercode"></param>
		public static void InsertPriceBooks(Int32 modelID, string usercode)
		{
			string paramValues = "PARAMS:\nmodelId : " + modelID.ToString() + "\nusercode:" + usercode;

			OracleConnection cn = null;
			OracleCommand cmd = null;

			try
			{
				cn = new OracleConnection(_connectionString);

				cmd = new OracleCommand();

				cmd.Connection = cn;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = "toppsproc.topssgetprice.insert_pricelvl_books";
				cmd.CommandTimeout = 300;

				cn.Open();

				cmd.Parameters.Add("New_Model_id", OracleDbType.Varchar2, modelID, ParameterDirection.Input);
				cmd.Parameters.Add("Maint_user_code", OracleDbType.Varchar2, usercode, ParameterDirection.Input);

				cmd.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				EmailException(ex, paramValues);
			}
			finally
			{
				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}
		}

		//6
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static List<PriceModelNumber> GetMissingPrices()
		{

			List<PriceModelNumber> priceModelList = new List<PriceModelNumber>();

			OracleConnection cn = null;
			OracleCommand cmd = null;
			OracleDataReader reader = null;

			try
			{
				cn = new OracleConnection(_connectionString);
				cmd = new OracleCommand();

				cmd.Connection = cn;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = "toppsproc.topssgetprice.getMissingPrices";
				cmd.BindByName = true;
				cmd.CommandTimeout = 300;

				cn.Open();

				var outRefCursor = new OracleParameter("OutNoPrices", OracleDbType.RefCursor, ParameterDirection.Output);
				cmd.Parameters.Add(outRefCursor);

				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					PriceModelNumber priceModelNumber = new PriceModelNumber();
					priceModelNumber.OrderNumber = reader["MODEL_NUMBER"].ToString();
					priceModelNumber.ModelNumber = reader["MDP_SERVICE_MODEL_NUMBER"].ToString();
					priceModelNumber.ProdCode = reader["PROD_CODE"].ToString();
					priceModelNumber.ProductFamilyId = Convert.ToInt64(reader["PROD_FAMILY_ID"]);
					priceModelNumber.ExisintgListPrice = Convert.ToDouble(reader["MODEL_PRICE"]);

					priceModelList.Add(priceModelNumber);
				}

				return priceModelList;
			}

			catch (Exception ex)
			{
				EmailException(ex);
				return priceModelList;
			}
			finally
			{
				if (reader != null)
					reader.Dispose();

				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}
		}

		/// <summary>
		/// Added to allow Summary matrix be created based on baseUnit and Accessory multipliers that differ
		/// Returns a DataTable 
		/// </summary>
		/// <param name="matrixParam"></param>
		/// <param name="familyName"></param>
		/// <param name="costCentre"></param>
		/// <returns></returns>
		public static DataTable CreateSummaryTable(string matrixParam, string familyName, string costCenter)
		{
			DataTable summaryTable = new DataTable();

			try
			{
				DataColumn dcList = new DataColumn("List", typeof(Double));
				dcList.DefaultValue = 0;
				summaryTable.Columns.Add(dcList);

				DataColumn dcApprovedSell = new DataColumn("ASell", typeof(Double));
				summaryTable.Columns.Add(dcApprovedSell);

				DataColumn dcQuotedBuy = new DataColumn("QBuy", typeof(Double));
				summaryTable.Columns.Add(dcQuotedBuy);

				DataColumn dcRebate = new DataColumn("Rebate", typeof(double));
				dcRebate.DefaultValue = 0;
				summaryTable.Columns.Add(dcRebate);

				double[] buyTbl = new double[21];
				double[] sellTbl = new double[21];
				double[] rebateTbl = new double[21];

				double[] buyMultTbl = new double[21];
				double[] sellMultTbl = new double[21];

				double[] modBuyTbl = new double[21];
				double[] modSellTbl = new double[21];

				int x;
				int pos;

				double totalList;
				double totalNet;

				for (int i = 10; i < 15; i++)
				{
					buyTbl[i] = 0;
					sellTbl[i] = 0;
					rebateTbl[i] = 0;
				}

				totalList = 0;
				totalNet = 0;

				pos = 1;
				for (Int32 i = 0; i < 61; i++)
				{
					if (matrixParam.Substring(pos, 5) == "ZZZZZ")
					{
						break;
					}

					string model = matrixParam.Substring(pos - 1, 15).Replace(".", "");
					string capacity = matrixParam.Substring(pos - 1 + 3, 3).Replace(".", "");
					string phase = matrixParam.Substring(pos - 1 + 7, 1).Replace(".", "");

					double list = Convert.ToDouble(matrixParam.Substring(pos + 14, 8).Replace(".", ""));
					double net = Convert.ToDouble(matrixParam.Substring(pos + 22, 8).Replace(".", ""));

					totalList = totalList + list;
					totalNet = totalNet + net;

					for (int f = 10; f < 15; f++)
					{
						buyMultTbl[f] = 0;
						sellMultTbl[f] = 0;
						modBuyTbl[f] = 0;
						modSellTbl[f] = 0;
					}

					if (pos == 1)
					{
						DataTable theModeltable = PricePCG.BuildMatrixTable(costCenter, model, list, net);
						summaryTable = theModeltable;
					}
					else
					{
						DataTable otherTable = PricePCG.BuildMatrixTable(costCenter, model, list, net);
						int r = 0;
						foreach (DataRow matRow in summaryTable.Rows)                        //Add the 'other' prices to the summaryTable....
						{
							summaryTable.Rows[r][5] = Convert.ToDouble(matRow[5]) + Convert.ToDouble(otherTable.Rows[r][5]);
							summaryTable.Rows[r][6] = Convert.ToDouble(matRow[6]) + Convert.ToDouble(otherTable.Rows[r][6]);
							summaryTable.Rows[r][7] = Convert.ToDouble(matRow[7]) + Convert.ToDouble(otherTable.Rows[r][7]);
							r++;
						}
					}
					pos = pos + 31;
				}  //for
				return summaryTable;
			}

			catch (Exception ex)
			{

				return summaryTable;
			}

		}

		// 7.
		/// <summary>
		/// 
		/// </summary>
		/// <param name="percent_dollar_value"></param>
		/// <param name="rounding_code"></param>
		/// <returns></returns>
		public static float Effective_Price(float percent_dollar_value, Int32 rounding_code)
		{
			string paramValues = "PARAMS:\npercent_dollar_value : " + Convert.ToString(percent_dollar_value) + "\nrounding_code:" + Convert.ToString(rounding_code);

			OracleConnection cn = null;
			OracleCommand cmd = null;

			try
			{
				cn = new OracleConnection(_connectionString);
				cmd = new OracleCommand();

				cmd.Connection = cn;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = "Effective_Price_Func";
				cmd.CommandTimeout = 300;

				cn.Open();
				double RoundedPrice = 0.0;

				cmd.Parameters.Add("Price_V", OracleDbType.Double, RoundedPrice, ParameterDirection.ReturnValue);
				cmd.Parameters.Add("Price_P", OracleDbType.Double, percent_dollar_value, ParameterDirection.Input);
				cmd.Parameters.Add("RoundingRule_P", OracleDbType.Int32, rounding_code, ParameterDirection.Input);


				cmd.ExecuteNonQuery();

				RoundedPrice = Convert.ToDouble(cmd.Parameters["Price_V"].Value.ToString());

				return (float)RoundedPrice;
			}

			catch (Exception ex)
			{
				EmailException(ex, paramValues);
				return 0.00f;
			}
			finally
			{
				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}

		}

		/// <summary>
		/// Get the listed price for any model
		/// </summary>
		/// <param name="modelNumber">Input model Number</param>
		/// <param name="listPrice">Returns List Price</param>
		/// <returns></returns>
		public static double ModelPrice(string modelNumber, double listPrice)
		{
			string paramValues = "PARAMS:\nmodelNumber : " + modelNumber + "\nlistPrice:" + Convert.ToString(listPrice);

			string outputListPrice = listPrice.ToString();
			OracleConnection cn = null;
			OracleCommand cmd = null;
			try
			{
				cn = new OracleConnection(_connectionString);
				cmd = new OracleCommand();

				cmd.Connection = cn;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = "toppsproc.topssgetprice.getModelPrice";
				cmd.CommandTimeout = 300;
				cn.Open();

				cmd.Parameters.Add("modelNumber", OracleDbType.NVarchar2, modelNumber, ParameterDirection.Input);
				OracleParameter p_listPrice = new OracleParameter("modelPrice", OracleDbType.Varchar2, 50, outputListPrice, ParameterDirection.Output);
				cmd.Parameters.Add(p_listPrice);
				cmd.ExecuteNonQuery();

				return Convert.ToDouble(p_listPrice.Value.ToString()); ;
			}

			catch (Exception ex)
			{
				EmailException(ex, paramValues);
				return 0.00;
			}

			finally
			{
				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}
		}



		//public static string BuildDetailedParam(Result result, ProductDataJSON productData, string costCenter)
		//{
		//	string paramString = "";
		//	string familyName = productData.ProductName;
		//	try
		//	{
		//		string InModelNum = result.OrderNumber;

		//		string accessoryPrice = string.Empty;
		//		string slistPrice = result.BaseListPrice.ToString();
		//		string snetBuyPrice = result.BaseNetBuyPrice.ToString();
		//		double listPrice = (Int32)Math.Round(Convert.ToDouble(slistPrice), 0);       //Tidy up final list/net price for Matrix Parsing
		//		double netBuyPrice = (Int32)Math.Round(Convert.ToDouble(snetBuyPrice), 0);
		//		string priceMatrixParm = string.Empty;

		//		if (familyName.Contains("SSC2"))
		//		{
		//			foreach (KeyValuePair<int, string> pair in result.OrderNumbers)    //Splits PricedMatrix
		//			{
		//				//get the pricedField for this OrderNumber, then use the ListPrice, traverse PricedGroups
		//				PricedField pf = null;
		//				foreach (PricedGroup pricedGroup in result.PricedGroups)
		//				{
		//					pf = result.PricedGroups[0].PricedFields.FirstOrDefault(k => k.OrderNumber == pair.Value);
		//					if (pf != null)
		//						break;
		//				}

		//				double splitListPrice = pf.ListPrice;
		//				double splitBuyPrice = pf.NetPrice;
		//				priceMatrixParm += pair.Value.PadRight(15, '.') + splitListPrice.ToString().PadRight(8, '.') + splitBuyPrice.ToString().PadRight(8, '.');
		//			}
		//		}
		//		else  //regular PriceMatrix
		//		{
		//			priceMatrixParm = InModelNum.PadRight(15, '.') + listPrice.ToString().PadRight(8, '.') + netBuyPrice.ToString().PadRight(8, '.');
		//		}

		//		if (result.PricedGroups != null && productData != null)
		//		{
		//			foreach (PricedGroup pm in result.PricedGroups)
		//			{
		//				foreach (PricedField accy in pm.PricedFields)
		//				{
		//					if (!TOPSS.Utility.ModelNumber.IsFieldIncludedInModelNumber(accy.VpcId, productData.BaseModuleData))
		//					{
		//						if (accy.ListPrice != 0)   //check is required for splits
		//						{
		//							accessoryPrice = accy.OrderNumber + "...." + accy.ListPrice.ToString().PadRight(8, '.') + accy.NetPrice.ToString().PadRight(8, '.');
		//							priceMatrixParm += accessoryPrice;
		//						}
		//					}
		//				}
		//			}
		//		}

		//		priceMatrixParm += "ZZZZZZZZZZZZZZZ";
		//		paramString = priceMatrixParm;

		//		return paramString;
		//	}

		//	catch (Exception ex)
		//	{
		//		return paramString;
		//	}
		//}

		public static DataTable BuildSummaryMatrixTable(string InCostCenter, string InModelNum, double listPrice, double netBuyPrice)
		{
			DataTable testTable = loadMatrix(InCostCenter, InModelNum);

			double modBuyTbl = 0;
			double modSellTbl = 0;
			double buyTbl = 0;
			double sellTbl = 0;
			double buyMultTbl = 0;
			double sellMultTbl = 0;

			foreach (DataRow dRow in testTable.Rows)
			{
				buyTbl = 0;
				sellTbl = 0;
				buyMultTbl = Convert.ToDouble(dRow["BUY_MULT"].ToString());
				sellMultTbl = Convert.ToDouble(dRow["SELL_MULT"].ToString());

				modBuyTbl = (Int32)(buyMultTbl * listPrice + 0.5);
				modSellTbl = (Int32)(sellMultTbl * listPrice + 0.5);
				if (modBuyTbl > netBuyPrice)
				{
					modBuyTbl = netBuyPrice;
				}
				buyTbl = buyTbl + modBuyTbl;
				sellTbl = sellTbl + modSellTbl;

				dRow["ASell"] = sellTbl;
				dRow["QBuy"] = buyTbl;
				dRow["Rebate"] = netBuyPrice - buyTbl;
			}

			return testTable;
		}

		public static DataTable BuildMatrixTable(string InCostCenter, string InModelNum, double listPrice, double netBuyPrice)
		{
			DataTable testTable = loadMatrix(InCostCenter, InModelNum);

			DataColumn dcList = new DataColumn("List", typeof(Double));
			dcList.DefaultValue = listPrice;
			testTable.Columns.Add(dcList);

			DataColumn dcApprovedSell = new DataColumn("ASell", typeof(Double));
			testTable.Columns.Add(dcApprovedSell);

			DataColumn dcQuotedBuy = new DataColumn("QBuy", typeof(Double));
			testTable.Columns.Add(dcQuotedBuy);

			DataColumn dcRebate = new DataColumn("Rebate", typeof(double));
			dcRebate.DefaultValue = 0;
			testTable.Columns.Add(dcRebate);


			double modBuyTbl = 0;
			double modSellTbl = 0;
			double buyTbl = 0;
			double sellTbl = 0;
			double buyMultTbl = 0;
			double sellMultTbl = 0;

			foreach (DataRow dRow in testTable.Rows)
			{
				buyTbl = 0;
				sellTbl = 0;
				buyMultTbl = Convert.ToDouble(dRow["BUY_MULT"].ToString());
				sellMultTbl = Convert.ToDouble(dRow["SELL_MULT"].ToString());

				modBuyTbl = (Int32)(buyMultTbl * listPrice + 0.5);
				modSellTbl = (Int32)(sellMultTbl * listPrice + 0.5);
				if (modBuyTbl > netBuyPrice)
				{
					modBuyTbl = netBuyPrice;
				}
				buyTbl = buyTbl + modBuyTbl;
				sellTbl = sellTbl + modSellTbl;

				dRow["ASell"] = sellTbl;
				dRow["QBuy"] = buyTbl;
				dRow["Rebate"] = netBuyPrice - buyTbl;
			}

			return testTable;
		}


        /// <summary>
        /// Overloaded matrix load, of TOPSS Pricing
        /// </summary>
        /// <param name="inCostCentre"></param>
        /// <param name="inModel"></param>
        /// <returns></returns>
        public static DataTable loadMatrix(string inCostCentre, string inModel)
        {
            return loadMatrix(inCostCentre, inModel, true);
        }

		/// <summary>
		/// Loads the Price Matrix for a single model, using either PROA or SMART
		/// </summary>
		/// <param name="inCostCentre"></param>
		/// <param name="inModel"></param>
		/// <returns></returns>
		public static DataTable loadMatrix(string inCostCentre, string inModel,bool useSMARTPricing)
		{
             //BMCA Modified to user the SMART pricing
            if (useSMARTPricing)
            {
                return GetSingleSmartPricingMatrix(inCostCentre, inModel);
            }
            else
            {

                DataTable dt = new DataTable();

                OracleConnection cn = null;
                OracleCommand cmd = null;

                _logger.Trace(_connectionString);

                try
                {
                    cn = new OracleConnection(_connectionString);
                    cmd = new OracleCommand();

                    cmd.Connection = cn;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "TOPSSMATRIX.priceMatrix";
                    cmd.BindByName = true;
                    cmd.CommandTimeout = 300;

                    cn.Open();

                    cmd.Parameters.Add("outPriceMatrix", OracleDbType.RefCursor, ParameterDirection.Output);
                    cmd.Parameters.Add("inCostCentreCode", OracleDbType.Varchar2, inCostCentre, ParameterDirection.Input);
                    cmd.Parameters.Add("inModel", OracleDbType.Varchar2, inModel, ParameterDirection.Input);

                    OracleDataAdapter adapter = new OracleDataAdapter(cmd);
                    adapter.Fill(dt);

                    return dt;
                }
                catch (Exception ex)
                {
                    EmailException(ex);
                    return dt;
                }
                finally
                {
                    if (cn != null)
                        cn.Dispose();

                    if (cmd != null)
                        cmd.Dispose();
                }
            }
		}

		private static object _lock = new object();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="OrderNum"></param>
		/// <param name="ModelConfNum"></param>
		/// <param name="BusinessSegment"></param>
		/// <param name="multGroupCode"></param>
		/// <param name="userID"></param>
		/// <param name="TOPSSListPrice"></param>
		public static void UpdateTOPSSWithPricingData(string OrderNum, string ModelConfNum, string BusinessSegment, string multGroupCode, string userID, double TOPSSListPrice)
		{
			lock (_lock)
			{
				string paramValues = "PARAMS:\nOrderNum : " + OrderNum + "\nModelConfNum:" + ModelConfNum + "\nBusinessSegment:" + BusinessSegment + "\nuserID:" + userID + Convert.ToString(TOPSSListPrice);

				try
				{
					Int32 newModelID;
					newModelID = InsertProductLvl(OrderNum, ModelConfNum, BusinessSegment);            //Insert new model or returns ID if exists

					InsertProdAssoc(newModelID, multGroupCode, userID);                                //Insert into assoc table

					Int32 orgID = 4701;                                         							//Pricing Org

					//BMCA TODO : is this update MODEL price with the MODEL+Accessory list Price?
					InsertOrUpdateListPrice(newModelID, TOPSSListPrice, userID, orgID);                //Insert TOPSS List Price

					InsertPriceBooks(newModelID, userID);                                          	//Update other price books
				}
				catch (Exception ex)
				{
					EmailException(ex, paramValues);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="costCenter"></param>
		/// <param name="prodUserTag"></param>
		/// <param name="prodFamilyID"></param>
		/// <returns></returns>
		public static float AccessoryBuyPriceMultiplier(string costCenter, string accyProdTag, string prodFamilyID)
		{
			string paramValues = "PARAMS:\ncostCenter:" + costCenter + "\naccyProdTag:" + accyProdTag + "\nprodFamilyID:" + prodFamilyID;

			OracleConnection cn = null;
			OracleCommand cmd = null;
			OracleDataReader reader = null;

			try
			{
				if (costCenter == string.Empty || costCenter.ToLower() == "test" || costCenter.ToLower() == "wxyz" || costCenter.ToLower() == "abcd")
					return 0.0f;//no need to query the database if we don't have a valid costcenter...

				cn = new OracleConnection(_connectionString);

				float netBuyPrice_multiplier = 0.0f;

				cmd = new OracleCommand();

				cmd.Connection = cn;
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = "toppsproc.TOPSSGETACCYPRICE.accynetbuyprice";
				cmd.BindByName = true;
				cmd.CommandTimeout = 300;

				cn.Open();


				cmd.Parameters.Add("OutNetBuyPriceMult", OracleDbType.RefCursor, ParameterDirection.Output);
				cmd.Parameters.Add("InCostCenter", OracleDbType.Varchar2, costCenter, ParameterDirection.Input);
				cmd.Parameters.Add("inProdUserTag", OracleDbType.Varchar2, accyProdTag, ParameterDirection.Input);
				cmd.Parameters.Add("InProdFamilyID", OracleDbType.Varchar2, prodFamilyID, ParameterDirection.Input);

				reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					// Console.WriteLine("level : {0}", reader["NET_BUY_PRICE_MULTIPLIER"].ToString());
					netBuyPrice_multiplier = Convert.ToSingle(reader["NET_BUY_PRICE_MULTIPLIER"]);
				}

				return netBuyPrice_multiplier;
			}
			catch (Exception ex)
			{
				EmailException(ex, paramValues);
				return 0.00f;
			}
			finally
			{
				if (reader != null)
					reader.Dispose();

				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}
		}



        /// <summary>
        /// Get the R12_PRICING_CATEGORY (Multiplier Group)
        /// for that model, in Product Master table.
        /// </summary>
        /// <param name="modelNumber">Model or Accessory Number</param>
        /// <returns></returns>
        public static string FindSMARTMultiplierGroup(string modelNumber)
        {
            string returnData = string.Empty;
            string paramValues = "PARAMS:\n FindSMARTMultiplierGroup : \nmodelNumber:" + modelNumber + "\n";

            OracleConnection cn = null;
            OracleCommand cmd = null;
            OracleDataReader reader = null;

            try
            {
                cn = new OracleConnection(_ddiaConnectionString);
				cn.Open();
				cmd = new OracleCommand();
				cmd.Connection = cn;
				string query = string.Empty;

                query = string.Format("select substr(R12_PRICING_CATEGORY,-4,4) MULTGROUP from fdpdb_prod_mast_dn P where MODEL_NUMBER='{0}'", modelNumber);

                cmd.CommandText = query;
				cmd.CommandType = System.Data.CommandType.Text;

				reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					string retMultGroup = reader["MULTGROUP"] as string;
					if (retMultGroup != string.Empty && retMultGroup != null)
					{
						returnData=retMultGroup;
					}
				}


            }

            catch (Exception ex)
            {
                EmailException(ex, paramValues);
                return returnData;
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();

                if (cn != null)
                    cn.Dispose();

                if (cmd != null)
                    cmd.Dispose();
            }

            return returnData;

        }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="ordering"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		public static List<KeyValuePair<string, string>> FindMatchingOrderingOrModelNumber(string ordering, string model)
		{
			List<KeyValuePair<string, string>> returnData = new List<KeyValuePair<string, string>>();

			//Select ordering_number, lynx_service_model_number From fdpde_ordering_number Where ordering_number in (Your list of 15 digit models)
			string paramValues = "PARAMS:\nordering number : " + ordering + "\nmodNumber:" + model + "\n";

			OracleConnection cn = null;
			OracleCommand cmd = null;
			OracleDataReader reader = null;

			try
			{
				cn = new OracleConnection(_connectionString);
				cn.Open();

				cmd = new OracleCommand();

				cmd.Connection = cn;

				string query = string.Empty;
				//if(ordering != null)
				//   query = string.Format("Select ordering_number, lynx_service_model_number From fdpde_ordering_number Where ordering_number in '{0}'", ordering);
				//else
				//   query = string.Format("Select ordering_number, lynx_service_model_number From fdpde_ordering_number Where lynx_service_model_number in '{0}'", model);
				if (ordering != null && ordering != string.Empty)
					query = string.Format("select model_number,config_product_number from product_lvl where model_number in '{0}'", ordering);
				else
					query = string.Format("select model_number,config_product_number from product_lvl where config_product_number in '{0}'", model);

				cmd.CommandText = query;
				cmd.CommandType = System.Data.CommandType.Text;

				reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					string retordering = reader["MODEL_NUMBER"] as string;
					string retmodel = reader["CONFIG_PRODUCT_NUMBER"] as string;
					if (retordering != null && retordering != string.Empty && retmodel != null && retmodel != string.Empty)
					{
						KeyValuePair<string, string> pair = new KeyValuePair<string, string>(retordering, retmodel);

						returnData.Add(pair);
					}
				}

			}
			catch (Exception ex)
			{
				EmailException(ex, paramValues);
				return new List<KeyValuePair<string, string>>();
			}
			finally
			{
				if (reader != null)
					reader.Dispose();

				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}

			return returnData;
		}


        public static DataTable ToDictionary(Dictionary<string, List<Tuple<short, float, float>>> inputSMART)
        {
            try
            {
                DataTable dt = new DataTable();

                foreach (var item in inputSMART)
                {
                    var row = dt.NewRow();

                }


                return dt;

            }
            catch (Exception ex)
            {
                return new DataTable();
            }
            
        }

        /// <summary>
        /// Strips the last character '*' from the Order/Model Number returned from SMART Pricing Interface
        /// </summary>
        /// <param name="smartModelNumber"></param>
        /// <returns></returns>
        public static string TOPSSModelNumberFromSMART(string smartModelNumber)
        {
            if (smartModelNumber.Substring(smartModelNumber.Length - 1) == "*")
            {
                return smartModelNumber.Substring(0, smartModelNumber.Length - 1);
            }
            else
                return smartModelNumber;
        }

        public static DataTable GetSingleSmartPricingMatrix(string costCenter, string modelNumber)
        {
            try
            {
                string connectionString = "mongodb://localhost/TOPSS";
                var database = MongoDB.Driver.MongoDatabase.Create(connectionString);
                var mcollection = database.GetCollection<Multiplier>(typeof(Multiplier).Name);

                string R12GroupCode = FindSMARTMultiplierGroup(TOPSSModelNumberFromSMART(modelNumber));

                var query = MongoDB.Driver.Builders.Query.And(
                            MongoDB.Driver.Builders.Query.EQ("costCenter", costCenter),
                            MongoDB.Driver.Builders.Query.EQ("multGroupCode", R12GroupCode)
                        );


                var list = mcollection.FindAs<Multiplier>(query);
                DataTable dtMatrix = new DataTable();
                dtMatrix.Columns.Add("MULT_GROUP_CODE",typeof(string));
                dtMatrix.Columns.Add("MARGIN_PERCENT", typeof(short));
                dtMatrix.Columns.Add("SELL_MULT", typeof(float));
                dtMatrix.Columns.Add("BUY_MULT", typeof(float));
                
                foreach (var mult in list)
                {
                    dtMatrix.Rows.Add(mult.multGroupCode, "10", (float)mult.tenSellMult, (float)mult.tenBuyMult);
                    dtMatrix.Rows.Add(mult.multGroupCode, "11", (float)mult.elevenSellMult, (float)mult.elevenBuyMult);
                    dtMatrix.Rows.Add(mult.multGroupCode, "12", (float)mult.twelveSellMult, (float)mult.twelveBuyMult);
                    dtMatrix.Rows.Add(mult.multGroupCode, "13", (float)mult.thirteenSellMult, (float)mult.thirteenBuyMult);
                    dtMatrix.Rows.Add(mult.multGroupCode, "14", (float)mult.fourteenSellMult, (float)mult.fourteenBuyMult);
                }
                return dtMatrix;
            }
            catch (Exception ex)
            {
                //do stuff
                return new DataTable();
            }
        }

        /// <summary>
        /// Loads the SMART Multipliers for a single Cost Center (50+ Multipliers)
        /// </summary>
        /// <param name="costCenter"></param>
        /// <returns></returns>
        public static Dictionary<string, List<Tuple<short, float, float>>> GetSmartPricingMatrix(string costCenter)
        {
            try
            {
                string connectionString = "mongodb://localhost/TOPSS";
                var database = MongoDB.Driver.MongoDatabase.Create(connectionString);
                var mcollection = database.GetCollection<Multiplier>(typeof(Multiplier).Name);
                var query = MongoDB.Driver.Builders.Query.EQ("costCenter", costCenter);

                var list = mcollection.FindAs<Multiplier>(query);
                var matrix = new Dictionary<string, List<Tuple<short, float, float>>>();
                string multGroupCode = string.Empty;
                foreach (var mult in list)
                {
                    multGroupCode = mult.multGroupCode;
                    matrix[multGroupCode] = new List<Tuple<short, float, float>>() { new Tuple<short, float, float>(10, (float)mult.tenBuyMult, (float)mult.tenSellMult) };
                    matrix[multGroupCode].Add(new Tuple<short, float, float>((short)11, (float)mult.elevenBuyMult, (float)mult.elevenSellMult));
                    matrix[multGroupCode].Add(new Tuple<short, float, float>((short)12, (float)mult.twelveBuyMult, (float)mult.twelveSellMult));
                    matrix[multGroupCode].Add(new Tuple<short, float, float>((short)13, (float)mult.thirteenBuyMult, (float)mult.thirteenSellMult));
                    matrix[multGroupCode].Add(new Tuple<short, float, float>((short)14, (float)mult.fourteenBuyMult, (float)mult.fourteenSellMult));
                }
                return matrix;
            }
            catch (Exception ex)
            {
                //do stuff
                return new Dictionary<string, List<Tuple<short, float, float>>>();
            }
        }

        public class Multiplier
        {
            public MongoDB.Bson.ObjectId _id { get; set; }
            public string costCenter { get; set; }

            public string multGroupCode { get; set; }

            public double tenBuyMult { get; set; }
            public double tenSellMult { get; set; }

            public double elevenBuyMult { get; set; }
            public double elevenSellMult { get; set; }

            public double twelveBuyMult { get; set; }
            public double twelveSellMult { get; set; }

            public double thirteenBuyMult { get; set; }
            public double thirteenSellMult { get; set; }

            public double fourteenBuyMult { get; set; }
            public double fourteenSellMult { get; set; }

            public string userUpdated { get; set; }
            public string dateUpdated { get; set; }

        }

        public static void LoadDataIntoMongoDB(string costCenter,Dictionary<string, List<Tuple<short, float, float>>> opMatrix)
        {
            try
            {
                string connectionString = "mongodb://localhost/TOPSS";
                var database = MongoDB.Driver.MongoDatabase.Create(connectionString);
                var mCollection = database.GetCollection<Multiplier>(typeof(Multiplier).Name);
                

                foreach (KeyValuePair<string,List<Tuple<short, float, float>>> rec in opMatrix)
                {
                    string multGroupCode = rec.Key;
                    var result = rec.Value.OrderBy(x => x.Item1).ToList();
                    var cMult = new Multiplier
                    {
                        costCenter=costCenter,
                        multGroupCode=rec.Key,
                        tenBuyMult=result[0].Item2,
                        tenSellMult=result[0].Item3,
                        
                        elevenBuyMult=result[1].Item2,
                        elevenSellMult=result[1].Item3,
                        
                        twelveBuyMult=result[2].Item2,
                        twelveSellMult=result[2].Item3,

                        thirteenBuyMult=result[3].Item2,
                        thirteenSellMult=result[3].Item3,

                        fourteenBuyMult=result[4].Item2,
                        fourteenSellMult=result[4].Item3,

                        userUpdated="topssLoader",
                         dateUpdated = DateTime.Now.ToShortDateString()

                    };

                    mCollection.Insert(cMult);
                }
            }
            catch (Exception ex)
            {
                //do stuff
            }
        }


        public static void LoadAllCostCenterMultipliers()
        {
            //Load all the costCenters in a list from the database,

            OracleConnection cn = null;
			OracleCommand cmd = null;
			OracleDataReader reader = null;

			
            try
            {
                cn = new OracleConnection(_connectionString);
                cn.Open();

                cmd = new OracleCommand();

                cmd.Connection = cn;
                string query = string.Format("select distinct cost_center_code from pricing_matrix");

                cmd.CommandText = query;
                cmd.CommandType = System.Data.CommandType.Text;

                reader = cmd.ExecuteReader();
                var matrix = new Dictionary<string, List<Tuple<short, float, float>>>();
                while (reader.Read())
                {
                    //ste thru each one nad call GetPricingMatrix
                    // -> this will then call the LoadIntoMongoDB
                    string cost_center=reader["cost_center_code"].ToString();
                    matrix = GetPricingMatrix(cost_center);
                    LoadDataIntoMongoDB(cost_center, matrix);
                }
            }
            catch (Exception ex)
            {
                //do stuff
            }
        }

        
		/// <summary>
		/// Loads all the multipliers (50+) for a single cost center into a overview -using either PROA or SMART
		/// </summary>
		/// <param name="ordering"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		public static Dictionary<string, List<Tuple<short, float, float>>> GetPricingMatrix(string costcenter)
		{
            //BMCA Modified to user the SMART pricing
            if (useSMART)
            {
                return GetSmartPricingMatrix(costcenter);
            }
            else
            {
                List<KeyValuePair<string, string>> returnData = new List<KeyValuePair<string, string>>();

                //Select ordering_number, lynx_service_model_number From fdpde_ordering_number Where ordering_number in (Your list of 15 digit models)
                string paramValues = "PARAMS:\nCost Center Code: " + costcenter + "\n";

                OracleConnection cn = null;
                OracleCommand cmd = null;
                OracleDataReader reader = null;

                var matrix = new Dictionary<string, List<Tuple<short, float, float>>>();
                try
                {
                    cn = new OracleConnection(_connectionString);
                    cn.Open();

                    cmd = new OracleCommand();

                    cmd.Connection = cn;
                    string query = string.Format("SELECT MULT_GROUP_CODE, MARGIN_PERCENT, SELL_MULT, BUY_MULT FROM PCG.PRICING_MATRIX WHERE (STATUS_INDICATOR = '1') AND (COST_CENTER_CODE = '{0}')", costcenter);

                    cmd.CommandText = query;
                    cmd.CommandType = System.Data.CommandType.Text;

                    reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        string multGroup = reader["MULT_GROUP_CODE"] as string;
                        short? percent = reader["MARGIN_PERCENT"] as short?;
                        float? sellVal = reader["SELL_MULT"] as float?;
                        float? buyVal = reader["BUY_MULT"] as float?;
                        if (multGroup != null && multGroup != string.Empty && percent != null && percent >= 10 && percent <= 14 &&
                             sellVal != null && sellVal > 0 && sellVal < 1 && buyVal != null && buyVal > 0 && buyVal < 1)
                        {
                            if (matrix.ContainsKey(multGroup))
                            {
                                matrix[multGroup].Add(new Tuple<short, float, float>((short)percent, (float)sellVal, (float)buyVal));
                            }
                            else
                            {
                                matrix[multGroup] = new List<Tuple<short, float, float>>() { new Tuple<short, float, float>((short)percent, (float)sellVal, (float)buyVal) };
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    EmailException(ex, paramValues, "simon.weisse@trane.com");
                    return new Dictionary<string, List<Tuple<short, float, float>>>();
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();

                    if (cn != null)
                        cn.Dispose();

                    if (cmd != null)
                        cmd.Dispose();
                }

                return matrix;
            }
		}

		static List<KeyValuePair<string, string>> _costCenterList = null;

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public static List<KeyValuePair<string, string>> GetCostCenterList()
		{
			if (_costCenterList == null)
			{
				//SELECT a.cost_center_code,a.cost_center_Name FROM COST_CENTER a, org_lvl b  
				//WHERE a.cost_center_code = b.cost_center_code 
				//and b.org_lvl_type_code in ('DSO','IWD') 
				//AND A.INTERFACE_INDICATOR = 2   
				//order by A.COST_CENTER_NAME, A.COST_CENTER_Code

				_costCenterList = new List<KeyValuePair<string, string>>();

				//Select ordering_number, lynx_service_model_number From fdpde_ordering_number Where ordering_number in (Your list of 15 digit models)
				//string paramValues = "PARAMS:\nordering number : " + ordering + "\nmodNumber:" + model + "\n";

				OracleConnection cn = null;
				OracleCommand cmd = null;
				OracleDataReader reader = null;

				try
				{
					cn = new OracleConnection(_connectionString);
					cn.Open();

					cmd = new OracleCommand();

					cmd.Connection = cn;

					string query = "SELECT a.cost_center_code,a.cost_center_Name FROM COST_CENTER a, org_lvl b WHERE a.cost_center_code = b.cost_center_code and b.org_lvl_type_code in ('DSO','IWD') AND A.INTERFACE_INDICATOR = 2 order by A.COST_CENTER_NAME, A.COST_CENTER_Code";

					cmd.CommandText = query;
					cmd.CommandType = System.Data.CommandType.Text;

					reader = cmd.ExecuteReader();

					while (reader.Read())
					{
						string costCenterCode = reader["COST_CENTER_CODE"] as string;
						string costCenterName = reader["COST_CENTER_NAME"] as string;
						costCenterName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(costCenterName.ToLower());
						if (costCenterCode != null && costCenterCode != string.Empty && costCenterName != null && costCenterName != string.Empty)
						{
							KeyValuePair<string, string> pair = new KeyValuePair<string, string>(costCenterCode, costCenterName);

							_costCenterList.Add(pair);
						}
					}

				}
				catch (Exception ex)
				{
					EmailException(ex, string.Empty);
					return new List<KeyValuePair<string, string>>();
				}
				finally
				{
					if (reader != null)
						reader.Dispose();

					if (cn != null)
						cn.Dispose();

					if (cmd != null)
						cmd.Dispose();
				}
			}

			return _costCenterList;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ex"></param>
		/// <param name="parameterData"></param>
		public static void EmailException(Exception ex, string parameterData = "NO PARAM DATA", string additionalSendTo = "")
		{
			try
			{
				string error = "\n" + parameterData + "\n\n-------------------------\nException Message: " + ex.Message + " \n\n-------------------------\nStack: " + ex.StackTrace;

				_logger.Error(error);
				//email file
            //SmtpClient sc = new SmtpClient();

            //MailMessage mm = new MailMessage();

            //mm.From = new MailAddress("webtopss@trane.com");
            //mm.To.Add(new MailAddress("breinhart@trane.com"));
            //mm.To.Add(new MailAddress("brendan_mcardle@eu.irco.com"));
            //if (additionalSendTo != "")
            //   mm.To.Add(new MailAddress(additionalSendTo));

            //if (webTOPSSServerId == null)
            //   webTOPSSServerId = string.Empty;

            //mm.Subject = "webTOPSS PricePCG Error MVC " + webTOPSSServerId;
            ////mm.Body = "\n" + parameterData + "\n\n-------------------------\nException Message: " + ex.Message + " \n\n-------------------------\nStack: " + ex.StackTrace;
            //mm.Body = error;

            //sc.Host = "smtp_host";
            //sc.UseDefaultCredentials = true;

            //sc.Send(mm);
			}
			catch (Exception ex2)
			{

			}
		}

		public static List<Tuple<string, string, double>> FindMatchingOrderingOrModelNumbers(string ordering, int returnLength = 100)
		{
			List<Tuple<string, string, double>> returnData = new List<Tuple<string, string, double>>();
			if (ordering == null || ordering.Count() < 6 || ordering.Contains("%")) // Make sure that the query won't take forever, and doesn't contain wildcards.
				return returnData;

			//Select ordering_number, lynx_service_model_number From fdpde_ordering_number Where ordering_number in (Your list of 15 digit models)
			string paramValues = "PARAMS:\nordering number : " + ordering + "\n";

			OracleConnection cn = null;
			OracleCommand cmd = null;
			OracleDataReader reader = null;

			try
			{
				cn = new OracleConnection(_connectionString);
				cn.Open();

				cmd = new OracleCommand();

				cmd.Connection = cn;

				string query = string.Empty;
				if (ordering != null && ordering != string.Empty)
					query = string.Format("select model_number,config_product_number from product_lvl where model_number like '%{0}%'", ordering.ToUpper());

				cmd.CommandText = query;
				cmd.CommandType = System.Data.CommandType.Text;

				reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					string retordering = reader["MODEL_NUMBER"] as string; //This is the Order Number.
					string retmodel = reader["CONFIG_PRODUCT_NUMBER"] as string; // This is the Model Number.
					if (retordering != null && retordering != string.Empty && retmodel != null && retmodel != string.Empty)
					{
						Tuple<string, string, double> tuple = new Tuple<string, string, double>(retordering, retmodel, /*ModelPrice(retordering, -1)*/ 0.0);

						returnData.Add(tuple);
					}
				}

			}
			catch (Exception ex)
			{
				//EmailException(ex, paramValues);
				return new List<Tuple<string, string, double>>() /*{ new KeyValuePair<string, string>("YSC048A3EMA005D", "YSC048A3EMA**0001000000000000A0000000000") }*/;//uncomment if you want a test.
			}
			finally
			{
				if (reader != null)
					reader.Dispose();

				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}

			return returnData.Take(returnLength).ToList();
		}





		public class MatchedPrice
		{
			public string ModelNumber { get; set; }
			public string OrderNumber { get; set; }
			public double ListPrice { get; set; }
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="ordering"></param>
		/// <param name="model"></param>
		/// <returns></returns>
		public static List<MatchedPrice> FindMatchingModelNumbers(string searchModel, int rowsReturned = 100)
		{

			List<MatchedPrice> returnData = new List<MatchedPrice>();

			//Select ordering_number, lynx_service_model_number From fdpde_ordering_number Where ordering_number in (Your list of 15 digit models)
			string paramValues = "PARAMS:\nsearchModel : " + searchModel + "\nrowsReturned:" + rowsReturned + "\n";

			OracleConnection cn = null;
			OracleCommand cmd = null;
			OracleDataReader reader = null;

			try
			{
				cn = new OracleConnection(_connectionString);
				cn.Open();
				cmd = new OracleCommand();
				cmd.Connection = cn;

				cmd.CommandText = "toppsproc.topssgetprice.findMatchingModels";
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.BindByName = true;


				cmd.Parameters.Add("outModelsMatched", OracleDbType.RefCursor, ParameterDirection.Output);
				cmd.Parameters.Add("modelSearch", OracleDbType.Varchar2, searchModel.ToUpper(), ParameterDirection.Input);
				cmd.Parameters.Add("numRows", OracleDbType.Int16, rowsReturned, ParameterDirection.Input);

				reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					string retOrderNumber = reader["MODEL_NUMBER"].ToString();
					string retModelNumber = reader["CONFIG_PRODUCT_NUMBER"].ToString();
					double retlistPrice = -1;
					try
					{
						retlistPrice = Convert.ToDouble(reader["LISTPRICE"].ToString());
					}
					catch (Exception e)
					{
						retlistPrice = -1;
					}

					if (retOrderNumber != null && retOrderNumber != string.Empty && retModelNumber != null && retModelNumber != string.Empty && retlistPrice != null)
					{
						MatchedPrice matchedPrice = new MatchedPrice()
						{
							OrderNumber = retOrderNumber,
							ModelNumber = retModelNumber,
							ListPrice = retlistPrice
						};
						returnData.Add(matchedPrice);
					}
				}

			}
			catch (Exception ex)
			{
				EmailException(ex, paramValues);
				return new List<MatchedPrice>();
			}
			finally
			{
				if (reader != null)
					reader.Dispose();

				if (cn != null)
					cn.Dispose();

				if (cmd != null)
					cmd.Dispose();
			}

			return returnData;
		}


      public static List<PriceModelNumber> GetAllModeslToPrices()
      {

         List<PriceModelNumber> priceModelList = new List<PriceModelNumber>();

         OracleConnection cn = null;
         OracleCommand cmd = null;
         OracleDataReader reader = null;

         try
         {
            cn = new OracleConnection(_connectionString);
            cn.Open();

            cmd = new OracleCommand();

            cmd.Connection = cn;
            //string query = "select model_number,config_product_number from product_lvl where config_product_number like '%VA**%'";
            //string query = "select model_number,config_product_number from product_lvl where config_product_number like 'YSC%' or config_product_number like 'YHC%'";

            //string query = "select model_number,config_product_number from product_lvl where config_product_number like 'YSC%' or config_product_number like 'YHC%' or config_product_number like 'TSC%' or config_product_number like 'THC%' or config_product_number like 'WSC%' or config_product_number like 'WHC%' or config_product_number like 'YSD%' or config_product_number like 'YHD%' or config_product_number like 'TSD%' or config_product_number like 'THD%' or config_product_number like 'WSD%' or config_product_number like 'WHD%'";

            //string query = "select model_number,config_product_number from product_lvl where config_product_number like 'YSD%' or config_product_number like 'YHD%' or config_product_number like 'TSD%' or config_product_number like 'THD%' or config_product_number like 'WSD%' or config_product_number like 'WHD%'";

            string query = "select model_number, long_model_number from FDPDB_PROD_MAST_DN where status = 'ACTIVE' and ( model_number like 'YSC%' or model_number like 'YHC%' or model_number like 'TSC%' or model_number like 'THC%' or model_number like 'WSC%' or model_number like 'WHC%' or model_number like 'YSD%' or model_number like 'YHD%' or model_number like 'TSD%' or model_number like 'THD%' or model_number like 'WSD%' or model_number like 'WHD%')";

            cmd.CommandText = query;
            cmd.CommandType = System.Data.CommandType.Text;

            reader = cmd.ExecuteReader();
            while(reader.Read())
            {
               PriceModelNumber priceModelNumber = new PriceModelNumber();
               priceModelNumber.OrderNumber = reader["MODEL_NUMBER"].ToString();
               priceModelNumber.ModelNumber = reader["LONG_MODEL_NUMBER"].ToString();

               if(priceModelNumber.ModelNumber.Trim() != string.Empty)
               {
                  priceModelNumber.ProdCode = string.Empty;
                  priceModelNumber.ProductFamilyId = -1;
                  priceModelNumber.ExisintgListPrice = 0.0;

                  priceModelList.Add(priceModelNumber);
               }
            }

            return priceModelList;
         }

         catch(Exception ex)
         {
            EmailException(ex);
            return priceModelList;
         }
         finally
         {
            if(reader != null)
               reader.Dispose();

            if(cn != null)
               cn.Dispose();

            if(cmd != null)
               cmd.Dispose();
         }
      }




	}
}
