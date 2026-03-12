# Introduction

This library contains health checks that can be used to integrate with the OCC (operation control center) of the Federal Office of Information Technology, Systems and Telecommunication FOITT.
Additionally, it offers automatic mapping to an HTTP endpoint where the current state of the health checks can be queried.

The latest NuGet package is published at https://www.nuget.org/packages/Swiss.FCh.Monitoring.

# Usage

The health checks can be configured in your ```Program.cs```.

```csharp
using Swiss.FCh.Monitoring.Extensions;

[...]

var builder = WebApplication.CreateBuilder(args);

builder.Services
  .AddHealthChecks()
  .AddDatabase<YourDataContext>()
  .AddUrl("my-health-name", "https://url-to-check.test");
  
[...]

var app = builder.Build();
app.MapFChHealthChecks(); //creates '/api/systemstatus' endpoint (route can be overriden)
```

# Contribution
See: https://github.com/swiss/fch-monitoring/blob/main/CONTRIBUTING.md

# Security
See: https://github.com/swiss/fch-monitoring/blob/main/SECURITY.md

# Development Workflow

To publish a new version of the NuGet package, proceed as follows.

* apply and push your changes
* define and describe the new version in ```CHANGELOG.md```
* push the corresponding label with ```git tag vx.x.x``` and ```git push origin v.x.x.x```
* go to GitHub -> Actions -> 'Build and Publish to NuGet.org' and trigger a run while specifying the correct GIT label
