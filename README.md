# Serilog.Sinks.SizeRollingFile

This project has been developed to extend [Serilog](https://github.com/serilog/serilog) buit-in RollingFile, to limit the log files based on size, also purge old files to free up disk space.

There are two ways to config the Serilog logger to use this custom rolling file.

### 1. Defining in code
 
```cs
new LoggerConfiguration()                                       
      .WriteTo.SizeRollingFile(@"C:\temp\log.txt", 
              retainedFileDurationLimit: TimeSpan.FromDays(2), 
              fileSizeLimitBytes: 1024 * 1024 * 10) // 10MB
      .CreateLogger();
```


### 2. Defining in Configuration file

```xml
<appSettings>
	<add key="serilog:using:SizeRollingFile" value="Serilog.Sinks.RollingFile.Extension"/>
    <add key="serilog:write-to:SizeRollingFile.pathFormat" value="C:\temp\log.txt"/>
    <add key="serilog:write-to:SizeRollingFile.fileSizeLimitBytes" value="10485760"/>
    <add key="serilog:write-to:SizeRollingFile.retainedFileDurationLimit" value="2.00:00:00"/>
</appSettings>
```
    
