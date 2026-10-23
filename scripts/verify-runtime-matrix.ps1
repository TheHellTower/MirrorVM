param(
    [string[]]$TargetFrameworks,
    [string]$DotNetHost,
    [switch]$X86
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $DotNetHost) {
    $DotNetHost = if ($X86) { Join-Path $root "Release\Samples\LegacyRuntimesX86\dotnet.exe" } else { Join-Path $root "Release\Samples\LegacyRuntimes\dotnet.exe" }
    if (-not $X86 -and -not (Test-Path $DotNetHost)) { $DotNetHost = "dotnet" }
}
if (Test-Path $DotNetHost) { $DotNetHost = (Resolve-Path $DotNetHost).Path }
$targets = @(
    "net20", "net30", "net35", "net40", "net45", "net451", "net452", "net46",
    "net461", "net462", "net47", "net471", "net472", "net48", "net481",
    "netcoreapp2.0", "netcoreapp2.1", "netcoreapp2.2", "netcoreapp3.0", "netcoreapp3.1",
    "net5.0", "net6.0", "net7.0", "net8.0", "net9.0", "net10.0"
)
if (-not $TargetFrameworks -or $TargetFrameworks.Count -eq 0) {
    $TargetFrameworks = $targets
}
$corFlags = if ($X86) {
    Join-Path ${env:ProgramFiles(x86)} "Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\CorFlags.exe"
} else { $null }
if ($corFlags -and -not (Test-Path $corFlags)) {
    throw "CorFlags.exe is required to run Framework samples as x86."
}

$runtimeList = & $DotNetHost --list-runtimes
$tool = Join-Path $root "Release\Release\net9.0\MirrorVM.dll"
$verificationRoot = Join-Path $root "Release\Samples\Verification"
New-Item -ItemType Directory -Path $verificationRoot -Force | Out-Null

function Invoke-Sample {
    param([string]$Path, [string]$Directory, [bool]$Modern, [string]$HostPath)

    Push-Location $Directory
    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        if ($Modern) {
            $lines = @('') | & $HostPath $Path 2>&1
        }
        else {
            $lines = @('') | & $Path 2>&1
        }

        return [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Lines = @($lines | ForEach-Object { $_.ToString().TrimEnd("`r") })
        }
    }
    finally {
        $ErrorActionPreference = $previousPreference
        Pop-Location
    }
}

foreach ($target in $TargetFrameworks) {
    if ($targets -notcontains $target) {
        throw "Unknown target framework: $target"
    }

    # The sample uses only 2.0 APIs; Framework 3.0 runs on the same CLR 2.0 line.
    $buildTarget = if ($target -eq "net30") { "net20" } else { $target }
    $isFramework = $target -match "^net\d+$"
    $isModernDotNet = -not $isFramework
    if ($isModernDotNet) {
        $runtimeVersion = $target -replace "^netcoreapp", "" -replace "^net", ""
        if (-not ($runtimeList | Where-Object { $_ -match "^Microsoft\.NETCore\.App\s+$([regex]::Escape($runtimeVersion))\." })) {
            throw "Required .NET runtime $runtimeVersion is not installed for $target."
        }
    }

    $outputDirectory = Join-Path $root "Release\Samples\Release\$buildTarget"
    $inputName = if ($isFramework) { "MirrorVM.Sample.exe" } else { "MirrorVM.Sample.dll" }
    $protectedName = if ($isFramework) { "MirrorVM.Sample_MVM.exe" } else { "MirrorVM.Sample_MVM.dll" }
    $inputPath = Join-Path $outputDirectory $inputName
    if (-not (Test-Path $inputPath) -or -not (Test-Path $tool)) {
        throw "Build MirrorVM.sln in Release before running the matrix. Missing $inputPath or $tool."
    }

    $workDirectory = Join-Path $verificationRoot ([guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $workDirectory | Out-Null
    try {
        Copy-Item -Path (Join-Path $outputDirectory "*") -Destination $workDirectory -Recurse
        $workInput = Join-Path $workDirectory $inputName
        if ($X86 -and $isFramework) {
            & $corFlags $workInput /32BITREQ+ /nologo
            if ($LASTEXITCODE -ne 0) {
                throw "$target could not be configured for x86 execution."
            }
        }

        $original = Invoke-Sample $workInput $workDirectory $isModernDotNet $DotNetHost
        if ($original.ExitCode -ne 0) {
            throw "$target original sample exited with code $($original.ExitCode):`n$($original.Lines -join "`n")"
        }
        $protectionOutput = & dotnet $tool $workInput 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "$target protection failed:`n$($protectionOutput -join "`n")"
        }

        $protectionText = $protectionOutput -join "`n"
        foreach ($method in @("Choose", "SumTo", "SwitchChoice", "LongSwitchBranch", "UnsignedAtLeast", "FloatLessThan", "NestedChoice", "IsNull", "UseOrFallback", "LoneSurrogate", "ChooseNullOrValue", "VoidReturn")) {
            if ($protectionText -notmatch [regex]::Escape("MirrorVM.Sample.ControlFlowFixtures::$method")) {
                throw "$target did not virtualize ControlFlowFixtures::${method}:`n$($protectionOutput -join "`n")"
            }
        }
        foreach ($method in @("BooleanIdentity", "SByteIdentity", "ByteIdentity", "Int16Identity", "UInt16Identity", "CharIdentity", "Int32Identity", "UInt32Identity", "Int64Identity", "UInt64Identity", "IntPtrIdentity", "UIntPtrIdentity", "SingleIdentity", "DoubleIdentity", "DecimalIdentity", "StringIdentity")) {
            if ($protectionText -notmatch [regex]::Escape("MirrorVM.Sample.NumericFixtures::$method")) {
                throw "$target did not virtualize NumericFixtures::${method}:`n$($protectionOutput -join "`n")"
            }
        }

        $protectedPath = Join-Path $workDirectory $protectedName
        if ($X86 -and $isFramework) {
            & $corFlags $protectedPath /32BITREQ+ /nologo
            if ($LASTEXITCODE -ne 0) {
                throw "$target could not be configured for x86 execution."
            }
        }
        $actual = Invoke-Sample $protectedPath $workDirectory $isModernDotNet $DotNetHost
        if ($actual.ExitCode -ne 0) {
            throw "$target protected sample exited with code $($actual.ExitCode):`n$($actual.Lines -join "`n")"
        }
        if (($actual.Lines -join "`n") -cne ($original.Lines -join "`n")) {
            throw "$target protected output differed from the original CLR output:`n$($actual.Lines -join "`n")"
        }

        Write-Host "$target passed"
    }
    finally {
        $resolvedWork = [System.IO.Path]::GetFullPath($workDirectory)
        $resolvedRoot = [System.IO.Path]::GetFullPath($verificationRoot) + [System.IO.Path]::DirectorySeparatorChar
        if ($resolvedWork.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedWork -Recurse -Force
        }
    }
}
