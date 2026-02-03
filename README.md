# Metaphrase

[![Build Action](https://github.com/HereticSoftware/Metaphrase/actions/workflows/build.yaml/badge.svg)](https://github.com/HereticSoftware/Metaphrase/actions/workflows/build.yaml)
[![Publish Action](https://github.com/HereticSoftware/Metaphrase/actions/workflows/publish.yaml/badge.svg)](https://github.com/HereticSoftware/Metaphrase/actions/workflows/publish.yaml)
[![License](https://img.shields.io/github/license/HereticSoftware/Metaphrase?style=flat)](https://github.com/HereticSoftware/Metaphrase/blob/main/LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET%208-%23512bd4?style=flat)](https://dotnet.microsoft.com/)
[![.NET 9](https://img.shields.io/badge/.NET%209-%23512bd4?style=flat)](https://dotnet.microsoft.com/)
[![Downloads](https://img.shields.io/nuget/dt/Metaphrase?style=flat)](https://www.nuget.org/packages/Metaphrase/)

A C# port of the [ngx-translate](https://github.com/ngx-translate/core). The port is not one to one and also aims to be more C# friendly where possible.

The library provides you with a `TranslateService` which combined with a `TranslateLoader` (HttpLoader built in) enables you to load, compile and display your translations. You format the strings keys. For a much stronger formatting a package that uses the awesome [SmartFormat](https://github.com/axuno/SmartFormat/) project is provided.

## Packages

| Package | Stable | Pre |
|:--|:--|:--|
| **Metaphrase** | [![Metaphrase](https://img.shields.io/nuget/v/Metaphrase)](https://www.nuget.org/packages/Metaphrase) | [![Metaphrase](https://img.shields.io/nuget/vpre/Metaphrase)](https://www.nuget.org/packages/Metaphrase) |
| **Metaphrase.SmartFormat** | [![Metaphrase.SmartFormat](https://img.shields.io/nuget/v/Metaphrase.SmartFormat)](https://www.nuget.org/packages/Metaphrase.SmartFormat) | [![Metaphrase.SmartFormat](https://img.shields.io/nuget/vpre/Metaphrase.SmartFormat)](https://www.nuget.org/packages/Metaphrase.SmartFormat) |

# Usage

Description
- Metaphrase
    - Contains the `abstractions`, `defaults`, `primitives`, `http loader` and the `service`.
- Metaphrase.SmartFormat
    - Contains the `SmartFormatParser`.

Installation:
- `Metaphrase` in projects that you want to use the service or any of the primitives.
- `Metaphrase.SmartFormat` in projects that use the service and you want to replace the default parser.

# Getting Started

The section will describe how to get started with Translate in a `Blazor Wasm` using the `HttpLoader` storing the language files at `wwwroot/i18n`.

1. Add the `Metaphrase` package.
```console
dotnet add package Metaphrase
```
2. Add the appropriate services to the service provider.
```csharp
services.AddScoped(sp => new HttpClient() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
services.AddScoped<TranslateLoader, TranslateHttpLoader>(); // This will use the default options
services.AddScoped<TranslateService>(); // This will use the default parser
```
3. Use the service

`TranslateService` also supports a "pipe" syntax to get your translation values.

For the tanslation `key: hello` and `value: Hello` you can do:
```csharp
translate.Instant("hello") // prints "Hello"
translate | "hello" // prints "Hello"
```

For the translation `key: welcome`, `value: Welcome {user}!` and `param: user`.
```csharp
translate.Instant("hello", new { user = "panos" }) // prints "Welcome panos"!
translate | "hello" | new { user = "panos" } // prints "Welcome panos"!
```

# Contributing

For general contribution information you can read the [Raven Tail Contributing document](https://github.com/HereticSoftware/.github/blob/main/CONTRIBUTING.md).

## Local Development

To develop you need:
1. dotnet 9.0 SDK
2. Visual Studio or VS Code with the C# extension.
3. Configured your IDE for the [TUnit](https://thomhurst.github.io/TUnit/) library.
