#Requires -Version 5.0

Begin {
    $ErrorActionPreference = "stop"
}

Process {
    function Exec([scriptblock]$Command) {
        & $Command
        if ($LASTEXITCODE -ne 0) {
            throw ("An error occurred while executing command: {0}" -f $Command)
        }
    }
    
    $workingDir = Join-Path $PSScriptRoot "src"
    $outputDir = Join-Path $PSScriptRoot ".output"
    $sysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest"

    try {
        Push-Location $workingDir
        Remove-Item $outputDir -Force -Recurse -ErrorAction SilentlyContinue
    
        Exec { & dotnet pack -c Release -o "$outputDir" }

        Push-Location $sysTestDir

        Exec { & echo "<?xml version=`"1.0`" encoding=`"utf-8`"?><configuration><packageSources><add key=`"local-packages`" value=`"$outputDir`" /></packageSources></configuration>" > NuGet.Config }
        Exec { & dotnet add package Workleap.OpenApi.MSBuild --prerelease }
        Exec { & dotnet build -c Release }

    }
    finally {
        Pop-Location
        Pop-Location
    }
}