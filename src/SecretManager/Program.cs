// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.ConfigurationModel.UserSecrets;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Runtime.Common.CommandLine;
using Newtonsoft.Json.Linq;

namespace SecretManager
{
    public class Program
    {
        private ILogger _logger;
        private CommandOutputProvider _loggerProvider;

        public Program()
        {
            var loggerFactory = new LoggerFactory();
            CommandOutputProvider = new CommandOutputProvider();
            loggerFactory.AddProvider(CommandOutputProvider);
            Logger = loggerFactory.CreateLogger<Program>();
        }

        public ILogger Logger
        {
            get { return _logger; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _logger = value;
            }
        }

        public CommandOutputProvider CommandOutputProvider
        {
            get { return _loggerProvider; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _loggerProvider = value;
            }
        }

        public int Main(string[] args)
        {
            try
            {
                var app = new CommandLineApplication();
                app.Name = "user-secret";
                app.Description = "Manages user secrets";

                app.HelpOption("-?|-h|--help");
                var optVerbose = app.Option("-v|--verbose", "Verbose output", CommandOptionType.NoValue);

                app.Command("set", c =>
                {
                    c.Description = "Sets the user secret to the specified value";

                    var optionProject = c.Option("-p|--project <PATH>", "Path to project, default is current directory", CommandOptionType.SingleValue);
                    var keyArg = c.Argument("[name]", "Name of the secret");
                    var valueArg = c.Argument("[value]", "Value of the secret");
                    c.HelpOption("-?|-h|--help");

                    c.OnExecute(() =>
                    {
                        var projectPath = optionProject.Value() ?? Directory.GetCurrentDirectory();

                        if (optVerbose.HasValue())
                        {
                            CommandOutputProvider.LogLevel = LogLevel.Verbose;
                        }

                        ProcessSecretFile(projectPath, secrets =>
                        {
                            secrets[keyArg.Value] = valueArg.Value;
                        });

                        Logger.LogInformation(Resources.Message_Saved_Secret, keyArg.Value, valueArg.Value);
                        return 0;
                    });
                });

                app.Command("remove", c =>
                {
                    c.Description = "Removes the specified user secret";

                    var optionProject = c.Option("-p|--project <PATH>", "Path to project, default is current directory", CommandOptionType.SingleValue);
                    var keyArg = c.Argument("[name]", "Name of the secret");
                    c.HelpOption("-?|-h|--help");

                    c.OnExecute(() =>
                    {
                        var projectPath = optionProject.Value() ?? Directory.GetCurrentDirectory();

                        if (optVerbose.HasValue())
                        {
                            CommandOutputProvider.LogLevel = LogLevel.Verbose;
                        }

                        ProcessSecretFile(projectPath, secrets =>
                        {
                            if (secrets[keyArg.Value] == null)
                            {
                                Logger.LogWarning(Resources.Error_Missing_Secret, keyArg.Value);
                            }
                            else
                            {
                                secrets.Remove(keyArg.Value);
                            }
                        });

                        return 0;
                    });
                });

                app.Command("list", c =>
                {
                    c.Description = "Lists all the application secrets";

                    var optionProject = c.Option("-p|--project <PATH>", "Path to project, default is current directory", CommandOptionType.SingleValue);
                    c.HelpOption("-?|-h|--help");

                    c.OnExecute(() =>
                    {
                        var projectPath = optionProject.Value() ?? Directory.GetCurrentDirectory();

                        if (optVerbose.HasValue())
                        {
                            CommandOutputProvider.LogLevel = LogLevel.Verbose;
                        }

                        ProcessSecretFile(projectPath, secrets =>
                        {
                            PrintAll(secrets);
                        },
                        persist: false);
                        return 0;
                    });
                });

                app.Command("clear", c =>
                {
                    c.Description = "Deletes all the application secrets";

                    var optionProject = c.Option("-p|--project <PATH>", "Path to project, default is current directory", CommandOptionType.SingleValue);
                    c.HelpOption("-?|-h|--help");

                    c.OnExecute(() =>
                    {
                        var projectPath = optionProject.Value() ?? Directory.GetCurrentDirectory();

                        if (optVerbose.HasValue())
                        {
                            CommandOutputProvider.LogLevel = LogLevel.Verbose;
                        }

                        ProcessSecretFile(projectPath, secrets =>
                        {
                            secrets.RemoveAll();
                        });

                        return 0;
                    });
                });

                // Show help information if no subcommand/option was specified.
                app.OnExecute(() =>
                {
                    app.ShowHelp();
                    return 2;
                });

                return app.Execute(args);
            }
            catch (Exception exception)
            {
                Logger.LogCritical(Resources.Error_Command_Failed, exception.Message);
                return 1;
            }
        }

        private void PrintAll(JObject secrets)
        {
            if (secrets.Count == 0)
            {
                Logger.LogInformation(Resources.Error_No_Secrets_Found);
            }
            else
            {
                foreach (var secret in secrets)
                {
                    Logger.LogInformation(Resources.Message_Secret_Value_Format, secret.Key, secret.Value);
                }
            }
        }

        private void ProcessSecretFile(string projectPath, Action<JObject> observer, bool persist = true)
        {
            Logger.LogVerbose(Resources.Message_Project_File_Path, projectPath);
            var secretsFilePath = PathHelper.GetSecretsPath(projectPath);
            Logger.LogVerbose(Resources.Message_Secret_File_Path, secretsFilePath);
            var secretObj = File.Exists(secretsFilePath) ?
                            JObject.Parse(File.ReadAllText(secretsFilePath)) :
                            new JObject();

            observer(secretObj);

            if (persist)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(secretsFilePath));
                File.WriteAllText(secretsFilePath, secretObj.ToString());
            }
        }
    }
}