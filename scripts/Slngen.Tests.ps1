[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingEmptyCatchBlock', '', Justification = 'False positive')]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', '', Justification = 'False positive')]
param()

Describe "Slngen.ps1" {
    BeforeAll {
        function Invoke-SlngenExe (
            $Folders,
            $OutSln,
            $Globs,
            $NoLaunch,
            $Exclude,
            $ConsoleOutput,
            $MSBuild) {}

        function Compare-Array {
            $($args[0] -join " ") -eq $($args[1] -join " ")
        }

        $DefaultExcludePath = "--exclude src\Tools\MutationTesting\samples\ --exclude src\Templates\templates"
        $DefaultSlnPath = '"' + (Join-Path -Path (Get-Location) -ChildPath "SDK.sln") + '"'
        $PollyKeywordGlobs = 'test/TestUtilities/TestUtilities.csproj src/**/*Polly*/**/*.*sproj test/**/*Polly*/**/*.*sproj bench/**/*Polly*/**/*.*sproj int_test/**/*Polly*/**/*.*sproj docs/**/*Polly*/**/*.*sproj'
        $PollyHttpKeywordsGlobs = 'test/TestUtilities/TestUtilities.csproj src/**/*Polly*/**/*.*sproj test/**/*Polly*/**/*.*sproj bench/**/*Polly*/**/*.*sproj int_test/**/*Polly*/**/*.*sproj docs/**/*Polly*/**/*.*sproj src/**/*Http*/**/*.*sproj test/**/*Http*/**/*.*sproj bench/**/*Http*/**/*.*sproj int_test/**/*Http*/**/*.*sproj docs/**/*Http*/**/*.*sproj'
    }

    Context "Invoke-SlngenExe with test cases from examples" {
        BeforeEach {
            #Arrange
            Mock Invoke-SlngenExe {}
        }
        It "Runs slngen with default params" {
            #Act
            . $PSScriptRoot/Slngen.ps1

            #Assert
            Should -Invoke -CommandName Invoke-SlngenExe -Times 1 -ParameterFilter {
                $PesterBoundParameters.Folders -eq $false -and
                $PesterBoundParameters.OutSln -eq $DefaultSlnPath -and
                (Compare-Array $PesterBoundParameters.Globs 'test/TestUtilities/TestUtilities.csproj src/**/*.*sproj test/**/*.*sproj bench/**/*.*sproj int_test/**/*.*sproj') -and
                $PesterBoundParameters.NoLaunch -eq $false -and
                (Compare-Array $PesterBoundParameters.Exclude $DefaultExcludePath) -and
                $PesterBoundParameters.ConsoleOutput -eq $null -and
                $PesterBoundParameters.MSBuild -eq $null
            }
        }

        It "Runs slngen without integration tests" {
            #Act
            . $PSScriptRoot/Slngen.ps1 -IntegrationTests:$false

            #Assert
            Should -Invoke -CommandName Invoke-SlngenExe -Times 1 -ParameterFilter {
                $PesterBoundParameters.Folders -eq $false -and
                $PesterBoundParameters.OutSln -eq $DefaultSlnPath -and
                (Compare-Array $PesterBoundParameters.Globs 'test/TestUtilities/TestUtilities.csproj src/**/*.*sproj test/**/*.*sproj bench/**/*.*sproj') -and
                $PesterBoundParameters.NoLaunch -eq $false -and
                (Compare-Array $PesterBoundParameters.Exclude $DefaultExcludePath) -and
                $PesterBoundParameters.ConsoleOutput -eq $null -and
                $PesterBoundParameters.MSBuild -eq $null
            }
        }

        It "Runs slngen without benchmarks" {
            #Act
            . $PSScriptRoot/Slngen.ps1 -BenchmarkTests:$false

            #Assert
            Should -Invoke -CommandName Invoke-SlngenExe -Times 1 -ParameterFilter {
                $PesterBoundParameters.Folders -eq $false -and
                $PesterBoundParameters.OutSln -eq $DefaultSlnPath -and
                (Compare-Array $PesterBoundParameters.Globs 'test/TestUtilities/TestUtilities.csproj src/**/*.*sproj test/**/*.*sproj int_test/**/*.*sproj') -and
                $PesterBoundParameters.NoLaunch -eq $false -and
                (Compare-Array $PesterBoundParameters.Exclude $DefaultExcludePath) -and
                $PesterBoundParameters.ConsoleOutput -eq $null -and
                $PesterBoundParameters.MSBuild -eq $null
            }
        }

        It "Runs slngen with a keyword Polly" {
            #Act
            . $PSScriptRoot/Slngen.ps1 -Keywords "Polly"

            #Assert
            Should -Invoke -CommandName Invoke-SlngenExe -Times 1 -ParameterFilter {
                $PesterBoundParameters.Folders -eq $false -and
                $PesterBoundParameters.OutSln -eq $DefaultSlnPath -and
                (Compare-Array $PesterBoundParameters.Globs $PollyKeywordGlobs) -and
                $PesterBoundParameters.NoLaunch -eq $false -and
                (Compare-Array $PesterBoundParameters.Exclude $DefaultExcludePath) -and
                $PesterBoundParameters.ConsoleOutput -eq $null -and
                $PesterBoundParameters.MSBuild -eq $null
            }
        }

        It "Runs slngen with keywords Polly, Http" {
            #Act
            . $PSScriptRoot/Slngen.ps1 -Keywords "Polly", "Http"

            #Assert
            Should -Invoke -CommandName Invoke-SlngenExe -Times 1 -ParameterFilter {
                $PesterBoundParameters.Folders -eq $false -and
                $PesterBoundParameters.OutSln -eq $DefaultSlnPath -and
                (Compare-Array $PesterBoundParameters.Globs $PollyHttpKeywordsGlobs) -and
                $PesterBoundParameters.NoLaunch -eq $false -and
                (Compare-Array $PesterBoundParameters.Exclude $DefaultExcludePath) -and
                $PesterBoundParameters.ConsoleOutput -eq $null -and
                $PesterBoundParameters.MSBuild -eq $null
            }
        }

        It "Runs slngen with keywords Polly, Http and Folders switch on" {
            #Act
            . $PSScriptRoot/Slngen.ps1 -Keywords "Polly", "Http" -Folders

            #Assert
            Should -Invoke -CommandName Invoke-SlngenExe -Times 1 -ParameterFilter {
                $PesterBoundParameters.Folders -eq $true -and
                $PesterBoundParameters.OutSln -eq $DefaultSlnPath -and
                (Compare-Array $PesterBoundParameters.Globs $PollyHttpKeywordsGlobs) -and
                $PesterBoundParameters.NoLaunch -eq $false -and
                (Compare-Array $PesterBoundParameters.Exclude $DefaultExcludePath) -and
                $PesterBoundParameters.ConsoleOutput -eq $null -and
                $PesterBoundParameters.MSBuild -eq $null
            }
        }

        It "Runs slngen with keywords Polly, Http and NoLaunch switch on" {
            #Act
            . $PSScriptRoot/Slngen.ps1 -Keywords "Polly", "Http" -NoLaunch

            #Assert
            Should -Invoke -CommandName Invoke-SlngenExe -Times 1 -ParameterFilter {
                $PesterBoundParameters.Folders -eq $false -and
                $PesterBoundParameters.OutSln -eq $DefaultSlnPath -and
                (Compare-Array $PesterBoundParameters.Globs $PollyHttpKeywordsGlobs) -and
                $PesterBoundParameters.NoLaunch -eq $true -and
                (Compare-Array $PesterBoundParameters.Exclude $DefaultExcludePath) -and
                $PesterBoundParameters.ConsoleOutput -eq $null -and
                $PesterBoundParameters.MSBuild -eq $null
            }
        }

        It "Runs slngen with keywords Polly, Http and custom sln name" {
            #Act
            . $PSScriptRoot/Slngen.ps1 -Keywords "Polly", "Http" -OutSln 'test.sln'

            #Assert
            Should -Invoke -CommandName Invoke-SlngenExe -Times 1 -ParameterFilter {
                $PesterBoundParameters.Folders -eq $false -and
                $PesterBoundParameters.OutSln -eq ('"' + (Join-Path -Path (Get-Location) -ChildPath "test.sln") + '"') -and
                (Compare-Array $PesterBoundParameters.Globs $PollyHttpKeywordsGlobs) -and
                $PesterBoundParameters.NoLaunch -eq $false -and
                (Compare-Array $PesterBoundParameters.Exclude $DefaultExcludePath) -and
                $PesterBoundParameters.ConsoleOutput -eq $null -and
                $PesterBoundParameters.MSBuild -eq $null
            }
        }

        It "Runs slngen with exclude paths" {
            #Act
            . $PSScriptRoot/Slngen.ps1 -All -ExcludePaths "testpath\testpath"

            #Assert
            Should -Invoke -CommandName Invoke-SlngenExe -Times 1 -ParameterFilter {
                $PesterBoundParameters.Folders -eq $false -and
                $PesterBoundParameters.OutSln -eq $DefaultSlnPath -and
                (Compare-Array $PesterBoundParameters.Globs 'test/TestUtilities/TestUtilities.csproj src/**/*.*sproj test/**/*.*sproj bench/**/*.*sproj int_test/**/*.*sproj') -and
                $PesterBoundParameters.NoLaunch -eq $false -and
                (Compare-Array $PesterBoundParameters.Exclude "--exclude testpath\testpath") -and
                $PesterBoundParameters.ConsoleOutput -eq $null -and
                $PesterBoundParameters.MSBuild -eq $null
            }
        }
    }
}