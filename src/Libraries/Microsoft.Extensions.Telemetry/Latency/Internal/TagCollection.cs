// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Latency;

namespace Microsoft.Extensions.Telemetry.Latency.Internal;

/// <summary>
/// Class used to add tags.
/// </summary>
internal sealed class TagCollection : IResettable
{
    private readonly Registry _tagNames;

    private readonly int _numTags;

    private readonly Tag[] _tags;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagCollection"/> class.
    /// </summary>
    /// <param name="registry">Registry of tag names.</param>
    public TagCollection(Registry registry)
    {
        _tagNames = registry;
        var keyCount = registry.KeyCount;
        _numTags = keyCount;
        _tags = new Tag[_numTags];
        _ = TryReset();
    }

    /// <summary>
    /// Resets <see cref="TagCollection"/>.
    /// </summary>
    public bool TryReset()
    {
        var names = _tagNames.OrderedKeys;
        for (int i = 0; i < _numTags; i++)
        {
            _tags[i] = new Tag(names[i], string.Empty);
        }

        return true;
    }

    public TagToken GetToken(string name)
    {
        int pos = _tagNames.GetRegisteredKeyIndex(name);
        return new TagToken(name, pos);
    }

    /// <summary>
    /// Set value of the tag.
    /// </summary>
    /// <param name="token">Token for the tag.</param>
    /// <param name="value">Value of the tag.</param>
    public void Set(TagToken token, string value)
    {
        int pos = token.Position;
        if (pos > -1)
        {
            _tags[pos] = new Tag(token.Name, value);
        }
    }

    /// <summary>
    /// Gets the list of tags that have been added.
    /// </summary>
    public ArraySegment<Tag> Tags => new(_tags, 0, _numTags);
}
