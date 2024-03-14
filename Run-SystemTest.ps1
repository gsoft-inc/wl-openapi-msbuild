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
    $contractFirstSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.ContractFirst"
    $codeFirstSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.CodeFirst"
    $oasDiffErrorSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.OasDiffError"
    $spectralErrorSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.SpectralError"

    try {
        Push-Location $workingDir

        Exec { & dotnet pack -c Release -o "$outputDir" }

        Push-Location $contractFirstSysTestDir
        
        Exec { & dotnet add package Workleap.OpenApi.MSBuild --prerelease --source $outputDir }
        Exec { & dotnet build -c Release }
        
        Push-Location $codeFirstSysTestDir
        
        Exec { & dotnet add package Workleap.OpenApi.MSBuild --prerelease --source $outputDir }
        Exec { & dotnet build -c Release }

        Push-Location $oasDiffErrorSysTestDir
        
        Exec { & dotnet add package Workleap.OpenApi.MSBuild --prerelease --source $outputDir }
        Exec { & dotnet build -c Release }
        
        Push-Location $spectralErrorSysTestDir
        
        Exec { & dotnet add package Workleap.OpenApi.MSBuild --prerelease --source $outputDir }
        Exec { & dotnet build -c Release }

    }
    finally {
        Pop-Location
        Pop-Location
    }
}