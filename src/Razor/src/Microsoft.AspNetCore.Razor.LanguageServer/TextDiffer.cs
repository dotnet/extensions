// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    //
    // This class implements the linear space variation of the difference algorithm described in
    // "An O(ND) Difference Algorithm and its Variations" by Eugene W. Myers
    //
    // Note: Some variable names in this class are not be fully compliant with the C# naming guidelines
    // in the interest of using the same terminology discussed in the paper for ease of understanding.
    // 
    internal abstract class TextDiffer
    {
        protected abstract int OldTextLength { get; }

        protected abstract int NewTextLength { get; }

        protected abstract bool ContentEquals(int oldTextIndex, int newTextIndex);

        protected IReadOnlyList<DiffEdit> ComputeDiff()
        {
            var edits = new List<DiffEdit>();

            // Initialize the vectors to use for forward and reverse searches.
            var MAX = NewTextLength + OldTextLength;
            var Vf = new int[(2 * MAX) + 1];
            var Vr = new int[(2 * MAX) + 1];

            ComputeDiffRecursive(edits, 0, OldTextLength, 0, NewTextLength, Vf, Vr);

            return edits;
        }

        private void ComputeDiffRecursive(List<DiffEdit> edits, int lowA, int highA, int lowB, int highB, int[] Vf, int[] Vr)
        {
            while (lowA < highA && lowB < highB && ContentEquals(lowA, lowB))
            {
                // Skip equal text at the start.
                lowA++;
                lowB++;
            }

            while (lowA < highA && lowB < highB && ContentEquals(highA - 1, highB - 1))
            {
                // Skip equal text at the end.
                highA--;
                highB--;
            }

            if (lowA == highA)
            {
                // Base case 1: We've reached the end of original text. Insert whatever is remaining in the new text.
                while (lowB < highB)
                {
                    edits.Add(DiffEdit.Insert(lowA, lowB));
                    lowB++;
                }
            }
            else if (lowB == highB)
            {
                // Base case 2: We've reached the end of new text. Delete whatever is remaining in the original text.
                while (lowA < highA)
                {
                    edits.Add(DiffEdit.Delete(lowA));
                    lowA++;
                }
            }
            else
            {
                // Find the midpoint of the optimal path.
                var (middleX, middleY) = FindMiddleSnake(lowA, highA, lowB, highB, Vf, Vr);

                // Recursively find the midpoint of the left half.
                ComputeDiffRecursive(edits, lowA, middleX, lowB, middleY, Vf, Vr);

                // Recursively find the midpoint of the right half.
                ComputeDiffRecursive(edits, middleX, highA, middleY, highB, Vf, Vr);
            }
        }

        private (int, int) FindMiddleSnake(int lowA, int highA, int lowB, int highB, int[] Vf, int[] Vr)
        {
            var N = highA - lowA;
            var M = highB - lowB;
            var delta = N - M;
            var deltaIsEven = delta % 2 == 0;

            var MAX = N + M;

            // Compute the k-line to start the forward and reverse searches.
            var forwardK = lowA - lowB;
            var reverseK = highA - highB;

            // The paper uses negative indexes but we can't do that here. So we'll add an offset.
            var forwardOffset = MAX - forwardK;
            var reverseOffset = MAX - reverseK;

            // Initialize the vector
            Vf[forwardOffset + forwardK + 1] = lowA;
            Vr[reverseOffset + reverseK - 1] = highA;

            var maxD = Math.Ceiling((double)(M + N) / 2);
            for (var D = 0; D <= maxD; D++) // For D ← 0 to ceil((M + N)/2) Do
            {
                // Run the algorithm in forward direction.
                for (var k = forwardK - D; k <= forwardK + D; k += 2) // For k ← −D to D in steps of 2 Do
                {
                    // Find the end of the furthest reaching forward D-path in diagonal k.
                    int x;
                    if (k == forwardK - D ||
                        (k != forwardK + D && Vf[forwardOffset + k - 1] < Vf[forwardOffset + k + 1]))
                    {
                        // Down
                        x = Vf[forwardOffset + k + 1];
                    }
                    else
                    {
                        // Right
                        x = Vf[forwardOffset + k - 1] + 1;
                    }

                    var y = x - k;

                    // Traverse diagonal if possible.
                    while (x < highA && y < highB && ContentEquals(x, y))
                    {
                        x++;
                        y++;
                    }

                    Vf[forwardOffset + k] = x;
                    if (deltaIsEven)
                    {
                        // Can't have overlap here.
                    }
                    else if (k > reverseK - D && k < reverseK + D) // If ∆ is odd and k ∈ [∆ − (D − 1) , ∆ + (D − 1)] Then
                    {
                        if (Vr[reverseOffset + k] <= Vf[forwardOffset + k]) // If the path overlaps the furthest reaching reverse (D − 1)-path in diagonal k Then
                        {
                            // The last snake of the forward path is the middle snake.
                            x = Vf[forwardOffset + k];
                            y = x - k;
                            return (x, y);
                        }
                    }
                }

                // Run the algorithm in reverse direction.
                for (var k = reverseK - D; k <= reverseK + D; k += 2) // For k ← −D to D in steps of 2 Do
                {
                    // Find the end of the furthest reaching reverse D-path in diagonal k+∆.
                    int x;
                    if (k == reverseK + D ||
                        (k != reverseK - D && Vr[reverseOffset + k - 1] < Vr[reverseOffset + k + 1] - 1))
                    {
                        // Up
                        x = Vr[reverseOffset + k - 1];
                    }
                    else
                    {
                        // Left
                        x = Vr[reverseOffset + k + 1] - 1;
                    }

                    var y = x - k;

                    // Traverse diagonal if possible.
                    while (x > lowA && y > lowB && ContentEquals(x - 1, y - 1))
                    {
                        x--;
                        y--;
                    }

                    Vr[reverseOffset + k] = x;
                    if (!deltaIsEven)
                    {
                        // Can't have overlap here.
                    }
                    else if (k >= forwardK - D && k <= forwardK + D) // If ∆ is even and k + ∆ ∈ [−D, D] Then
                    {
                        if (Vr[reverseOffset + k] <= Vf[forwardOffset + k]) // If the path overlaps the furthest reaching forward D-path in diagonal k+∆ Then
                        {
                            // The last snake of the reverse path is the middle snake.
                            x = Vf[forwardOffset + k];
                            y = x - k;
                            return (x, y);
                        }
                    }
                }
            }

            throw new InvalidOperationException("Shouldn't reach here.");
        }
    }
}
