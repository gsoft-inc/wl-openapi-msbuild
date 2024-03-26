# Workleap.OpenApi.MSBuild

Validates at build time that the OpenAPI specification files extracted from the ASP.NET Core Web API being built conform to Workleap API guidelines.

Depending if the user chose the ValidateContract or GenerateContract development mode this MSBuild task will:

- Install tools: [OasDiff](https://github.com/Tufin/oasdiff), [Spectral](https://github.com/stoplightio/spectral), [SwashbuckleCLI](https://github.com/domaindrivendev/Swashbuckle.AspNetCore?tab=readme-ov-file#swashbuckleaspnetcorecli)
- Generate the OpenAPI specification file from the associated Web API
- Validate [Workleap Spectral rules](https://github.com/gsoft-inc/wl-api-guidelines/blob/main/.spectral.yaml)
- Compare the given OpenAPI specification file with the generated one

## How it works

[Official Documentation](https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-custom-task-code-generation?view=vs-2022#include-msbuild-properties-and-targets-in-a-package)

For the TLDR version: 

- The entry point is `ValidateOpenApiTask.ExecuteAsync()` and will be executed after the referencing project is built. This is defined in `./src/Workleap.OpenApi.MSBuild/msbuild/tools/Workleap.OpenApi.MSBuild.targets` as a `UsingTask.TaskName`
- The default value are defined in the property group on the target `ValidateOpenApi` in this file `./src/Workleap.OpenApi.MSBuild/msbuild/tools/Workleap.OpenApi.MSBuild.targets`

## How to test locally

### How debug the project

Since it's a MSBuild task named `ValidateOpenApi` you can run it this command: `msbuild /t:ValidateOpenApi`. `./src/WebApiDebugger` already have some preconfigured in `launchSettings.json` that you can run in debug.

Note: Before executing this task it will build the project.

Warming: validate your IDE is in Configuration:Debug otherwise the execution will not be done on the most recent code.

### With the system test

This command `./Run-SystemTest.ps1` will:

1. Pack the MSBuild library
2. Build the WebApi.MsBuild.SystemTest and inject the previously packed library

Be careful since it will update the project dependencies to use a local version: do not commit this.

Also if you run it multiple time on the same branch you need to clear the local cache `%UserProfile%\.nuget\packages\workleap.openapi.msbuild` since the name won't change.