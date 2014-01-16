using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOPSS.Common;
using TOPSS.ProdData;
using System.Threading.Tasks;
using UnitPricing.wsJobPricer;

namespace TOPSS.Pricing
{
    public static class PricingDependencies
    {

        public static void RunPriceDepencyChecks(List<assoc_category> listDependencies, SettingSet set)
        {

            //step thru each dependency
            foreach (assoc_category assoc_cat in listDependencies)
            {

                //get my dependencies met in the set?
               // var targetFieldValue=set.GetValue

                //no ? add field to requiredPriceDepList
               // MissingPriceDependentFields.Add();
            }
        }

    }
}
