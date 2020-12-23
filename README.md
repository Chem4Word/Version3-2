# Introduction 
This project is Version 3.2 of the Chemistry for Word Add-In

## Getting Started
1.	Installation process see $/docs/Chem4Word-Version3-2-Developer-SetUp.docx
2.	Software dependencies Office 2010/2013/2016/2019/365 (Installed on Desktop)
3.	Latest releases

## Build and Test
The Chemistry for Word Add-in (Chem4Word) is contained within a single Visual Studio solution located at src/Chem4Word.V3-2.sln
This solution has two main projects (use Set as Start Up then run the project), from time to time there may be other utility or PoC projects.
1. Chem4Word.V3 is the Add-In
2. WinForms.TestHarness this allows testing of a the Editing subset of operations without starting MS Word

All unit tests are written with XUnit

## List of NuGet packages
| Package | Version | Licence |
|--|--|--|
|DocumentFormat.OpenXml|2.11.3|MIT|
|DotNetProjects.WpfToolkit.Input|6.0.85|MS-PL|
|EntityFramework|6.4.4|Apache-2.0|
|Jacobslusser.ScintillaNET|3.6.3|MIT|
|Microsoft.Azure.KeyVault.Core|3.0.5|MIT|
|Microsoft.Azure.Services.AppAuthentication|1.6.0|MIT|
|Microsoft.IdentityModel.Clients.ActiveDirectory|5.2.8|MIT|
|Microsoft.IdentityModel.JsonWebTokens|6.8.0|MIT|
|Microsoft.IdentityModel.Logging|6.8.0|MIT|
|Microsoft.IdentityModel.Tokens|6.8.0|MIT|
|Microsoft.Rest.ClientRuntime.Azure|3.3.19|MIT|
|Microsoft.Rest.ClientRuntime|2.3.22|MIT|
|Microsoft_VisualStudio_QualityTools_UnitTestFramework.STW|12.0.21005.1|Microsoft ?|
|Newtonsoft.Json|12.0.3|MIT|
|Ookii.Dialogs.WindowsForms|1.0|Public Domain|
|Stub.System.Data.SQLite.Core.NetFramework|1.0.113.3|Public Domain|
|System.Buffers|4.5.1|MIT|
|System.Collections.Immutable|5.0.0|MIT|
|System.Data.SQLite.Core|1.0.113.6|Public Domain|
|System.Data.SQLite.EF6|1.0.113.0|Public Domain|
|System.Data.SQLite.Linq|1.0.113.0|Public Domain|
|System.Data.SQLite|1.0.113.6|Public Domain|
|System.IdentityModel.Tokens.Jwt|6.8.0|MIT|
|System.IO.FileSystem.Primitives|4.3.0|MS-.NET-Library License|
|System.IO.Packaging|5.0.0|MIT|
|System.Memory|4.5.4|MIT|
|System.Net.Http|4.3.4|MS-.NET-Library License|
|System.Numerics.Vectors|4.5.0|MIT|
|System.Runtime.CompilerServices.Unsafe|5.0.0|MIT|
|System.Security.Cryptography.Algorithms|4.3.1|MS-.NET-Library License|
|System.Security.Cryptography.Encoding|4.3.0|MS-.NET-Library License|
|System.Security.Cryptography.Primitives|4.3.0|MS-.NET-Library License|
|System.Security.Cryptography.X509Certificates|4.3.2|MS-.NET-Library License|
|System.ValueTuple|4.5.0|MIT|
|System.Windows.Interactivity.WPF|2.0.20525|Inherits from MS Expression Blend 4 ??|
|Unofficial.Ionic.Zip|1.9.1.8|Unknown ?|
|VirtualizingWrapPanel|1.5.3|MIT|
|WindowsAzure.ServiceBus|6.0.2|MS-.NET-Library License|
|xunit.abstractions|2.0.3|Apache-2.0|
|xunit.analyzers|0.10.0|Apache-2.0|
|xunit.assert|2.4.1|Apache-2.0|
|xunit.core|2.4.1|Apache-2.0|
|xunit.extensibility.core|2.4.1|Apache-2.0|
|xunit.extensibility.execution|2.4.1|Apache-2.0|
|xunit.runner.console|2.4.1|Apache-2.0|
|xunit.runner.visualstudio|2.4.2|Apache-2.0|
|xunit|2.4.1|Apache-2.0|

## Acknowledgements
1. [CEVOpen](https://github.com/petermr/CEVOpen) - This data represents about 2100 unique chemical names of volatile plant chemicals (essential oils) from the EssoilDB 1.0 database (compiled from the scientific literature over about 10 years in Dr Yadav's laboratory). They are made available for re-use by anyone for any purpose (CC0). We would appreciate acknowledgement of EssoilDB and the following people who extracted and cleaned the data during 2019. (Gitanjali Yadav, Ambarish Kumar, Peter Murray-Rust).

## Contribute
Please feel free to contribute to the project.
Create your own branch, make your changes then create a Pull Request to initiate a merge into the master branch.
