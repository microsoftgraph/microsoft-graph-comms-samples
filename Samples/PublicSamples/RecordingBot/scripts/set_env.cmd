@echo off
setlocal ENABLEDELAYEDEXPANSION
set vidx=0
for /F "tokens=*" %%A in (%1%) do (
	set /A ind=!vidx!
    set /A vidx=!vidx! + 1
	set var!vidx!=%%A
)
set var


FOR /F "delims== tokens=1" %%k IN ("%var7%") DO FOR /f "delims== tokens=2" %%v IN ("%var1%") DO SET %%k=%%v
FOR /F "delims== tokens=1" %%k IN ("%var2%") DO FOR /f "delims== tokens=2" %%v IN ("%var2%") DO SET %%k=%%v
FOR /F "delims== tokens=1" %%k IN ("%var3%") DO FOR /f "delims== tokens=2" %%v IN ("%var3%") DO SET %%k=%%v
FOR /F "delims== tokens=1" %%k IN ("%var4%") DO FOR /f "delims== tokens=2" %%v IN ("%var4%") DO SET %%k=%%v
FOR /F "delims== tokens=1" %%k IN ("%var5%") DO FOR /f "delims== tokens=2" %%v IN ("%var5%") DO SET %%k=%%v
FOR /F "delims== tokens=1" %%k IN ("%var6%") DO FOR /f "delims== tokens=2" %%v IN ("%var6%") DO SET %%k=%%v
FOR /F "delims== tokens=1" %%k IN ("%var7%") DO FOR /f "delims== tokens=2" %%v IN ("%var7%") DO SET %%k=%%v
FOR /F "delims== tokens=1" %%k IN ("%var8%") DO FOR /f "delims== tokens=2" %%v IN ("%var8%") DO SET %%k=%%v
FOR /F "delims== tokens=1" %%k IN ("%var9%") DO FOR /f "delims== tokens=2" %%v IN ("%var9%") DO SET %%k=%%v
FOR /F "delims== tokens=1" %%k IN ("%var10%") DO FOR /f "delims== tokens=2" %%v IN ("%var10%") DO SET %%k=%%v
FOR /F "delims== tokens=1" %%k IN ("%var11%") DO FOR /f "delims== tokens=2" %%v IN ("%var11%") DO SET %%k=%%v

