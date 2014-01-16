select distinct(cost_center_code) from pricing_matrix

select * from pricing_matrix 
where cost_center_Code='98H0'
--and MULT_GROUP_CODE='0289'
and status_indicator=1


select MULT_GROUP_CODE from pricing_matrix 
where cost_center_Code='98H0'
--and MULT_GROUP_CODE='0289'
and status_indicator=1
group by MULT_GROUP_CODE




--group by cost_center_code