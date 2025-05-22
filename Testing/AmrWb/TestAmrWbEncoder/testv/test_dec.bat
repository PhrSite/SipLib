@echo off
set TestPath=\3gpp\TestAmrCpp\testv
echo %TestPath%
cd \3GPP\TestAmrCpp\bin\Debug\net8.0>TestAmrCpp.exe

TestAmrCpp.exe -decode %TestPath%\tst_m0.cod %TestPath%\_tst_m0.out
TestAmrCpp.exe -decode %TestPath%\tst_m1.cod %TestPath%\_tst_m1.out
TestAmrCpp.exe -decode %TestPath%\tst_m2.cod %TestPath%\_tst_m2.out
TestAmrCpp.exe -decode %TestPath%\tst_m4.cod %TestPath%\_tst_m4.out
TestAmrCpp.exe -decode %TestPath%\tst_m5.cod %TestPath%\_tst_m5.out
TestAmrCpp.exe -decode %TestPath%\tst_m6.cod %TestPath%\_tst_m6.out
TestAmrCpp.exe -decode %TestPath%\tst_m7.cod %TestPath%\_tst_m7.out
TestAmrCpp.exe -decode %TestPath%\tst_m8.cod %TestPath%\_tst_m8.out
TestAmrCpp.exe -decode %TestPath%\tst_md.cod %TestPath%\_tst_md.out

cd %TestPath%
fc /b tst_m0.out _tst_m0.out
fc /b tst_m1.out _tst_m1.out
fc /b tst_m2.out _tst_m2.out
fc /b tst_m3.out _tst_m3.out
fc /b tst_m4.out _tst_m4.out
fc /b tst_m5.out _tst_m5.out
fc /b tst_m6.out _tst_m6.out
fc /b tst_m7.out _tst_m7.out
fc /b tst_m8.out _tst_m8.out
fc /b tst_md.out _tst_md.out

del _tst_m?.out
