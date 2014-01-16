

select * from org_lvl where org_lvl_ID=9360
select * from FDCMB_CUST_MAST_DN where CUST_NBR='35803'

select * from FDCMB_CUST_MAST_DN where NAME like '%INDOOR COMFORT%'
select * from FDCMB_CUST_MAST_DN where COST_CENTER='9540'
select * from FDCMB_CUST_MAST_DN where ma
--Cost Center Code : 9840  MadidNumber : 9812281 -- CUST_NBR : 02248
select * from FDCMB_CUST_MAST_DN where MADIS_CUSTOMER_NBR='9548588'
select * from FDCMB_CUST_MAST_DN where MADIS_CUSTOMER_NBR like '9548%'

SELECT a.cost_center_code,a.cost_center_Name,b.MADIS_CUSTOMER_NUMBER,b.*
FROM COST_CENTER a, org_lvl b  
				WHERE a.cost_center_code = b.cost_center_code 
				and b.org_lvl_type_code in ('DSO','IWD','DLR') 
				AND A.INTERFACE_INDICATOR = 2  
        --and a.COST_CENTER_CODE='9540'
        and b.MADIS_CUSTOMER_NUMBER='9548057'
        order by A.COST_CENTER_NAME, A.COST_CENTER_Code
        
select * from org_lvl where MADIS_CUSTOMER_NUMBER='9548103'
select * from COST_CENTER where Cost_Center_Code='97D0'
        