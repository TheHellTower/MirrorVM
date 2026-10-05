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
$pointerSize = if ($X86) { "4" } else { "8" }
$targets = @(
    "net20", "net30", "net35", "net40", "net45", "net451", "net452", "net46",
    "net461", "net462", "net47", "net471", "net472", "net48", "net481",
    "netcoreapp2.0", "netcoreapp2.1", "netcoreapp2.2", "netcoreapp3.0", "netcoreapp3.1",
    "net5.0", "net6.0", "net7.0", "net8.0", "net9.0", "net10.0"
)
$expected = @(
    "8", "42", "42", "42", "42", "Test", "True", "-101", "251", "-30001", "60001",
    "True", "-2147483636", "4294967295", "-9223372036854775796", "18446744073709551615",
    "42", "42", "1.25", "2.5", "123.45", "1161981756646125696", "1.25", "1.25",
    "42", "42", "9223372036854775807", "0.75", "0.75", "0",
    "1", "42", "42", "42", "True", "48", "243", "195", "-1", "16", "-4", "1",
    "True", "True", "True", "True", "True", "checked:overflow"
)

if (-not $TargetFrameworks -or $TargetFrameworks.Count -eq 0) {
    $TargetFrameworks = $targets
}
$expected[0] = $pointerSize
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
        $protectionOutput = & dotnet $tool $workInput 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "$target protection failed:`n$($protectionOutput -join "`n")"
        }

        $protectedPath = Join-Path $workDirectory $protectedName
        if ($X86 -and $isFramework) {
            & $corFlags $protectedPath /32BITREQ+ /nologo
            if ($LASTEXITCODE -ne 0) {
                throw "$target could not be configured for x86 execution."
            }
        }
        Push-Location $workDirectory
        $previousPreference = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        try {
            if ($isModernDotNet) {
                $actual = @('') | & $DotNetHost $protectedPath 2>&1
            }
            else {
                $actual = @('') | & $protectedPath 2>&1
            }
            $runExitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousPreference
            Pop-Location
        }

        if ($runExitCode -ne 0) {
            throw "$target protected sample exited with code ${runExitCode}:`n$($actual -join "`n")"
        }

        $actualLines = @($actual | ForEach-Object { $_.ToString().TrimEnd("`r") })
        if (($actualLines -join "`n") -cne ($expected -join "`n")) {
            throw "$target output did not match the CLR reference values:`n$($actualLines -join "`n")"
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
