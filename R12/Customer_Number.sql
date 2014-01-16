SELECT c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.*
  FROM dia.fdcmb_cust_mast_dn c
WHERE c.CUST_NBR = '13410'
and c.name like '%STAR SUPPLY COMPANY, INC%'


SELECT c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
  FROM dia.fdcmb_cust_mast_dn c
WHERE 
c.name like '%AIR ENGIN%'
and LENGTH(c.COST_CENTER) > 1

--268417

SELECT c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
  FROM dia.fdcmb_cust_mast_dn c
WHERE 
  C.SITE_USE_ID=284918
  
  
select c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
from dia.fdcmb_cust_mast_dn c
where c.CUST_NBR='02248'

select c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
from dia.fdcmb_cust_mast_dn c
where c.CUST_NBR='284918'

select c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
from dia.fdcmb_cust_mast_dn c
where c.Name like '%HEARTLAND%'
and c.COST_CENTER='9540'


select c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
from dia.fdcmb_cust_mast_dn c
where c.COST_CENTER='9540'
order by c.Name

select c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
from dia.fdcmb_cust_mast_dn c
where c.COST_CENTER='96K0'
order by c.Name

SELECT c.R12_BILL_TO_ACCOUNT_NUMBER, c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
  FROM dia.fdcmb_cust_mast_dn c
WHERE 
  C.SITE_USE_ID=268417


select c.R12_BILL_TO_ACCOUNT_NUMBER, c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
  FROM dia.fdcmb_cust_mast_dn c  where c.R12_BILL_TO_ACCOUNT_NUMBER=35803
  
 
  select C.STATUS,c.R12_BILL_TO_ACCOUNT_NUMBER, c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
  FROM dia.fdcmb_cust_mast_dn c  where  C.SITE_USE_ID=268417
  
  select C.STATUS,c.R12_BILL_TO_ACCOUNT_NUMBER, c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.name,c.*
  FROM dia.fdcmb_cust_mast_dn c  
  where
  c.COST_CENTER='96K0'
  and C.STATUS='A'
  
  
  
  
  
  
   



