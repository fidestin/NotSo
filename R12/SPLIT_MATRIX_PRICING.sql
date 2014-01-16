select * from pricing_matrix where model_number like 'TWE018P130B%'

var inCostCentre varchar2
var inModel varchar2
var rc refcursor
exec :inCostCentre :='9840'
exec :inModel :='TWE090D300A'
//exec :inModel :='BAYSTAT152A'

--PRICEMATRIXPARM=WSC036E1R0A00006253....2026....ZZZZZZZZZZZZZZZ
exec TOPSSMATRIX.priceMatrix(:inCostCentre,:inModel,outPriceMatrix =>:rc)
print :rc