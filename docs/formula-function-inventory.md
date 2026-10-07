# SS.Formula unimplemented-function inventory (task #43)

## XLOOKUP verification
`main/SS/Formula/Atp/XLookupFunction.cs` (registered in `AnalysisToolPak.cs`) supports if_not_found,
match modes -1/0/1/2 (wildcard), search modes 1/-1/2/-2 and array (row/column) returns; covered by
`TestXLookupFunction` (Microsoft examples 1-6, binary search) and, added here, backward search,
if_not_found and next-smaller cases. No gaps found.

## Implemented in #43
GCD, LCM, SQRTPI, EFFECT, NOMINAL, MULTINOMIAL, GESTEP (`Functions/EngineeringMathFunctions.cs`).

## Implemented in #59
BIN2HEX BIN2OCT DEC2OCT HEX2BIN HEX2OCT OCT2BIN OCT2HEX ERF ERFC and IMABS IMARGUMENT IMCONJUGATE IMCOS IMDIV IMEXP IMLN IMLOG10 IMLOG2 IMPOWER IMPRODUCT IMSIN IMSQRT IMSUB IMSUM (`Functions/EngineeringConversionFunctions.cs`).

## Implemented in #60
FISHER FISHERINV PERMUT CORREL PEARSON RSQ COVAR STEYX SKEW KURT GEOMEAN HARMEAN QUARTILE TRIMMEAN AVERAGEA STDEVA STDEVPA VARA VARPA (`Functions/LegacyStatisticalFunctions.cs`). Distribution/regression functions (LINEST, BETADIST, ...) remain.

## Implemented in #61
SLN SYD DB DDB VDB; DISC PRICEDISC YIELDDISC INTRATE RECEIVED ACCRINTM TBILLPRICE TBILLYIELD TBILLEQ FVSCHEDULE CUMIPMT CUMPRINC XNPV XIRR COUPDAYBS COUPDAYS COUPDAYSNC COUPNCD COUPNUM COUPPCD PRICE YIELD DURATION MDURATION (`Functions/FinancialFunctions.cs`; tests in `TestFinancialFunctions`, expected values are Microsoft documentation examples). Day counts follow Excel bases 0-4. VDB with fractional start/end weights each period's amount by overlap (Excel's exact fractional rule not verified).

## Already present (no work needed)
XMATCH, TEXTJOIN, IFS, SWITCH, XLOOKUP, CONCAT, MAXIFS, MINIFS.

## Still unimplemented, ATP registry (`AnalysisToolPak.cs`, registered as null)
- Engineering conversions: CONVERT BESSELI BESSELK BESSELY
- Financial: ACCRINT AMORDEGRC AMORLINC ODDFPRICE ODDFYIELD ODDLPRICE ODDLYIELD PRICEMAT YIELDMAT
- Other: BAHTTEXT JIS RTD SERIESSUM CUBE* (CUBEKPIMEMBER CUBEMEMBER CUBEMEMBERPROPERTY CUBERANKEDMEMBER CUBESET CUBESETCOUNT CUBEVALUE)

## Still unimplemented, built-in table (`Eval/FunctionEval.cs`, NotImplementedFunction)
- Statistical: LINEST TREND LOGEST GROWTH BETADIST GAMMALN BETAINV BINOMDIST CHIDIST CHIINV CONFIDENCE CRITBINOM EXPONDIST FDIST FINV GAMMADIST GAMMAINV HYPGEOMDIST LOGNORMDIST LOGINV NEGBINOMDIST WEIBULL CHITEST FTEST TTEST PROB ZTEST TINV
- Text/date: TIMEVALUE DATEDIF N INFO SEARCHB LEFTB RIGHTB MIDB LENB ASC DBCS PHONETIC
- Macro/legacy (out of scope): GOTO HALT ARGUMENT ERROR STEP ECHO REGISTER CALL etc.

(Some legacy statistical names may be served by newer implementations elsewhere; verify before starting.)

## Not present at all (dynamic-array / modern)
FILTER SORT SORTBY UNIQUE SEQUENCE RANDARRAY LET LAMBDA TAKE DROP VSTACK HSTACK TOCOL TOROW.
These need spill/array-result support in the evaluator and are a separate design task (not done in #61: spilling requires the evaluator to write a multi-cell result back to neighbouring cells and track the spill range for `#SPILL!`; HSSF `.xls` has no dynamic-array storage, so this mainly concerns XSSF).

## Priority list
1. Engineering conversions and complex-number families (small, well-defined).
2. Statistical legacy functions (CORREL, RSQ, SKEW, KURT, QUARTILE, GEOMEAN, HARMEAN, AVERAGEA/STDEVA/VARA, FISHER).
3. Financial: done in #61 except the odd-period, AMOR*, ACCRINT, PRICEMAT/YIELDMAT functions.
4. Dynamic-array functions (blocked on spill support).
