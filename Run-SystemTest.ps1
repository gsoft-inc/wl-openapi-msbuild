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

        Exec { & dotnet pack -c Release -o "$outputDir" }

        Push-Location $sysTestDir
        
        Exec { & dotnet add package Workleap.OpenApi.MSBuild --prerelease --source $outputDir }
        Exec { & dotnet build -c Release }

    }
    finally {
        Pop-Location
        Pop-Location
    }
}