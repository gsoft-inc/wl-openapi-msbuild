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

        try
        {
            Push-Location $projectPath

            Exec { & dotnet add package Workleap.OpenApi.MSBuild --prerelease --source $openApiMsBuildSource }

            $buildProcess = Start-Process -FilePath "dotnet" -ArgumentList "build -c Release $extraArgs" -NoNewWindow -PassThru -Wait

            Exec { & dotnet remove package Workleap.OpenApi.MSBuild }

            if ($isFailureExpected -and $buildProcess.ExitCode -eq 0 ) {
                Write-Error "The build for project $projectPath was expected to fail, but it succeeded."
            } elseif (!$isFailureExpected -and $buildProcess.ExitCode -ne 0) {
                Write-Error "The build for project $projectPath was expected to succeed, but it failed."
            }
        }
        finally {
            Pop-Location
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

    $genericSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.GenericTest"
    $oasDiffErrorSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.OasDiffError"
    $spectralErrorSysTestDir = Join-Path $PSScriptRoot "src/tests/WebApi.MsBuild.SystemTest.SpectralError"

    try {
        Push-Location $workingDir

        # Build the OpenApi.MSBuild package to be used in the system tests
        Exec { & dotnet pack -c Release -o "$outputDir" }

        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=CodeFirst"
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=ContractFirst"
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=ValidateContract" "/p:OpenApiCompareCodeAgainstSpecFile=true"
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=GenerateContract"
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=GenerateContract" "/p:OpenApiProfile=frontend"
        BuildProject -openApiMsBuildSource $outputDir -projectPath $oasDiffErrorSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiTreatWarningsAsErrors=false"
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiTreatWarningsAsErrors=false"
        BuildProject -openApiMsBuildSource $outputDir -projectPath $oasDiffErrorSysTestDir -isFailureExpected $true
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $true
    }
    finally {
        Pop-Location
    }
}
