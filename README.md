# Workleap.OpenApi.MSBuild

Validates at build time that the OpenAPI specification files extracted from the ASP.NET Core Web API being built conform to Workleap API guidelines.

Depending if the user chose the Contract-First or Code-First development mode this MSBuild task will:

- Install tools: OasDiff, Spectral, SwashbuckleCLI
- Generate the OpenAPI specification file from the associated Web API
- Validate Spectral rules
- Compare the given OpenAPI specification file with the generated one

## How it works

- The entry point is `ValidateOpenApiTask.ExecuteAsync()` and will be executed during the build process on project that reference it. This is defined in `./src/Workleap.OpenApi.MSBuild/msbuild/tools/Workleap.OpenApi.MSBuild.targets` as a `UsingTask.TaskName`
- The default value are defined in the property group on the target `ValidateOpenApi` in this file `./src/Workleap.OpenApi.MSBuild/msbuild/tools/Workleap.OpenApi.MSBuild.targets`

## How to test locally

### How debug the project

Since it's a MSBuild task named `ValidateOpenApi` you can run it this command: `msbuild /t:ValidateOpenApi`. `./src/WebApiDebugger` already have a `launchSettings.json` named `ValidateOpenApi` that you can run in debug.

### With the system test

This command `./Run-SystemTest.ps1` will:

1. Pack the MSBuild library
2. Build the WebApi.MssBuild.SystemTest and inject the previously packed library

Be careful since it will update the project dependencies to use a local version: do not commit this.

Also if you run it multiple time on the same branch you need to clear the local cache `%UserProfile%\.nuget\packages\workleap.openapi.msbuild` since the name won't change.