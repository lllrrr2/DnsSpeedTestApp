# Regression checks

Run on Windows with the repository's .NET SDK:

```powershell
dotnet run --project Tests/DNSSpeedTester.Checks.csproj -c Release
```

The checks use temporary configuration files and loopback servers. They do not
change the machine's DNS settings or contact public DNS servers.
