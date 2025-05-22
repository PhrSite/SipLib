@echo off
set TestPath=\3gpp\TestAmrCpp\testv
echo %TestPath%
cd \3GPP\TestAmrCpp\bin\Debug\net8.0

TestAmrCpp.exe -encode -dtx 0 %TestPath%\tst.inp %TestPath%\_tst_m0.cod
TestAmrCpp.exe -encode -dtx 1 %TestPath%\tst.inp %TestPath%\_tst_m1.cod
TestAmrCpp.exe -encode -dtx 2 %TestPath%\tst.inp %TestPath%\_tst_m2.cod
TestAmrCpp.exe -encode -dtx 3 %TestPath%\tst.inp %TestPath%\_tst_m3.cod
TestAmrCpp.exe -encode -dtx 4 %TestPath%\tst.inp %TestPath%\_tst_m4.cod
TestAmrCpp.exe -encode -dtx 5 %TestPath%\tst.inp %TestPath%\_tst_m5.cod
TestAmrCpp.exe -encode -dtx 6 %TestPath%\tst.inp %TestPath%\_tst_m6.cod
TestAmrCpp.exe -encode -dtx 7 %TestPath%\tst.inp %TestPath%\_tst_m7.cod
TestAmrCpp.exe -encode -dtx 8 %TestPath%\tst.inp %TestPath%\_tst_m8.cod
TestAmrCpp.exe -encode -dtx 2 %TestPath%\dtx.inp %TestPath%\_tst_md.cod

cd %TestPath%
fc /b tst_m0.cod _tst_m0.cod
fc /b tst_m1.cod _tst_m1.cod
fc /b tst_m2.cod _tst_m2.cod
fc /b tst_m3.cod _tst_m3.cod
fc /b tst_m4.cod _tst_m4.cod
fc /b tst_m5.cod _tst_m5.cod
fc /b tst_m6.cod _tst_m6.cod
fc /b tst_m7.cod _tst_m7.cod
fc /b tst_m8.cod _tst_m8.cod
fc /b tst_md.cod _tst_md.cod

del _tst_m?.cod
