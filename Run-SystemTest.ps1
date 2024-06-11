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

        ### Testing Generate Contract Mode ###

        # Then Should Successfully Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=GenerateContract"
        # When using legacy name / Then Should Successfully Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=CodeFirst"
        # When Comparing Spec and No Diff Error / Then Should Successfully Build 
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=GenerateContract;OpenApiCompareCodeAgainstSpecFile=true"
        # When Comparing Spec and Have Diff / Then Should Fail Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $oasDiffErrorSysTestDir -isFailureExpected $true -extraArgs "/p:OpenApiDevelopmentMode=GenerateContract;OpenApiCompareCodeAgainstSpecFile=true"


        ### Testing Compare Contract Mode ###

        # Given no diff / Then Should Successfully Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=ValidateContract"
        # Given no diff / When using legacy name / Then Should Successfully Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=ContractFirst"
        # Given diff / Then Should Fail Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $oasDiffErrorSysTestDir -isFailureExpected $true -extraArgs "/p:OpenApiDevelopmentMode=ValidateContract"
        # Given diff / When OpenApiTreatWarningsAsErrors=false / Then Should Successfully Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $oasDiffErrorSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiTreatWarningsAsErrors=false"


        ### Testing Spectral Validation ###

        # Given no spectral violation / When using frontend profile / Then Should Successfully Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiDevelopmentMode=GenerateContract;OpenApiServiceProfile=frontend"
        # Given no spectral violation / When using invalid profile / Then Should Fail Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $genericSysTestDir -isFailureExpected $true -extraArgs "/p:OpenApiDevelopmentMode=GenerateContract;OpenApiServiceProfile=scrap"
        # Given spectral violations / When using default ruleset / Then Should Fail Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $true
        # Given spectral violations / When using default ruleset And OpenApiTreatWarningsAsErrors=false / Then Should Successfully Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiTreatWarningsAsErrors=false"
        # Given workleap spectral violations / When ejecting ruleset / Then Should Successfully Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiSpectralRulesetUrl=./eject.spectral.yaml"
        # Given spectral violations / When overriding ruleset without disabling problematic ruleset / Then Should Fail Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $true -extraArgs "/p:OpenApiSpectralRulesetUrl=./override.spectral.yaml"
        # Given spectral violations / When overriding ruleset while disabling problematic ruleset / Then Should Successfully Build
        BuildProject -openApiMsBuildSource $outputDir -projectPath $spectralErrorSysTestDir -isFailureExpected $false -extraArgs "/p:OpenApiSpectralRulesetUrl=./override.fixed.spectral.yaml"
    }
    finally {
        Pop-Location
    }
}
