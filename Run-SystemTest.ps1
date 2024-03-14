#Requires -Version 5.0

Begin {
    $ErrorActionPreference = "stop"
}

Process {
    function BuildProject {
        param (
            [string]$openApiMsBuildSource,
            [string]$projectPath,
            [bool]$isFailureExpected,
            [string]$extraArgs
        )
        Push-Location $projectPath
        
        Exec { & dotnet add package Workleap.OpenApi.MSBuild --prerelease --source $openApiMsBuildSource }

        $buildProcess = Start-Process -FilePath "dotnet" -ArgumentList "build -c Release $extraArgs" -NoNewWindow -PassThru -Wait

        if ($isFailureExpected -and $buildProcess.ExitCode -eq 0 ) {
            Write-Error "The build did not fail as expected for project $projectPath."
        } elseif (!$isFailureExpected -and $buildProcess.ExitCode -ne 0) {
            Write-Error "The build unexpectedly failed for project $projectPath."
        }
    }

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

        # Build the OpenApi.MSBuild package to be used in the system tests
        Exec { & dotnet pack -c Release -o "$outputDir" }

        BuildProject -openApiMsBuildSource $outputDir -projectPath $contractFirstSysTestDir -isFailureExpected $false
        BuildProject -openApiMsBuildSource $outputDir -projectPath $codeFirstSysTestDir -isFailureExpected $false
        BuildProject -openApiMsBuildSource $outputDir -projectPath $oasDiffErrorSysTestDir -isFailureExpected $true
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $true
        BuildProject -openApiMsBuildSource $outputDir -projectPath $oasDiffErrorSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiIgnoreErrors=true"
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiIgnoreErrors=true"
    }
    finally {
        Pop-Location
        Pop-Location
    }
}