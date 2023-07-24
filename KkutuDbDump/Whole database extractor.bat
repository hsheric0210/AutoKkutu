@ECHO OFF

ECHO Note that you should load kkutu db^(db.sql^) into postgresql and set its database name to postdata

SET PGPASSWORD=OhxFhPXLqFVEATmKQALhQhKdVGiupcYVaCQgWySdLBWCrEwckaxgKUTcPdGWYROz

"%ProgramFiles%\PostgreSQL\15\bin\psql.exe" -U postgres -d kkutu -c "COPY (SELECT _id FROM public.kkutu_ko WHERE LENGTH(_id) > 1 AND regexp_like(type, '(,|^)(0|1|3|7|8|11|9|16|15|17|2|18|20|26|19|INJEONG)(,|$)') ORDER BY LENGTH(_id) DESC) TO 'D:\Utilities\Macro\Kkutu\kor_list.txt' (FORMAT CSV, HEADER FALSE, DELIMITER ';', ENCODING 'UTF8');"
"%ProgramFiles%\PostgreSQL\15\bin\psql.exe" -U postgres -d kkutu -c "COPY (SELECT _id FROM public.kkutu_en WHERE LENGTH(_id) > 1 AND regexp_like(_id, '^[a-z]+', 'i') ORDER BY LENGTH(_id) DESC) TO 'D:\Utilities\Macro\Kkutu\eng_list.txt' (FORMAT CSV, HEADER FALSE, DELIMITER ';', ENCODING 'UTF8');"
