create or replace
PROCEDURE IMPORT_PRICMATRIXRECS(clobToParse in CLOB, returnCode out varchar2) is

    type PricMatrixImport is varray(8) of varchar2(200);

	tblWithVals2 PricMatrixImport := PricMatrixImport(' ',' ',' ',' ',' ',' ',' ',' ');
	i                  NUMBER;  -- index for record loop
	b                  NUMBER := 1;  -- begin char position
	e                  NUMBER;  --  end char pos
	L                  NUMBER;  --  length
	j                  NUMBER;  -- index for field loop
	intStrLen          NUMBER;
	strDelim           varchar2(2) := '|';
  begin
    	-- intStrLen := dbms_lob.length(clobToParse);
	-- Venkat's code here.
	SELECT dbms_lob.getlength(clobToParse) INTO intStrLen
	FROM dual;
	
        returnCode := '0';
        i :=1;
     	loop
     	    exit when b >= intStrLen;     	
     	    for j in 1..8 loop
                SELECT dbms_lob.instr(clobToParse,strDelim,b) into e FROM dual;
                L := e-b;
                --dbms_output.put_line('in loop: '||substr(clobToParse,b,l)||'-'||b||'-'||e||'-'||l);
                
                SELECT dbms_lob.substr(clobToParse,L,b) into tblWithVals2(j) FROM dual;
                b := e+1;     -- skip over |
            end loop;
            b := b+1;  -- skip over double ||
            IF i = 1  then
                UPDATE
                    PCG.PRICING_MATRIX
                    SET
                    STATUS_INDICATOR = 4
                    WHERE
                    (COST_CENTER_CODE LIKE tblWithVals2(1));
            end if;
       		--'Value: '||tblWithVals2(1)||'-'||tblWithVals2(2)||'-'||tblWithVals2(3)||'-'||tblWithVals2(4)||'-'||tblWithVals2(5)||'-'||tblWithVals2(6)||'-'||tblWithVals2(7)||'-'||tblWithVals2(8));

            INSERT INTO
				PCG.PRICING_MATRIX
               (
                COST_CENTER_CODE,
				PROD_FAMILY,
				PRODUCT_CODE,
				MULT_GROUP_CODE,
				STATUS_INDICATOR,
				MARGIN_PERCENT,
				SELL_MULT,
				BUY_MULT
                )
			VALUES
	           (
                tblWithVals2(1),
                tblWithVals2(2),
			    tblWithVals2(3),
			    tblWithVals2(4),
			    1,
			    tblWithVals2(5),
			    tblWithVals2(6),
			    tblWithVals2(7));
            		--dbms_output.put_line('--Subvalue: '||tblWithVals2(j));
     		i := i+1;
       end loop;
EXCEPTION
    WHEN others THEN
        returnCode := '1';
end IMPORT_PRICMATRIXRECS;
