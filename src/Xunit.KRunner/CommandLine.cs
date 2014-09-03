// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Xunit.ConsoleClient
{
    public class CommandLine
    {
        readonly Stack<string> arguments = new Stack<string>();

        protected CommandLine(string[] args)
        {
            for (int i = args.Length - 1; i >= 0; i--)
                arguments.Push(args[i]);

            TeamCity = Environment.GetEnvironmentVariable("TEAMCITY_PROJECT_NAME") != null;
            ParallelizeTestCollections = true;
            Tests = new List<string>();
            Parse();
        }

        public bool DesignTime { get; set; }

        public bool List { get; set; }

        public int MaxParallelThreads { get; set; }

        public bool ParallelizeTestCollections { get; set; }

        public bool TeamCity { get; protected set; }

        public List<string> Tests { get; private set; }

        static void GuardNoOptionValue(KeyValuePair<string, string> option)
        {
            if (option.Value != null)
                throw new ArgumentException(String.Format("error: unknown command line option: {0}", option.Value));
        }

        public static CommandLine Parse(params string[] args)
        {
            return new CommandLine(args);
        }

        protected virtual void Parse()
        {
            while (arguments.Count > 0)
            {
                var option = PopOption(arguments);
                var optionName = option.Key.ToLowerInvariant();

                if (!optionName.StartsWith("-"))
                    throw new ArgumentException(String.Format("unknown command line option: {0}", option.Key));

                if (optionName == "-maxthreads")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -maxthreads");

                    int threadValue;
                    if (!Int32.TryParse(option.Value, out threadValue) || threadValue < 0)
                        throw new ArgumentException("incorrect argument value for -maxthreads");

                    MaxParallelThreads = threadValue;
                }
                else if (optionName == "-parallel")
                {
                    if (option.Value == null)
                        throw new ArgumentException("missing argument for -parallel");

                    ParallelismOption parallelismOption;
                    if (!Enum.TryParse<ParallelismOption>(option.Value, out parallelismOption))
                        throw new ArgumentException("incorrect argument value for -parallel");

                    switch (parallelismOption)
                    {
                        case ParallelismOption.all:
                        case ParallelismOption.collections:
                            ParallelizeTestCollections = true;
                            break;

                        case ParallelismOption.none:
                        default:
                            ParallelizeTestCollections = false;
                            break;
                    }
                }
                else if (optionName == "--test")
                {
                    if (option.Value == null)
                    {
                        throw new ArgumentException("missing argument for --test");
                    }

                    Tests.AddRange(option.Value.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries));
                }
                else if (optionName == "-teamcity")
                {
                    GuardNoOptionValue(option);
                    TeamCity = true;
                }
                else if (optionName == "--list")
                {
                    GuardNoOptionValue(option);
                    List = true;
                }
                else if (optionName == "--designtime")
                {
                    GuardNoOptionValue(option);
                    DesignTime = true;
                }
            }
        }

        static KeyValuePair<string, string> PopOption(Stack<string> arguments)
        {
            string option = arguments.Pop();
            string value = null;

            if (arguments.Count > 0 && !arguments.Peek().StartsWith("-"))
                value = arguments.Pop();

            return new KeyValuePair<string, string>(option, value);
        }
    }
}
