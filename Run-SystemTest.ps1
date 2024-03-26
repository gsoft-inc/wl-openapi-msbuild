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
            Write-Error "The build for project $projectPath was expected to fail, but it succeeded."
        } elseif (!$isFailureExpected -and $buildProcess.ExitCode -ne 0) {
            Write-Error "The build for project $projectPath was expected to succeed, but it failed."
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
    $validateContractSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.ValidateContract"
    $codeFirstSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.CodeFirst"
    $generateContractSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.GenerateContract"
    $oasDiffErrorSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.OasDiffError"
    $spectralErrorSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.SpectralError"

    try {
        Push-Location $workingDir

        # Build the OpenApi.MSBuild package to be used in the system tests
        Exec { & dotnet pack -c Release -o "$outputDir" }

        BuildProject -openApiMsBuildSource $outputDir -projectPath $contractFirstSysTestDir -isFailureExpected $false
        BuildProject -openApiMsBuildSource $outputDir -projectPath $validateContractSysTestDir -isFailureExpected $false
        BuildProject -openApiMsBuildSource $outputDir -projectPath $codeFirstSysTestDir -isFailureExpected $false
        BuildProject -openApiMsBuildSource $outputDir -projectPath $generateContractSysTestDir -isFailureExpected $false
        BuildProject -openApiMsBuildSource $outputDir -projectPath $oasDiffErrorSysTestDir -isFailureExpected $true
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $true
        BuildProject -openApiMsBuildSource $outputDir -projectPath $oasDiffErrorSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiTreatWarningsAsErrors=false"
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiTreatWarningsAsErrors=false"
    }
    finally {
        Pop-Location
        Pop-Location
    }
}