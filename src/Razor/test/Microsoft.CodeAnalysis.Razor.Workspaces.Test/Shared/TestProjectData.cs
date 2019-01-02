// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor
{
    // Used to abstract away platform-specific file/directory path information.
    //
    // The System.IO.Path methods don't processes Windows paths in a Windows way 
    // on *nix (rightly so), so we need to use platform-specific paths.
    //
    // Target paths are always Windows style.
    internal static class TestProjectData
    {
        static TestProjectData()
        {
            var baseDirectory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "c:\\users\\example\\src" : "/home/example";

            SomeProject = new HostProject(Path.Combine(baseDirectory, "SomeProject", "SomeProject.csproj"), RazorConfiguration.Default);
            SomeProjectFile1 = new HostDocument(Path.Combine(baseDirectory, "SomeProject", "File1.cshtml"), "File1.cshtml", FileKinds.Legacy);
            SomeProjectFile2 = new HostDocument(Path.Combine(baseDirectory, "SomeProject", "File2.cshtml"), "File2.cshtml", FileKinds.Legacy);
            SomeProjectImportFile = new HostDocument(Path.Combine(baseDirectory, "SomeProject", "_Imports.cshtml"), "_Imports.cshtml", FileKinds.Legacy);
            SomeProjectNestedFile3 = new HostDocument(Path.Combine(baseDirectory, "SomeProject", "Nested", "File3.cshtml"), "Nested\\File1.cshtml", FileKinds.Legacy);
            SomeProjectNestedFile4 = new HostDocument(Path.Combine(baseDirectory, "SomeProject", "Nested", "File4.cshtml"), "Nested\\File2.cshtml", FileKinds.Legacy);
            SomeProjectNestedImportFile = new HostDocument(Path.Combine(baseDirectory, "SomeProject", "Nested", "_Imports.cshtml"), "Nested\\_Imports.cshtml", FileKinds.Legacy);
            SomeProjectComponentFile1 = new HostDocument(Path.Combine(baseDirectory, "SomeProject", "File1.razor"), "File1.razor", FileKinds.Component);
            SomeProjectComponentFile2 = new HostDocument(Path.Combine(baseDirectory, "SomeProject", "File2.razor"), "File2.razor", FileKinds.Component);
            SomeProjectNestedComponentFile3 = new HostDocument(Path.Combine(baseDirectory, "SomeProject", "Nested", "File3.razor"), "Nested\\File1.razor", FileKinds.Component);
            SomeProjectNestedComponentFile4 = new HostDocument(Path.Combine(baseDirectory, "SomeProject", "Nested", "File4.razor"), "Nested\\File2.razor", FileKinds.Component);

            AnotherProject = new HostProject(Path.Combine(baseDirectory, "AnotherProject", "AnotherProject.csproj"), RazorConfiguration.Default);
            AnotherProjectFile1 = new HostDocument(Path.Combine(baseDirectory, "AnotherProject", "File1.cshtml"), "File1.cshtml", FileKinds.Legacy);
            AnotherProjectFile2 = new HostDocument(Path.Combine(baseDirectory, "AnotherProject", "File2.cshtml"), "File2.cshtml", FileKinds.Legacy);
            AnotherProjectImportFile = new HostDocument(Path.Combine(baseDirectory, "AnotherProject", "_Imports.cshtml"), "_Imports.cshtml", FileKinds.Legacy);
            AnotherProjectNestedFile3 = new HostDocument(Path.Combine(baseDirectory, "AnotherProject", "Nested", "File3.cshtml"), "Nested\\File1.cshtml", FileKinds.Legacy);
            AnotherProjectNestedFile4 = new HostDocument(Path.Combine(baseDirectory, "AnotherProject", "Nested", "File4.cshtml"), "Nested\\File2.cshtml", FileKinds.Legacy);
            AnotherProjectNestedImportFile = new HostDocument(Path.Combine(baseDirectory, "AnotherProject", "Nested", "_Imports.cshtml"), "Nested\\_Imports.cshtml", FileKinds.Legacy);
            AnotherProjectComponentFile1 = new HostDocument(Path.Combine(baseDirectory, "AnotherProject", "File1.razor"), "File1.razor", FileKinds.Component);
            AnotherProjectComponentFile2 = new HostDocument(Path.Combine(baseDirectory, "AnotherProject", "File2.razor"), "File2.razor", FileKinds.Component);
            AnotherProjectNestedComponentFile3 = new HostDocument(Path.Combine(baseDirectory, "AnotherProject", "Nested", "File3.razor"), "Nested\\File1.razor", FileKinds.Component);
            AnotherProjectNestedComponentFile4 = new HostDocument(Path.Combine(baseDirectory, "AnotherProject", "Nested", "File4.razor"), "Nested\\File2.razor", FileKinds.Component);
        }

        public static readonly HostProject SomeProject;
        public static readonly HostDocument SomeProjectFile1;
        public static readonly HostDocument SomeProjectFile2;
        public static readonly HostDocument SomeProjectImportFile;
        public static readonly HostDocument SomeProjectNestedFile3;
        public static readonly HostDocument SomeProjectNestedFile4;
        public static readonly HostDocument SomeProjectNestedImportFile;
        public static readonly HostDocument SomeProjectComponentFile1;
        public static readonly HostDocument SomeProjectComponentFile2;
        public static readonly HostDocument SomeProjectNestedComponentFile3;
        public static readonly HostDocument SomeProjectNestedComponentFile4;

        public static readonly HostProject AnotherProject;
        public static readonly HostDocument AnotherProjectFile1;
        public static readonly HostDocument AnotherProjectFile2;
        public static readonly HostDocument AnotherProjectImportFile;
        public static readonly HostDocument AnotherProjectNestedFile3;
        public static readonly HostDocument AnotherProjectNestedFile4;
        public static readonly HostDocument AnotherProjectNestedImportFile;
        public static readonly HostDocument AnotherProjectComponentFile1;
        public static readonly HostDocument AnotherProjectComponentFile2;
        public static readonly HostDocument AnotherProjectNestedComponentFile3;
        public static readonly HostDocument AnotherProjectNestedComponentFile4;
    }
}
