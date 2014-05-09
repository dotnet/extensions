// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit.Abstractions;

namespace Xunit
{
    public class XmlTestExecutionVisitor : TestMessageVisitor<ITestAssemblyFinished>
    {
        public XmlTestExecutionVisitor(Func<bool> cancelThunk)
        {
            CancelThunk = cancelThunk ?? (() => false);
        }

        public readonly Func<bool> CancelThunk;
        public int Failed;
        public int Skipped;
        public decimal Time;
        public int Total;

        public override bool OnMessage(IMessageSinkMessage message)
        {
            var result = base.OnMessage(message);
            if (result)
                result = !CancelThunk();

            return result;
        }

        protected override bool Visit(ITestAssemblyFinished assemblyFinished)
        {
            Total += assemblyFinished.TestsRun;
            Failed += assemblyFinished.TestsFailed;
            Skipped += assemblyFinished.TestsSkipped;
            Time += assemblyFinished.ExecutionTime;

            return base.Visit(assemblyFinished);
        }

        protected static string Escape(string value)
        {
            if (value == null)
                return String.Empty;

            return value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\0", "\\0");
        }

        protected static string XmlEscape(string value)
        {
            if (value == null)
                return String.Empty;

            return value.Replace("\0", "\\0");
        }
    }
}