@ECHO OFF
SET "FSIBAT=%ProgramW6432%\fsx\fsi.bat"
SET "FSXFSX=%ProgramW6432%\fsx\fsx.dll"

IF NOT EXIST "%FSIBAT%" (
    ECHO "%FSIBAT% not found" && EXIT /b 1
)

IF NOT EXIST "%FSXFSX%" (
    ECHO "%FSXFSX% not found" && EXIT /b 1
)

"%FSIBAT%" "%FSXFSX%" %*
