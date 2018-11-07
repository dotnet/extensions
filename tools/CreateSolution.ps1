dotnet new sln;
foreach ($proj in (Get-ChildItem *.csproj -Recurse))
{
    dotnet sln add $proj.FullName
}