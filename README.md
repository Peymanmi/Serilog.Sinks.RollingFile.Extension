# Serilog.Sinks.SizeRollingFile      [![Build status](https://ci.appveyor.com/api/projects/status/iygk78hbgjbh4tp6)](https://ci.appveyor.com/project/peymanmi/serilog-sinks-rollingfile-extension)

This project has been developed to extend [Serilog](https://github.com/serilog/serilog) buit-in RollingFile, to limit the log files based on size, also purge old files to free up disk space.

## The nuget package  [![NuGet Status](https://img.shields.io/nuget/v/Serilog.Sinks.RollingFile.Extension.svg?style=flat)](https://www.nuget.org/packages/Serilog.Sinks.RollingFile.Extension/)

https://www.nuget.org/packages/Serilog.Sinks.RollingFile.Extension/

    PM> Install-Package Serilog.Sinks.RollingFile.Extension



## Configuring the logger


### 1. Through the code
 
```cs
new LoggerConfiguration()                                       
      .WriteTo.SizeRollingFile(@"C:\temp\log.txt", 
              retainedFileDurationLimit: TimeSpan.FromDays(2), 
              fileSizeLimitBytes: 1024 * 1024 * 10) // 10MB
      .CreateLogger();
```


### 2. Configuration file

#### XML `appsettings.json` configuration

```xml
<appSettings>
    <add key="serilog:using:SizeRollingFile" value="Serilog.Sinks.RollingFile.Extension"/>
    <add key="serilog:write-to:SizeRollingFile.pathFormat" value="C:\temp\log.txt"/>
    <add key="serilog:write-to:SizeRollingFile.fileSizeLimitBytes" value="10485760"/>
    <add key="serilog:write-to:SizeRollingFile.retainedFileDurationLimit" value="2.00:00:00"/>
</appSettings>
```

#### JSON `appsettings.json` configuration

To use the file sink with _Microsoft.Extensions.Configuration_, for example with ASP.NET Core or .NET Core, use the [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) package. First install that package if you have not already done so:

```powershell
Install-Package Serilog.Settings.Configuration
```

Instead of configuring the file directly in code, call `ReadFrom.Configuration()`:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
```

your `appsettings.json` file should look like this :

```json
{
  "Serilog": {
    "Using": [ "Serilog.Sinks.RollingFile.Extension" ],
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "SizeRollingFile",
        "Args": {
          "pathFormat": "C:\\temp\\log.txt",
          "fileSizeLimitBytes ": 200,
          "retainedFileDurationLimit": "00:00:30"
        }
      }
    ]
  }
}
```

## New Feature

### 1. Separating output file based on Log Level

Adding new variable in path tamplte to suppoert the log's {Level}, can use it same as {Date} variable

```xml

<add key="serilog:write-to:SizeRollingFile.pathFormat" value="C:\temp\log-{Date}-{Level}.txt"/>

```

### 2. Writing log in file asynchronously

To enable async, should enable in config file as follow

```xml
<add key="serilog:write-to:SizeRollingFile.supportAsync" value="true" />
```

There are defferent parameter that can update the default value

```xml
<add key="serilog:write-to:SizeRollingFile.maxRetries" value="5" /> <!-- Default = 3 -->
<add key="serilog:write-to:SizeRollingFile.bufferSize" value="9999" /> <!-- Default = 10000 -->
```





    
