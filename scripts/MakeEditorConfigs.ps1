    #!/usr/bin/env pwsh
<#
.DESCRIPTION
    Builds and invokes the DiagConfig tool to generate the .editorconfig files we use within the source base
#>

$Project = $PSScriptRoot + "/../eng/Tools/DiagConfig/DiagConfig.csproj"
$Command = $PSScriptRoot + "/../artifacts/bin/DiagConfig/Debug/net8.0/DiagConfig.exe"
$Diags = $PSScriptRoot + "/../eng/Diags"

Write-Output "Building DiagConfig tool"

& dotnet build $Project --nologo --verbosity q

Write-Output "Creating .editorconfig files"

# The files we use for this repo
& $Command $Diags editorconfig save --exclude xunit.analyzers src/Analyzers/.editorconfig       general,performance,production
& $Command $Diags editorconfig save --exclude xunit.analyzers src/Generators/.editorconfig      general,performance,production
& $Command $Diags editorconfig save --exclude xunit.analyzers src/Libraries/.editorconfig       general,api,performance,production
& $Command $Diags editorconfig save --exclude xunit.analyzers src/LegacySupport/.editorconfig  general,performance,production
& $Command $Diags editorconfig save --exclude xunit.analyzers src/Shared/.editorconfig          general,performance,production
& $Command $Diags editorconfig save --exclude xunit.analyzers bench/.editorconfig               general,performance
& $Command $Diags editorconfig save --exclude xunit.analyzers eng/Tools/.editorconfig           general
& $Command $Diags editorconfig save                           test/.editorconfig                general,test

# The files we publish with the M.E.StaticAnalysis package

& $Command $Diags editorconfig save --max-tier 1 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/General-Tier1.globalconfig       general --is-global
& $Command $Diags editorconfig save --max-tier 1 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/ProdExe-Tier1.globalconfig       general,performance,production --is-global
& $Command $Diags editorconfig save --max-tier 1 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/ProdLib-Tier1.globalconfig       general,api,performance,production --is-global
& $Command $Diags editorconfig save --max-tier 1 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/NonProdExe-Tier1.globalconfig    general --is-global
& $Command $Diags editorconfig save --max-tier 1 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/NonProdLib-Tier1.globalconfig    general,api --is-global
& $Command $Diags editorconfig save --max-tier 1 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/Benchmark-Tier1.globalconfig     general,performance --is-global
& $Command $Diags editorconfig save --max-tier 1 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle                 src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/Test-Tier1.globalconfig          general,test --is-global

& $Command $Diags editorconfig save --max-tier 2 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/General-Tier2.globalconfig       general --is-global
& $Command $Diags editorconfig save --max-tier 2 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/ProdExe-Tier2.globalconfig       general,performance,production --is-global
& $Command $Diags editorconfig save --max-tier 2 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/ProdLib-Tier2.globalconfig       general,api,performance,production --is-global
& $Command $Diags editorconfig save --max-tier 2 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/NonProdExe-Tier2.globalconfig    general --is-global
& $Command $Diags editorconfig save --max-tier 2 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/NonProdLib-Tier2.globalconfig    general,api --is-global
& $Command $Diags editorconfig save --max-tier 2 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/Benchmark-Tier2.globalconfig     general,performance --is-global
& $Command $Diags editorconfig save --max-tier 2 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle                 src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/Test-Tier2.globalconfig          general,test --is-global

& $Command $Diags editorconfig save --max-tier 3 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/General.globalconfig       general --is-global
& $Command $Diags editorconfig save --max-tier 3 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/ProdExe.globalconfig       general,performance,production --is-global
& $Command $Diags editorconfig save --max-tier 3 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/ProdLib.globalconfig       general,api,performance,production --is-global
& $Command $Diags editorconfig save --max-tier 3 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/NonProdExe.globalconfig    general --is-global
& $Command $Diags editorconfig save --max-tier 3 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/NonProdLib.globalconfig    general,api --is-global
& $Command $Diags editorconfig save --max-tier 3 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle,xunit.analyzers src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/Benchmark.globalconfig     general,performance --is-global
& $Command $Diags editorconfig save --max-tier 3 --exclude StyleCop.Analyzers,Microsoft.CodeAnalysis.CodeStyle,Microsoft.CodeAnalysis.CSharp.CodeStyle                 src/Packages/Microsoft.Extensions.StaticAnalysis/build/config/Test.globalconfig          general,test --is-global
