#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

Describe 'PR coverage comments' {
    BeforeEach {
        $validationScript = Join-Path $PSScriptRoot 'ValidateProjectCoverage.ps1'
        $pwsh = (Get-Process -Id $PID).Path
        $repo = Join-Path $TestDrive ([guid]::NewGuid().ToString())
        New-Item $repo -ItemType Directory | Out-Null
        Push-Location $repo

        function Write-FixtureFile {
            param([string]$Path, [string]$Content)
            $directory = Split-Path $Path
            if ($directory) {
                New-Item $directory -ItemType Directory -Force | Out-Null
            }
            Set-Content $Path $Content
        }

        function Save-FixtureCommit {
            git add . -- ':!coverage-report.md' ':!coverage.xml'
            git -c user.name=CoverageTests -c user.email=coverage@example.com commit --quiet -m Fixture
            if ($LASTEXITCODE -ne 0) { throw 'Cannot commit coverage test fixture.' }
        }

        function Invoke-CoverageValidation {
            param([string]$Base = 'HEAD^1')
            $arguments = @{}
            if ($Base) { $arguments.PullRequestBase = $Base }
            $output = & $pwsh -NoProfile -File $validationScript -CoberturaReportXml coverage.xml @arguments 2>&1 | Out-String
            $comment = ($output -split "`r?`n" | Where-Object { $_ -like '##vso[[]task.setvariable variable=GITHUB_COMMENT]*' }) -join "`n"
            return @{
                Output = $output
                Comment = $comment
                ExitCode = $LASTEXITCODE
            }
        }

        git init --quiet --initial-branch=main
        foreach ($name in 'A', 'B', 'C', 'D') {
            $minimum = if ($name -eq 'B') { 100 } else { 75 }
            Write-FixtureFile "src/Libraries/$name/$name.csproj" "<Project><PropertyGroup><AssemblyName>Assembly.$name</AssemblyName><MinCodeCoverage>$minimum</MinCodeCoverage></PropertyGroup></Project>"
            Write-FixtureFile "src/Libraries/$name/Code.cs" '// Original'
        }
        Write-FixtureFile 'test/Libraries/A.Tests/A.Tests.csproj' '<Project><ItemGroup><ProjectReference Include="../../../src/Libraries/A/A.csproj" /><ProjectReference Include="../../../src/Libraries/C/C.csproj" /></ItemGroup></Project>'
        Write-FixtureFile 'test/Libraries/A.Tests/Test.cs' '// Original'
        Write-FixtureFile 'src/Shared/Shared.csproj' '<Project />'
        Write-FixtureFile 'src/Shared/Helper.cs' '// Original'
        Write-FixtureFile 'docs/coverage.md' 'Original'
        Save-FixtureCommit

        Write-FixtureFile 'coverage.xml' @'
<coverage><packages>
  <package name="Assembly.A" line-rate="0.60" branch-rate="0.65" />
  <package name="Assembly.B" line-rate="0.99" branch-rate="1" />
  <package name="Assembly.C" line-rate="0.90" branch-rate="0.95" />
  <package name="Assembly.D" line-rate="0.95" branch-rate="0.96" />
</packages></coverage>
'@
    }

    AfterEach {
        Pop-Location
    }

    It 'filters both shortfalls and kudos while retaining full validation and the saved report' {
        Write-FixtureFile 'src/Libraries/A/Code.cs' '// Changed'
        Write-FixtureFile 'src/Libraries/C/Code.cs' '// Changed'
        Save-FixtureCommit

        $result = Invoke-CoverageValidation
        $result.Comment | Should -Match '\| Assembly.A \| Line'
        $result.Comment | Should -Match '\| Assembly.A \| Branch'
        $result.Comment | Should -Match '\| Assembly.C \| 75 \| \*\*90\*\*'
        $result.Comment | Should -Not -Match 'Assembly.B|Assembly.D'
        $result.Output | Should -Match 'Breaking issues detected'
        $result.ExitCode | Should -Not -Be 0
        $report = Get-Content coverage-report.md -Raw
        $report | Should -Match 'Assembly.B'
        $report | Should -Match 'Assembly.D'
    }

    It 'includes source projects referenced by changed tests, using their assembly names' {
        Write-FixtureFile 'test/Libraries/A.Tests/Test.cs' '// Changed'
        Save-FixtureCommit

        $result = Invoke-CoverageValidation
        $result.Comment | Should -Match 'Assembly.A'
        $result.Comment | Should -Match 'Assembly.C'
        $result.Comment | Should -Not -Match 'Assembly.B|Assembly.D'
    }

    It 'includes both the old and new owning projects for a renamed file' {
        git mv src/Libraries/A/Code.cs src/Libraries/C/Moved.cs
        Save-FixtureCommit

        $result = Invoke-CoverageValidation
        $result.Comment | Should -Match 'Assembly.A'
        $result.Comment | Should -Match 'Assembly.C'
        $result.Comment | Should -Not -Match 'Assembly.B|Assembly.D'
    }

    It 'does not generate a comment when there are no relevant findings' {
        Write-FixtureFile 'docs/coverage.md' 'Changed'
        Save-FixtureCommit

        $result = Invoke-CoverageValidation
        $result.Comment | Should -BeNullOrEmpty
        $result.ExitCode | Should -Not -Be 0
    }

    It 'does not generate empty sections when the changed project meets its threshold' {
        Write-FixtureFile 'src/Libraries/A/Code.cs' '// Changed'
        Save-FixtureCommit
        $coverage = (Get-Content coverage.xml -Raw).Replace('0.60', '0.75').Replace('0.65', '0.75')
        Set-Content coverage.xml $coverage

        $result = Invoke-CoverageValidation
        $result.Comment | Should -BeNullOrEmpty
        $result.ExitCode | Should -Not -Be 0
    }

    It 'keeps all projects for shared configuration or code changes: <Path>' -TestCases @(
        @{ Path = 'eng/CodeCoverage.config' }
        @{ Path = 'Directory.Build.props' }
        @{ Path = 'src/Shared/Helper.cs' }
        @{ Path = 'test/Directory.Build.targets' }
        @{ Path = '.config/dotnet-tools.json' }
        @{ Path = 'build.sh' }
        @{ Path = 'build.cmd' }
    ) {
        param($Path)
        Write-FixtureFile $Path 'Changed'
        Save-FixtureCommit

        $result = Invoke-CoverageValidation
        foreach ($name in 'A', 'B', 'C', 'D') {
            $result.Comment | Should -Match "Assembly.$name"
        }
    }

    It 'preserves unfiltered comments when no PR base is supplied' {
        $result = Invoke-CoverageValidation -Base ''
        foreach ($name in 'A', 'B', 'C', 'D') {
            $result.Comment | Should -Match "Assembly.$name"
        }
    }

    It 'compares a depth-two PR merge checkout against its target parent, excluding target-only changes' {
        git checkout --quiet -b feature
        Write-FixtureFile 'src/Libraries/A/Code.cs' '// PR change'
        Save-FixtureCommit
        git checkout --quiet main
        Write-FixtureFile 'src/Libraries/D/Code.cs' '// Target-only change'
        Save-FixtureCommit
        git -c user.name=CoverageTests -c user.email=coverage@example.com merge --quiet --no-ff feature -m Merge
        if ($LASTEXITCODE -ne 0) { throw 'Cannot create coverage test merge commit.' }

        $clone = Join-Path $TestDrive ([guid]::NewGuid().ToString())
        git clone --quiet --depth=2 --no-local $repo $clone
        if ($LASTEXITCODE -ne 0) { throw 'Cannot create shallow coverage test checkout.' }
        Copy-Item coverage.xml $clone
        Push-Location $clone
        try {
            (git rev-parse --is-shallow-repository) | Should -Be 'true'
            $result = Invoke-CoverageValidation
            $result.Comment | Should -Match 'Assembly.A'
            $result.Comment | Should -Not -Match 'Assembly.B|Assembly.C|Assembly.D'
        }
        finally {
            Pop-Location
        }
    }

    It 'fails explicitly when the PR base is unavailable' {
        $result = Invoke-CoverageValidation -Base 'missing-revision'
        $result.Output | Should -Match 'Cannot determine changed files'
        $result.ExitCode | Should -Not -Be 0
        $result.Comment | Should -BeNullOrEmpty
    }
}
