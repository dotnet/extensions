// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.AI.Evaluation.NLP.Common;
using Xunit;
using static Microsoft.Extensions.AI.Evaluation.NLP.Common.GLEUAlgorithm;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Tests;

public class GLEUAlgorithmTests
{
    [Fact]
    public void TestZeroMatches()
    {
        IEnumerable<IEnumerable<string>> references = ["The candidate has no alignment to any of the references".Split(' '),];
        IEnumerable<string> hypothesis = "John loves Mary".Split(' ');

        double score = SentenceGLEU(references, hypothesis);
        Assert.Equal(0.0, score, 4);
    }

    [Fact]
    public void TestFullMatches()
    {
        IEnumerable<IEnumerable<string>> references = ["John loves Mary".Split(' '),];
        IEnumerable<string> hypothesis = "John loves Mary".Split(' ');

        double score = SentenceGLEU(references, hypothesis);
        Assert.Equal(1.0, score, 4);
    }

    [Fact]
    public void TestSentenceGLEUExampleA()
    {
        IEnumerable<IEnumerable<string>> references = [
            "It is a guide to action that ensures that the military will forever heed Party commands".Split(' '),
            "It is the guiding principle which guarantees the military forces always being under the command of the Party".Split(' '),
            "It is the practical guide for the army always to heed the directions of the party".Split(' ')
        ];
        IEnumerable<string> hypothesis = "It is a guide to action which ensures that the military always obeys the commands of the party".Split(' ');

        double score = SentenceGLEU(references, hypothesis);
        Assert.Equal(0.2778, score, 4);
    }

    [Fact]
    public void TestSentenceGLEUMilitaryExampleA()
    {
        IEnumerable<IEnumerable<string>> references = [
            "It is a guide to action that ensures that the military will forever heed Party commands".Split(' '),
        ];
        IEnumerable<string> hypothesis = "It is a guide to action which ensures that the military always obeys the commands of the party".Split(' ');

        double score = SentenceGLEU(references, hypothesis);
        Assert.Equal(0.43939, score, 4);
    }

    [Fact]
    public void TestSentenceGLEUMilitaryExampleB()
    {
        IEnumerable<IEnumerable<string>> references = [
            "It is a guide to action that ensures that the military will forever heed Party commands".Split(' '),
        ];
        IEnumerable<string> hypothesis = "It is to insure the troops forever hearing the activity guidebook that party direct".Split(' ');

        double score = SentenceGLEU(references, hypothesis);
        Assert.Equal(0.12069, score, 4);
    }

    [Fact]
    public void TestSentenceGLEUExampleB()
    {
        IEnumerable<IEnumerable<string>> references = [
            "he was interested in world history because he read the book".Split(' '),
        ];
        IEnumerable<string> hypothesis = "he read the book because he was interested in world history".Split(' ');

        double score = SentenceGLEU(references, hypothesis);
        Assert.Equal(0.7895, score, 4);
    }

    [Fact]
    public void TestSentenceGLEUExampleAWithWordTokenizer()
    {
        IEnumerable<IEnumerable<string>> references = [
            SimpleWordTokenizer.WordTokenize("It is a guide to action that ensures that the military will forever heed Party commands"),
            SimpleWordTokenizer.WordTokenize("It is the guiding principle which guarantees the military forces always being under the command of the Party"),
            SimpleWordTokenizer.WordTokenize("It is the practical guide for the army always to heed the directions of the party")
        ];
        IEnumerable<string> hypothesis = SimpleWordTokenizer.WordTokenize("It is a guide to action which ensures that the military always obeys the commands of the party");

        double score = SentenceGLEU(references, hypothesis);
        Assert.Equal(0.2980, score, 4);

    }

    [Fact]
    public void TestSentenceGLEUExampleBWithWordTokenizer()
    {
        IEnumerable<IEnumerable<string>> references = [
            SimpleWordTokenizer.WordTokenize("he was interested in world history because he read the book"),
        ];
        IEnumerable<string> hypothesis = SimpleWordTokenizer.WordTokenize("he read the book because he was interested in world history");

        double score = SentenceGLEU(references, hypothesis);
        Assert.Equal(0.7895, score, 4);
    }

    [Fact]
    public void TestSentenceGLEUCatExample()
    {
        IEnumerable<IEnumerable<string>> references = [
            "the cat is on the mat".Split(' '),
        ];
        IEnumerable<string> hypothesis = "the the the the the the the".Split(' ');
        double score = SentenceGLEU(references, hypothesis);
        Assert.Equal(0.0909, score, 4);
    }
}
