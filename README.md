# Socket C# Programming Example

This is a very-very experimental Simple Chat protocol, using UDP, in single network

This is CLI based app (will be ported to AvaloniaUI), used as demonstration on Computer Network Course in SIB ISTTS Surabaya. 

For implemented Chat server, only UDP Chat Service is for now. As there are no TCP implementation other than skeleton mode. 

## Preq/requirement

1. .NET Core 6.0 LTS (min)
2. Any Linux or Windows CLI

## How to Restore and Run

```
dotnet restore
dotnet run
```

## How to Build as StandAlone

By default all app is x64, there are no x86/32 bit. You can build this either in Linux or Windows, or OSX and vice versa. Look more at [this docs](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog)

### Linux
```
dotnet publish --os linux --self-contained true -c Release
```

### Windows 
```
dotnet publish --os win --self-contained true -c Release
```

### MacOS
```
dotnet publish --os osx --self-contained true -c Release
```