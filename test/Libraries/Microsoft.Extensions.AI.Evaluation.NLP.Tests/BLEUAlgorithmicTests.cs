// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.AI.Evaluation.NLP.Common;
using Xunit;
using static Microsoft.Extensions.AI.Evaluation.NLP.Common.BLEUAlgorithm;

namespace Microsoft.Extensions.AI.Evaluation.NLP.Tests;

[Experimental("AIEVAL001")]
public class BLEUAlgorithmicTests
{
    [Fact]
    public void NGramGenerationNoPadding()
    {
        int[] input = [1, 2, 3, 4, 5];

        IEnumerable<NGram<int>> result = input.CreateNGrams(1);
        List<NGram<int>> expected = [[1], [2], [3], [4], [5]];
        Assert.True(result.SequenceEqual(expected));

        result = input.CreateNGrams(2);
        expected = [[1, 2], [2, 3], [3, 4], [4, 5]];
        Assert.True(result.SequenceEqual(expected));

        result = input.CreateNGrams(3);
        expected = [[1, 2, 3], [2, 3, 4], [3, 4, 5]];
        Assert.True(result.SequenceEqual(expected));
    }

    [Fact]
    public void ModifiedPrecisionTests()
    {
        IEnumerable<IEnumerable<string>> references = ["the cat is on the mat".Split(' '), "there is a cat on the mat".Split(' ')];
        IEnumerable<string> hypothesis = "the the the the the the the".Split(' ');
        RationalNumber prec = ModifiedPrecision(references, hypothesis, 1);
        Assert.Equal(0.2857, prec.ToDouble(), 4);

        references = [
            "It is a guide to action that ensures that the military will forever heed Party commands".Split(' '),
            "It is the guiding principle which guarantees the military forces always being under the command of the Party".Split(' '),
            "It is the practical guide for the army always to heed the directions of the party".Split(' '),
        ];
        hypothesis = "of the".Split(' ');
        prec = ModifiedPrecision(references, hypothesis, 1);
        Assert.Equal(1.0, prec.ToDouble(), 4);
        prec = ModifiedPrecision(references, hypothesis, 2);
        Assert.Equal(1.0, prec.ToDouble(), 4);

        references = [
            "It is a guide to action that ensures that the military will forever heed Party commands".Split(' '),
            "It is the guiding principle which guarantees the military forces always being under the command of the Party".Split(' '),
            "It is the practical guide for the army always to heed the directions of the party".Split(' '),
        ];
        IEnumerable<string> hypothesis1 = "It is a guide to action which ensures that the military always obeys the commands of the party".Split(' ');
        IEnumerable<string> hypothesis2 = "It is to insure the troops forever hearing the activity guidebook that party direct".Split(' ');
        prec = ModifiedPrecision(references, hypothesis1, 1);
        Assert.Equal(0.9444, prec.ToDouble(), 4);
        prec = ModifiedPrecision(references, hypothesis2, 1);
        Assert.Equal(0.5714, prec.ToDouble(), 4);
        prec = ModifiedPrecision(references, hypothesis1, 2);
        Assert.Equal(0.5882, prec.ToDouble(), 4);
        prec = ModifiedPrecision(references, hypothesis2, 2);
        Assert.Equal(0.07692, prec.ToDouble(), 4);
    }

    [Theory]
    [InlineData(new int[] { 0, 1, 0, 2 }, 10, new[] { 0.2303, 0.0576 })]
    [InlineData(new int[] { 4, 5, 2, 4 }, 10, new[] { 0.8000, 0.5 })]
    [InlineData(new int[] { 10, 14, 7, 13, 5, 12, 4, 11 }, 20, new[] { 0.7143, 0.5385, 0.4167, 0.3636 })]
    [InlineData(new int[] { 10, 14, 7, 13, 0, 12, 0, 11 }, 20, new[] { 0.7143, 0.5385, 0.02496, 0.01362 })]
    public void SmoothingMethod4Tests(int[] num_denom, int hypLen, double[] vals)
    {
        Assert.Equal(num_denom.Length, vals.Length * 2);

        RationalNumber[] prec = new RationalNumber[vals.Length];
        for (int i = 0; i < num_denom.Length - 1; i += 2)
        {
            prec[i / 2] = new RationalNumber(num_denom[i], num_denom[i + 1]);
        }

        double[] smoothed = SmoothingFunction.Method4(prec, hypLen);

        Assert.Equal(vals.Length, smoothed.Length);

        for (int i = 0; i < vals.Length; i++)
        {
            Assert.Equal(vals[i], smoothed[i], 4);
        }
    }

