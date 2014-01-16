using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOPSS.Utility;

namespace TOPSS.Common
{
	[Serializable]
	public class User
	{
		[BsonId]
		public ObjectId Id { get; set; }

		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string MiddleName { get; set; }
		public string Email { get; set; }
		public string Address1 { get; set; }
		public string Address2 { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string ZipCode { get; set; }
		public string Country { get; set; }
		public string Phone { get; set; }
		public string Fax { get; set; }
		public string Mobile { get; set; }
		public string Password { get; set; }
		public string Username { get; set; }
		public string Brand { get; set; }
		public string CompanyName { get; set; }
		public string ChannelType { get; set; }
		public string CostCenter { get; set; }
		public int TimesVisited { get; set; }
		public int SecondsLogged { get; set; }
		public long TempUserId { get; set; }
		public bool IsUserValid { get; set; }
		public bool IsPriceAdmin { get; set; }
		public bool IsDealer { get; set; }
		public ObjectId RoleId { get; set; }
		public DateTime CreatedOn { get; set; }
		public UserPermissions Permissions { get; set; }
		public UserSource Source { get; set; }



		//public DateTime DeletedOn { get; set; }
		//public DateTime LastLoggedOn { get; set; }

        public User()
        {

        }

		//NEVER OVERWRITE THE PERMISSIONS WHEN CREATING A USER. Only modify them.
		public User(UserSource source)
		{
			//Still needs most of the important information though.
			Id = ObjectId.GenerateNewId();
			TimesVisited = 1;
			SecondsLogged = 0;
			IsPriceAdmin = false;
			IsDealer = false;
			CreatedOn = DateTime.Now;
			Source = source;
			Permissions = new UserPermissions()
			{
				DefaultRunPerformance = true,
				DefaultRunSubmittal = false,
				DefaultRunPrice = false,
				DefaultShowModelNumMenu = false,
				SelectedUomScheme = "IP",
				SelectedLanguageId = "en",
				SelectedTimeZoneId = TimeZoneInfo.Local.Id,
				DefaultInputViewMap = DefaultInputViewMap.Normal,
                DefaultPriceSourceSMART = false
			};
		}
	}

	public enum UserSource
	{
		Ebiz,
		Normal,
      CorpDomain,
		QuickSelect
	}

	public enum DefaultInputViewMap 
	{
		Normal,
		ModelNumber
	}
	
    public enum DefaultPriceSourceMap
    {
        TOPSS,
        SMART
    }
	/// <summary>
	/// /
	/// </summary>
	/// 
	[Serializable]
	public class UserPermissions
	{
        public bool DefaultPriceSourceSMART { get; set; }
		public bool DefaultShowModelNumMenu { get; set; }
		public bool DefaultRunPerformance { get; set; }
		public bool DefaultRunSubmittal { get; set; }
		public bool DefaultRunPrice { get; set; }
		public string SelectedUomScheme { get; set; }
		public string SelectedLanguageId { get; set; }
		public string SelectedTimeZoneId { get; set; }
		public DefaultInputViewMap DefaultInputViewMap { get; set; }
	}

	public class UserRole
	{
		[BsonId]
		public ObjectId Id { get; set; }

		public string Name { get; set; }
		public string Description { get; set; }
		public List<UserProductPermission> ProductPermissions { get; set; }
		public List<UserPermission> UserPermissions { get; set; }
	}

	public class UserProductPermission
	{
		public string Product { get; set; }
	}

	[Serializable]
	public class UserPermission
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}
}
