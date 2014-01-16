
SELECT C.R12_BILL_TO_ACCOUNT_NUMBER,c.R12_BILL_TO_SITE_USE_ID,c.SITE_USE_ID, c.cost_center,c.*
  FROM dia.fdcmb_cust_mast_dn c
WHERE c.CUST_NBR = '13410'
and c.name like '%STAR SUPPLY COMPANY, INC%'


select C.R12_BILL_TO_ACCOUNT_NUMBER, c.SITE_USE_ID, c.R12_BILL_TO_SITE_USE_ID,c.cost_center,C.NAME,c.*
  FROM dia.fdcmb_cust_mast_dn c
WHERE c.name like '%AIR SUPPLY%'
and COST_CENTER='96C0'


select C.R12_BILL_TO_ACCOUNT_NUMBER, c.SITE_USE_ID, c.R12_BILL_TO_SITE_USE_ID,c.cost_center,C.NAME,c.*
from dia.fdcmb_cust_mast_dn C 
where C.R12_BILL_TO_ACCOUNT_NUMBER in ('35803')