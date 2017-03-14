// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.CommandLineUtils
{
    public class CommandLineApplication
    {
        // Indicates whether the parser should throw an exception when it runs into an unexpected argument.
        // If this field is set to false, the parser will stop parsing when it sees an unexpected argument, and all
        // remaining arguments, including the first unexpected argument, will be stored in RemainingArguments property.
        private readonly bool _throwOnUnexpectedArg;

        public CommandLineApplication(bool throwOnUnexpectedArg = true)
        {
            _throwOnUnexpectedArg = throwOnUnexpectedArg;
            Options = new List<CommandOption>();
            Arguments = new List<CommandArgument>();
            Commands = new List<CommandLineApplication>();
            RemainingArguments = new List<string>();
            Invoke = () => 0;
        }

        public CommandLineApplication Parent { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Syntax { get; set; }
        public string Description { get; set; }
        public bool ShowInHelpText { get; set; } = true;
        public string ExtendedHelpText { get; set; }
        public readonly List<CommandOption> Options;
        public CommandOption OptionHelp { get; private set; }
        public CommandOption OptionVersion { get; private set; }
        public readonly List<CommandArgument> Arguments;
        public readonly List<string> RemainingArguments;
        public bool IsShowingInformation { get; protected set; } // Is showing help or version?
        public Func<int> Invoke { get; set; }
        public Func<string> LongVersionGetter { get; set; }
        public Func<string> ShortVersionGetter { get; set; }
        public readonly List<CommandLineApplication> Commands;
        public bool AllowArgumentSeparator { get; set; }
        public TextWriter Out { get; set; } = Console.Out;
        public TextWriter Error { get; set; } = Console.Error;

        public IEnumerable<CommandOption> GetOptions()
        {
            var expr = Options.AsEnumerable();
            var rootNode = this;
            while (rootNode.Parent != null)
            {
                rootNode = rootNode.Parent;
                expr = expr.Concat(rootNode.Options.Where(o => o.Inherited));
            }

            return expr;
        }

        public CommandLineApplication Command(string name, Action<CommandLineApplication> configuration,
            bool throwOnUnexpectedArg = true)
        {
            var command = new CommandLineApplication(throwOnUnexpectedArg) {Name = name, Parent = this};
            Commands.Add(command);
            configuration(command);
            return command;
        }

        public CommandOption Option(string template, string description, CommandOptionType optionType)
            => Option(template, description, optionType, _ => { }, inherited: false);

        public CommandOption Option(string template, string description, CommandOptionType optionType, bool inherited)
            => Option(template, description, optionType, _ => { }, inherited);

        public CommandOption Option(string template, string description, CommandOptionType optionType,
            Action<CommandOption> configuration)
            => Option(template, description, optionType, configuration, inherited: false);

        public CommandOption Option(string template, string description, CommandOptionType optionType,
            Action<CommandOption> configuration, bool inherited)
        {
            var option = new CommandOption(template, optionType)
            {
                Description = description,
                Inherited = inherited
            };
            Options.Add(option);
            configuration(option);
            return option;
        }

        public CommandArgument Argument(string name, string description, bool multipleValues = false)
        {
            return Argument(name, description, _ => { }, multipleValues);
        }

        public CommandArgument Argument(string name, string description, Action<CommandArgument> configuration,
            bool multipleValues = false)
        {
            var lastArg = Arguments.LastOrDefault();
            if (lastArg != null && lastArg.MultipleValues)
            {
                var message = string.Format(
                    "The last argument '{0}' accepts multiple values. No more argument can be added.",
                    lastArg.Name);
                throw new InvalidOperationException(message);
            }

            var argument = new CommandArgument
            {
                Name = name,
                Description = description,
                MultipleValues = multipleValues
            };
            Arguments.Add(argument);
            configuration(argument);
            return argument;
        }

        public void OnExecute(Func<int> invoke)
        {
            Invoke = invoke;
        }

        public void OnExecute(Func<Task<int>> invoke)
        {
            Invoke = () => invoke().Result;
        }

        public int Execute(params string[] args)
        {
            var proc = new ArgsProcessor(this, args);
            CommandLineApplication command = proc.ProcessArgs();
            return command.Invoke();
        }

        // Helper method that adds a help option
        public CommandOption HelpOption(string template)
        {
            // Help option is special because we stop parsing once we see it
            // So we store it separately for further use
            OptionHelp = Option(template, "Show help information", CommandOptionType.NoValue);

            return OptionHelp;
        }

        public CommandOption VersionOption(string template,
            string shortFormVersion,
            string longFormVersion = null)
        {
            if (longFormVersion == null)
            {
                return VersionOption(template, () => shortFormVersion);
            }
            else
            {
                return VersionOption(template, () => shortFormVersion, () => longFormVersion);
            }
        }

        // Helper method that adds a version option
        public CommandOption VersionOption(string template,
            Func<string> shortFormVersionGetter,
            Func<string> longFormVersionGetter = null)
        {
            // Version option is special because we stop parsing once we see it
            // So we store it separately for further use
            OptionVersion = Option(template, "Show version information", CommandOptionType.NoValue);
            ShortVersionGetter = shortFormVersionGetter;
            LongVersionGetter = longFormVersionGetter ?? shortFormVersionGetter;

            return OptionVersion;
        }

        // Show short hint that reminds users to use help option
        public void ShowHint()
        {
            if (OptionHelp != null)
            {
                Out.WriteLine(string.Format("Specify --{0} for a list of available options and commands.",
                    OptionHelp.LongName));
            }
        }

        // Show full help
        public void ShowHelp(string commandName = null)
        {
            for (var cmd = this; cmd != null; cmd = cmd.Parent)
            {
                cmd.IsShowingInformation = true;
            }

            Out.WriteLine(GetHelpText(commandName));
        }

        public virtual string GetHelpText(string commandName = null)
        {
            var headerBuilder = new StringBuilder("Usage:");
            for (var cmd = this; cmd != null; cmd = cmd.Parent)
            {
                headerBuilder.Insert(6, string.Format(" {0}", cmd.Name));
            }

            CommandLineApplication target;

            if (commandName == null || string.Equals(Name, commandName, StringComparison.OrdinalIgnoreCase))
            {
                target = this;
            }
            else
            {
                target = Commands.SingleOrDefault(
                    cmd => string.Equals(cmd.Name, commandName, StringComparison.OrdinalIgnoreCase));

                if (target != null)
                {
                    headerBuilder.AppendFormat(" {0}", commandName);
                }
                else
                {
                    // The command name is invalid so don't try to show help for something that doesn't exist
                    target = this;
                }

            }

            var optionsBuilder = new StringBuilder();
            var commandsBuilder = new StringBuilder();
            var argumentsBuilder = new StringBuilder();

            var arguments = target.Arguments.Where(a => a.ShowInHelpText).ToList();
            if (arguments.Any())
            {
                headerBuilder.Append(" [arguments]");

                argumentsBuilder.AppendLine();
                argumentsBuilder.AppendLine("Arguments:");
                var maxArgLen = arguments.Max(a => a.Name.Length);
                var outputFormat = string.Format("  {{0, -{0}}}{{1}}", maxArgLen + 2);
                foreach (var arg in arguments)
                {
                    argumentsBuilder.AppendFormat(outputFormat, arg.Name, arg.Description);
                    argumentsBuilder.AppendLine();
                }
            }

            var options = target.GetOptions().Where(o => o.ShowInHelpText).ToList();
            if (options.Any())
            {
                headerBuilder.Append(" [options]");

                optionsBuilder.AppendLine();
                optionsBuilder.AppendLine("Options:");
                var maxOptLen = options.Max(o => o.Template.Length);
                var outputFormat = string.Format("  {{0, -{0}}}{{1}}", maxOptLen + 2);
                foreach (var opt in options)
                {
                    optionsBuilder.AppendFormat(outputFormat, opt.Template, opt.Description);
                    optionsBuilder.AppendLine();
                }
            }

            var commands = target.Commands.Where(c => c.ShowInHelpText).ToList();
            if (commands.Any())
            {
                headerBuilder.Append(" [command]");

                commandsBuilder.AppendLine();
                commandsBuilder.AppendLine("Commands:");
                var maxCmdLen = commands.Max(c => c.Name.Length);
                var outputFormat = string.Format("  {{0, -{0}}}{{1}}", maxCmdLen + 2);
                foreach (var cmd in commands.OrderBy(c => c.Name))
                {
                    commandsBuilder.AppendFormat(outputFormat, cmd.Name, cmd.Description);
                    commandsBuilder.AppendLine();
                }

                if (OptionHelp != null)
                {
                    commandsBuilder.AppendLine();
                    commandsBuilder.AppendFormat(
                        $"Use \"{target.Name} [command] --{OptionHelp.LongName}\" for more information about a command.");
                    commandsBuilder.AppendLine();
                }
            }

            if (target.AllowArgumentSeparator)
            {
                headerBuilder.Append(" [[--] <arg>...]");
            }

            headerBuilder.AppendLine();

            var nameAndVersion = new StringBuilder();
            nameAndVersion.AppendLine(GetFullNameAndVersion());
            nameAndVersion.AppendLine();

            return nameAndVersion.ToString()
                   + headerBuilder.ToString()
                   + argumentsBuilder.ToString()
                   + optionsBuilder.ToString()
                   + commandsBuilder.ToString()
                   + target.ExtendedHelpText;
        }

        public void ShowVersion()
        {
            for (var cmd = this; cmd != null; cmd = cmd.Parent)
            {
                cmd.IsShowingInformation = true;
            }

            Out.WriteLine(FullName);
            Out.WriteLine(LongVersionGetter());
        }

        public string GetFullNameAndVersion()
        {
            return ShortVersionGetter == null ? FullName : string.Format("{0} {1}", FullName, ShortVersionGetter());
        }

        public void ShowRootCommandFullNameAndVersion()
        {
            var rootCmd = this;
            while (rootCmd.Parent != null)
            {
                rootCmd = rootCmd.Parent;
            }

            Out.WriteLine(rootCmd.GetFullNameAndVersion());
            Out.WriteLine();
        }

        private bool TryGetCommand(string name, out CommandLineApplication command)
        {
            command = Commands
                .SingleOrDefault(
                    c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)
                );
            return command != null;
        }

        private bool TryGetShortOption(string name, out CommandOption option)
        {
            option = GetOptions()
                .SingleOrDefault(
                    opt => string.Equals(opt.ShortName, name, StringComparison.Ordinal));
            return option != null;
        }

        private bool TryGetSymbolOption(string name, out CommandOption option)
        {
            option = GetOptions()
                .SingleOrDefault(
                    opt => string.Equals(opt.SymbolName, name, StringComparison.Ordinal));
            return option != null;
        }

        private bool TryGetGetLongOption(string name, out CommandOption option)
        {
            option = GetOptions()
                .SingleOrDefault(
                    opt => string.Equals(opt.LongName, name, StringComparison.Ordinal));
            return option != null;
        }

        private class CommandArgumentEnumerator : IEnumerator<CommandArgument>
        {
            private readonly IEnumerator<CommandArgument> _enumerator;

            public CommandArgumentEnumerator(IEnumerator<CommandArgument> enumerator)
            {
                _enumerator = enumerator;
            }

            public CommandArgument Current
            {
                get { return _enumerator.Current; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            public bool MoveNext()
            {
                if (Current == null || !Current.MultipleValues)
                {
                    return _enumerator.MoveNext();
                }

                // If current argument allows multiple values, we don't move forward and
                // all later values will be added to current CommandArgument.Values
                return true;
            }

            public void Reset()
            {
                _enumerator.Reset();
            }
        }

        private class ArgsProcessor
        {
            private enum ArgType
            {
                None = 0,
                Error,
                ShortOption,
                LongOption,
                CommandOrArgument,
                ArgumentSeparator
            }

            private class RawCommandLineArg
            {
                public RawCommandLineArg(string arg)
                {
                    this.Arg = arg;
                    ArgType = GetArgType(arg);
                    if (ArgType == ArgType.LongOption || ArgType == ArgType.ShortOption)
                    {
                        string[] parts = arg.Split(new[] { ':', '=' }, 2);
                        if (parts.Length > 1)
                        {
                            ArgValue = parts[1];
                        }
                        if (ArgType == ArgType.ShortOption)
                        {
                            ArgName = parts[0].Substring(1);
                        }
                        else
                        {
                            ArgName = parts[0].Substring(2);
                        }
                    }
                }

                public string Arg { get; private set; }
                public string ArgName { get; private set; }
                public string ArgValue { get; private set; }
                public ArgType ArgType { get; private set; }

                private static ArgType GetArgType(string arg)
                {
                    if (String.IsNullOrEmpty(arg) || arg == "-")
                    {
                        // null, "" or "-" are invalid
                        return ArgType.Error;
                    }
                    if (arg[0] != '-')
                    {
                        // everything not starting with "-" is a command
                        return ArgType.CommandOrArgument;
                    }
                    if (arg[1] != '-')
                    {
                        // everything starting with "-" and continuing
                        // with something else is a short option
                        return ArgType.ShortOption;
                    }
                    if (arg.Length == 2)
                    {
                        // at this point we have something starting wit "--"
                        // if that's all it's an argument separator
                        return ArgType.ArgumentSeparator;
                    }
                    // finally it has to be a long option
                    return ArgType.LongOption;
                }
            }

            private class RawCommandLineArgEnumerator : IEnumerator<RawCommandLineArg>
            {
                private readonly IEnumerator<string> _argsEnumerator;
                private RawCommandLineArg _current;

                public RawCommandLineArgEnumerator(string[] args)
                {
                    _argsEnumerator = ((IEnumerable<string>) args).GetEnumerator();
                }

                public RawCommandLineArg Current => _current;

                object IEnumerator.Current => _current;

                public void Dispose()
                {
                    _current = null;
                    _argsEnumerator.Dispose();
                }

                public bool MoveNext()
                {
                    if (_argsEnumerator.MoveNext())
                    {
                        _current = new RawCommandLineArg(_argsEnumerator.Current);
                        return true;
                    }
                    return false;
                }

                public void Reset()
                {
                    _current = null;
                    _argsEnumerator.Reset();
                }
            }

            private readonly CommandLineApplication _initialCommand;
            private readonly string[] _args;
            private RawCommandLineArgEnumerator _argsEnumerator;
            private CommandLineApplication _currentCommand;
            private CommandArgumentEnumerator _currentCommandArguments;

            public ArgsProcessor(CommandLineApplication command, string[] args)
            {
                this._initialCommand = command;
                this._args = args;
            }

            public CommandLineApplication ProcessArgs()
            {
                using (_argsEnumerator = new RawCommandLineArgEnumerator(_args))
                {
                    _currentCommand = _initialCommand;
                    _currentCommandArguments = null;
                    while (_argsEnumerator.MoveNext())
                    {
                        if (!ProcessCurrentArg())
                        {
                            return _currentCommand;
                        }
                    }
                    return _currentCommand;
                }
            }

            private bool ProcessCurrentArg()
            {
                switch (_argsEnumerator.Current.ArgType)
                {
                    case ArgType.ArgumentSeparator:
                        if (_argsEnumerator.MoveNext())
                        {
                            HandleUnexpectedArg("option");
                        }
                        return false;
                    case ArgType.ShortOption:
                    case ArgType.LongOption:
                        if (!ProcessOption())
                        {
                            return false;
                        }
                        break;
                    case ArgType.CommandOrArgument:
                        if (!ProcessCommandOrArgument())
                        {
                            return false;
                        }
                        break;
                    default:
                        return false;
                }
                return true;
            }

            private bool ProcessOption()
            {
                if (!TryGetOption(out CommandOption option))
                {
                    HandleUnexpectedArg("option");
                    return false;
                }

                if (option.OptionType == CommandOptionType.NoValue)
                {
                    option.TryParse(_argsEnumerator.Current.ArgValue);
                    if (option == _currentCommand.OptionHelp ||
                        option == _currentCommand.OptionVersion)
                    {
                        return false;
                    }
                }

                if (_argsEnumerator.Current.ArgValue != null)
                {
                    return option.TryParse(_argsEnumerator.Current.ArgValue);
                }

                if (TryReadValueFromNextArg(out string value))
                {
                    return option.TryParse(value);
                }
                _currentCommand.ShowHint();
                throw new CommandParsingException(_currentCommand, $"Missing value for option '{option.LongName}'");
            }

            private bool ProcessCommandOrArgument()
            {
                if (_currentCommand.TryGetCommand(_argsEnumerator.Current.Arg, out var cmd))
                {
                    _currentCommand = cmd;
                    _currentCommandArguments?.Dispose();
                    _currentCommandArguments = null;
                    return true;
                }
                if (_currentCommandArguments == null)
                {
                    _currentCommandArguments = new CommandArgumentEnumerator(_currentCommand.Arguments.GetEnumerator());
                }
                if (_currentCommandArguments.MoveNext())
                {
                    _currentCommandArguments.Current.Values.Add(_argsEnumerator.Current.Arg);
                    return true;
                }
                HandleUnexpectedArg("command or argument");
                return false;
            }

            private bool TryGetOption(out CommandOption option)
            {
                if (_argsEnumerator.Current.ArgType == ArgType.ShortOption)
                {
                    if (!_currentCommand.TryGetShortOption(_argsEnumerator.Current.ArgName, out option))
                    {
                        return _currentCommand.TryGetSymbolOption(_argsEnumerator.Current.ArgName, out option);
                    }
                    return true;
                }
                return _currentCommand.TryGetGetLongOption(_argsEnumerator.Current.ArgName, out option);
            }

            private bool TryReadValueFromNextArg(out string value)
            {
                if (_argsEnumerator.MoveNext())
                {
                    value = _argsEnumerator.Current.Arg;
                    return true;
                }

                value = null;
                return false;
            }

            private void HandleUnexpectedArg(string argTypeName)
            {
                if (_currentCommand._throwOnUnexpectedArg)
                {
                    _currentCommand.ShowHint();
                    throw new CommandParsingException(_currentCommand,
                        $"Unrecognized {argTypeName} '{_argsEnumerator.Current.Arg}'");
                }

                AddRemainingArguments();
            }

            private void AddRemainingArguments()
            {
                _currentCommand.RemainingArguments.AddRange(GetRemainingArguments());
            }

            private IEnumerable<string> GetRemainingArguments()
            {
                do
                {
                    yield return _argsEnumerator.Current.Arg;
                } while (_argsEnumerator.MoveNext());
            }

        }
    }
}
