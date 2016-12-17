@echo off

nuget restore -PackagesDirectory .

cd ..\ImageSharp.Tests

where dotnet

..\CodeCoverage\OpenCover.4.6.519\tools\OpenCover.Console.exe -register:user -target:"C:\Program Files\dotnet\bin\dotnet.exe" -targetargs:"test -c Release" -excludebyattribute:*.ExcludeFromCodeCoverage* -hideskipped:All -oldStyle -output:.\ImageSharp.Coverage.xml

SET PATH=C:\\Python34;C:\\Python34\\Scripts;%PATH%
pip install codecov
codecov -f "ImageSharp.Coverage.xml"
