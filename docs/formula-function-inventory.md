# SS.Formula unimplemented-function inventory (task #43)

## XLOOKUP verification
`main/SS/Formula/Atp/XLookupFunction.cs` (registered in `AnalysisToolPak.cs`) supports if_not_found,
match modes -1/0/1/2 (wildcard), search modes 1/-1/2/-2 and array (row/column) returns; covered by
`TestXLookupFunction` (Microsoft examples 1-6, binary search) and, added here, backward search,
if_not_found and next-smaller cases. No gaps found.

## Implemented in #43
GCD, LCM, SQRTPI, EFFECT, NOMINAL, MULTINOMIAL, GESTEP (`Functions/EngineeringMathFunctions.cs`).

## Implemented in #60
FISHER FISHERINV PERMUT CORREL PEARSON RSQ COVAR STEYX SKEW KURT GEOMEAN HARMEAN QUARTILE TRIMMEAN AVERAGEA STDEVA STDEVPA VARA VARPA (`Functions/LegacyStatisticalFunctions.cs`). Distribution/regression functions (LINEST, BETADIST, ...) remain.

## Already present (no work needed)
XMATCH, TEXTJOIN, IFS, SWITCH, XLOOKUP, CONCAT, MAXIFS, MINIFS.

## Still unimplemented, ATP registry (`AnalysisToolPak.cs`, registered as null)
- Engineering conversions: BIN2HEX BIN2OCT DEC2OCT HEX2BIN HEX2OCT OCT2BIN OCT2HEX CONVERT ERF ERFC BESSELI BESSELK BESSELY
- Complex numbers: IMABS IMARGUMENT IMCONJUGATE IMCOS IMDIV IMEXP IMLN IMLOG10 IMLOG2 IMPOWER IMPRODUCT IMSIN IMSQRT IMSUB IMSUM
- Financial: ACCRINT ACCRINTM AMORDEGRC AMORLINC COUPDAYBS COUPDAYS COUPDAYSNC COUPNCD COUPNUM COUPPCD CUMIPMT CUMPRINC DISC DURATION FVSCHEDULE INTRATE MDURATION ODDFPRICE ODDFYIELD ODDLPRICE ODDLYIELD PRICE PRICEDISC PRICEMAT RECEIVED TBILLEQ TBILLPRICE TBILLYIELD XIRR XNPV YIELD YIELDDISC YIELDMAT
- Other: BAHTTEXT JIS RTD SERIESSUM CUBE* (CUBEKPIMEMBER CUBEMEMBER CUBEMEMBERPROPERTY CUBERANKEDMEMBER CUBESET CUBESETCOUNT CUBEVALUE)

## Still unimplemented, built-in table (`Eval/FunctionEval.cs`, NotImplementedFunction)
- Statistical: LINEST TREND LOGEST GROWTH BETADIST GAMMALN BETAINV BINOMDIST CHIDIST CHIINV CONFIDENCE CRITBINOM EXPONDIST FDIST FINV GAMMADIST GAMMAINV HYPGEOMDIST LOGNORMDIST LOGINV NEGBINOMDIST WEIBULL CHITEST FTEST TTEST PROB ZTEST TINV
- Financial: SLN SYD DDB DB VDB
- Text/date: TIMEVALUE DATEDIF N INFO SEARCHB LEFTB RIGHTB MIDB LENB ASC DBCS PHONETIC
- Macro/legacy (out of scope): GOTO HALT ARGUMENT ERROR STEP ECHO REGISTER CALL etc.

(Some legacy statistical names may be served by newer implementations elsewhere; verify before starting.)

## Not present at all (dynamic-array / modern)
FILTER SORT SORTBY UNIQUE SEQUENCE RANDARRAY LET LAMBDA TAKE DROP VSTACK HSTACK TOCOL TOROW.
These need spill/array-result support in the evaluator and are a separate design task.

## Priority list
1. Engineering conversions and complex-number families (small, well-defined).
2. Statistical legacy functions (CORREL, RSQ, SKEW, KURT, QUARTILE, GEOMEAN, HARMEAN, AVERAGEA/STDEVA/VARA, FISHER).
3. Financial (SLN, SYD, DDB, DB, VDB, then bond/coupon family).
4. Dynamic-array functions (blocked on spill support).
