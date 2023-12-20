# Workleap.OpenApi.MSBuild

Validates at build time that the OpenAPI specification files extracted from the ASP.NET Core Web API being built conform to Workleap API guidelines.

## How it work

- The entry point is `ValidateOpenApiTask.ExecuteAsync()` and will be executed during the build process on project that reference it. This is defined in `./src/Workleap.OpenApi.MSBuild/msbuild/tools/Workleap.OpenApi.MSBuild.targets` as a `UsingTask.TaskName`
- The default value are defined in the property group on the target `ValidateOpenApi` in this file `./src/Workleap.OpenApi.MSBuild/msbuild/tools/Workleap.OpenApi.MSBuild.targets`

## How to test locally

### With the system test

This command `./Run-SystemTest.ps1` will:

1. Pack the MSBuild library
2. Build the WebApi.MssBuild.SystemTest and inject the previously packed library

### How debug the project

Since it's a MSBuild task named `ValidateOpenApi` you can run it this command: `msbuild /t:ValidateOpenApi`. `./src/WebApiDebugger` already have a `launchSettings.json` named `ValidateOpenApi` that you can run in debug.
