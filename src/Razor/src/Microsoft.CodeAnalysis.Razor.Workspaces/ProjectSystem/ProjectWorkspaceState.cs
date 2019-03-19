// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public sealed class ProjectWorkspaceState : IEquatable<ProjectWorkspaceState>
    {
        public static readonly ProjectWorkspaceState Default = new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), LanguageVersion.Default);

        public ProjectWorkspaceState(
            IReadOnlyList<TagHelperDescriptor> tagHelpers,
            LanguageVersion csharpLanguageVersion)
        {
            if (tagHelpers == null)
            {
                throw new ArgumentNullException(nameof(tagHelpers));
            }

            TagHelpers = tagHelpers;
            CSharpLanguageVersion = csharpLanguageVersion;
        }

        public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; }

        public LanguageVersion CSharpLanguageVersion { get; }

        public override bool Equals(object obj)
        {
            return base.Equals(obj as ProjectWorkspaceState);
        }

        public bool Equals(ProjectWorkspaceState other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            if (!Enumerable.SequenceEqual(TagHelpers, other.TagHelpers))
            {
                return false;
            }

            if (CSharpLanguageVersion != other.CSharpLanguageVersion)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hash = new HashCodeCombiner();
            for (var i = 0; i < TagHelpers.Count; i++)
            {
                hash.Add(TagHelpers[i].GetHashCode());
            }

            hash.Add(CSharpLanguageVersion);

            return hash.CombinedHash;
        }
    }
}