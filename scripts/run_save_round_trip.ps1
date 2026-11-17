param([string]$RuntimeAssembly = "Temp/spec-20261117/Assembly-CSharp.dll")
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
$runtimePath = (Resolve-Path -LiteralPath $RuntimeAssembly).Path
$auditDirectory = Join-Path $projectRoot 'Temp/spec-save-20261117'
$managedDirectory = Join-Path $auditDirectory 'StandaloneManaged'
New-Item -ItemType Directory -Force $managedDirectory | Out-Null
$backupPath = Join-Path $auditDirectory 'railway2_local_v1.before.json'
if (!(Test-Path -LiteralPath $backupPath)) { Copy-Item -LiteralPath 'Assets/Resources/TestSaves/railway2_local_v1.json' -Destination $backupPath }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'managed/SaveRoundTripAudit.cs') -Destination $managedDirectory -Force
$escapedAssembly = [System.Security.SecurityElement]::Escape($runtimePath)
@"
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup>
<ItemGroup>
<Reference Include="Assembly-CSharp"><HintPath>$escapedAssembly</HintPath></Reference>
<Reference Include="UnityEngine.CoreModule"><HintPath>E:/Unity/6000.6.8f1/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.dll</HintPath></Reference>
</ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $managedDirectory 'SaveAudit.csproj') -Encoding utf8
@'
using System.IO;
using System.Runtime.Loader;
class SaveAuditMain {
 static void Main() {
  AssemblyLoadContext.Default.Resolving += (context,name)=> {
   string path=Path.Combine("E:/Unity/6000.6.8f1/Editor/Data/Managed/UnityEngine",name.Name+".dll");
   if(!File.Exists(path))path=Path.Combine("Library/ScriptAssemblies",name.Name+".dll");
   return File.Exists(path)?context.LoadFromAssemblyPath(Path.GetFullPath(path)):null;
  };
  SaveRoundTripAudit.Run();
 }
}
'@ | Set-Content -LiteralPath (Join-Path $managedDirectory 'Program.cs') -Encoding utf8
& dotnet build (Join-Path $managedDirectory 'SaveAudit.csproj') --verbosity quiet -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Managed audit compilation failed' }
& dotnet (Join-Path $managedDirectory 'bin/Debug/net10.0/SaveAudit.dll')
if ($LASTEXITCODE -ne 0) { throw 'Save round-trip audit failed' }