    [Fact]
    public void TestBrevityPenalty()
    {
        IEnumerable<IEnumerable<string>> references = [
            Enumerable.Repeat("a", 11),
            Enumerable.Repeat("a", 8),
        ];
        IEnumerable<string> hypothesis = Enumerable.Repeat("a", 7);
        int hypLength = hypothesis.Count();
        int closestRefLength = ClosestRefLength(references, hypLength);
        double brevityPenalty = BrevityPenalty(closestRefLength, hypLength);
        Assert.Equal(0.8669, brevityPenalty, 4);

        references = [
            Enumerable.Repeat("a", 11),
            Enumerable.Repeat("a", 8),
            Enumerable.Repeat("a", 6),
            Enumerable.Repeat("a", 7),
        ];
        hypothesis = Enumerable.Repeat("a", 7);
        hypLength = hypothesis.Count();
        closestRefLength = ClosestRefLength(references, hypLength);
        brevityPenalty = BrevityPenalty(closestRefLength, hypLength);
        Assert.Equal(1.0, brevityPenalty, 4);

        references = [
            Enumerable.Repeat("a", 28),
            Enumerable.Repeat("a", 28),
        ];
        hypothesis = Enumerable.Repeat("a", 12);
        hypLength = hypothesis.Count();
        closestRefLength = ClosestRefLength(references, hypLength);
        brevityPenalty = BrevityPenalty(closestRefLength, hypLength);
        Assert.Equal(0.26359, brevityPenalty, 4);

        references = [
            Enumerable.Repeat("a", 13),
            Enumerable.Repeat("a", 2),
        ];
        hypothesis = Enumerable.Repeat("a", 12);
        hypLength = hypothesis.Count();
        closestRefLength = ClosestRefLength(references, hypLength);
        brevityPenalty = BrevityPenalty(closestRefLength, hypLength);
        Assert.Equal(0.9200, brevityPenalty, 4);

        references = [
            Enumerable.Repeat("a", 13),
            Enumerable.Repeat("a", 11),
        ];
        hypothesis = Enumerable.Repeat("a", 12);
        hypLength = hypothesis.Count();
        closestRefLength = ClosestRefLength(references, hypLength);
        brevityPenalty = BrevityPenalty(closestRefLength, hypLength);
        Assert.Equal(1.0, brevityPenalty, 4);

        references = [
            Enumerable.Repeat("a", 11),
            Enumerable.Repeat("a", 13),
        ];
        hypothesis = Enumerable.Repeat("a", 12);
        hypLength = hypothesis.Count();
        closestRefLength = ClosestRefLength(references, hypLength);
        brevityPenalty = BrevityPenalty(closestRefLength, hypLength);
        Assert.Equal(1.0, brevityPenalty, 4);

    }

    [Fact]
    public void TestZeroMatches()
    {
        IEnumerable<IEnumerable<string>> references = ["The candidate has no alignment to any of the references".Split(' '),];
        IEnumerable<string> hypothesis = "John loves Mary".Split(' ');

        double score = SentenceBLEU(references, hypothesis, EqualWeights(hypothesis.Count()));
        Assert.Equal(0.0, score, 4);
    }

    [Fact]
    public void TestFullMatches()
    {
        IEnumerable<IEnumerable<string>> references = ["John loves Mary".Split(' '),];
        IEnumerable<string> hypothesis = "John loves Mary".Split(' ');

        double score = SentenceBLEU(references, hypothesis, EqualWeights(hypothesis.Count()));
        Assert.Equal(1.0, score, 4);
    }

    [Fact]
    public void TestPartialMatchesHypothesisLongerThanReference()
    {
        IEnumerable<IEnumerable<string>> references = ["John loves Mary".Split(' '),];
        IEnumerable<string> hypothesis = "John loves Mary who loves Mike".Split(' ');

        double score = SentenceBLEU(references, hypothesis);
        Assert.Equal(0, score, 4);
    }

    [Fact]
    public void TestSentenceBLEUExampleA()
    {
        IEnumerable<IEnumerable<string>> references = [
            "It is a guide to action that ensures that the military will forever heed Party commands".Split(' '),
            "It is the guiding principle which guarantees the military forces always being under the command of the Party".Split(' '),
            "It is the practical guide for the army always to heed the directions of the party".Split(' ')
        ];
        IEnumerable<string> hypothesis = "It is a guide to action which ensures that the military always obeys the commands of the party".Split(' ');

        double score = SentenceBLEU(references, hypothesis);
        Assert.Equal(0.5046, score, 4);

    }

    [Fact]
    public void TestSentenceBLEUExampleB()
    {
        IEnumerable<IEnumerable<string>> references = [
            "he was interested in world history because he read the book".Split(' '),
        ];
        IEnumerable<string> hypothesis = "he read the book because he was interested in world history".Split(' ');

        double score = SentenceBLEU(references, hypothesis);
        Assert.Equal(0.74009, score, 4);
    }

    [Fact]
    public void TestSentenceBLEUExampleAWithWordTokenizer()
    {
        IEnumerable<IEnumerable<string>> references = [
            SimpleWordTokenizer.WordTokenize("It is a guide to action that ensures that the military will forever heed Party commands"),
            SimpleWordTokenizer.WordTokenize("It is the guiding principle which guarantees the military forces always being under the command of the Party"),
            SimpleWordTokenizer.WordTokenize("It is the practical guide for the army always to heed the directions of the party")
        ];
        IEnumerable<string> hypothesis = SimpleWordTokenizer.WordTokenize("It is a guide to action which ensures that the military always obeys the commands of the party");

        double score = SentenceBLEU(references, hypothesis);
        Assert.Equal(0.5046, score, 4);

    }

    [Fact]
    public void TestSentenceBLEUExampleBWithWordTokenizer()
    {
        IEnumerable<IEnumerable<string>> references = [
            SimpleWordTokenizer.WordTokenize("he was interested in world history because he read the book"),
        ];
        IEnumerable<string> hypothesis = SimpleWordTokenizer.WordTokenize("he read the book because he was interested in world history");

        double score = SentenceBLEU(references, hypothesis);
        Assert.Equal(0.74009, score, 4);
    }
}
