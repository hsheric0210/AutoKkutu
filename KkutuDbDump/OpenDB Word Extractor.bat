@ECHO OFF

ECHO Note that you should load kkutu db^(db.sql^) into postgresql and set its database name to postdata

SETLOCAL ENABLEDELAYEDEXPANSION
FOR /F "DELIMS=" %%A IN (words.txt) DO (
	SET FORMAT=%%A%%
	"%ProgramFiles%\PostgreSQL\15\bin\psql.exe" -U postgres -d kkutu -c "COPY (SELECT _id FROM public.kkutu_ko WHERE LENGTH(_id) > 1 AND _id LIKE '!FORMAT!' AND type SIMILAR TO '(,|^)(0|1|3|7|8|11|9|16|15|17|2|18|20|26|19|INJEONG)(,|$)' ORDER BY LENGTH(_id) DESC) TO 'D:\Utilities\Macro\Kkutu\list.txt' (FORMAT CSV, HEADER FALSE, DELIMITER ';', ENCODING 'UTF8');"
	REN .\list.txt %%A.txt
)
ENDLOCAL

PAUSE