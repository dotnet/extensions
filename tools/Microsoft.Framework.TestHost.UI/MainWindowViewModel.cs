// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Framework.TestHost.Client;
using Microsoft.Win32;

namespace Microsoft.Framework.TestHost.UI
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Window _window;
        private readonly TextBox _console;
        private readonly TextBox _messages;
        private readonly TaskScheduler _scheduler;

        private bool _debug;
        private string _dnx;
        private string _dthPort;
        private bool _isRunning;
        private int _processId;
        private string _selectedProject;
        private string _status;

        private TestHostWrapper _host;

        public MainWindowViewModel(Window window)
        {
            _window = window;
            _console = (TextBox)window.FindName("_consoleBuffer");
            _messages = (TextBox)window.FindName("_messageBuffer");
            _scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            DiscoverTests = new RelayCommand(ExecuteDiscoverTests, CanExecuteDiscoverTests);
            SelectDNX = new RelayCommand(ExecuteSelectDNX, CanExecuteSelectDNX);
            SelectProject = new RelayCommand(ExecuteSelectProject, CanExecuteSelectProject);
            StartTestHost = new RelayCommand(ExecuteStartTestHost, CanExecuteStartTestHost);
            StopTestHost = new RelayCommand(ExecuteStopTestHost, CanExecuteStopTestHost);
            RunAllTests = new RelayCommand(ExecuteRunAllTests, CanExecuteRunAllTests);
            RunSelectedTests = new RelayCommand(ExecuteRunSelectedTests, CanExecuteRunSelectedTests);
        }

        public bool Debug
        {
            get
            {
                return _debug;
            }
            set
            {
                _debug = value;
                OnPropertyChanged();
            }
        }

        public string DNX
        {
            get
            {
                return _dnx;
            }
            set
            {
                _dnx = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReady));
            }
        }

        public string DTHPort
        {
            get
            {
                return _dthPort;
            }
            set
            {
                _dthPort = value;
                OnPropertyChanged();
            }
        }

        public bool IsReady
        {
            get
            {
                return !string.IsNullOrWhiteSpace(SelectedProject) && !string.IsNullOrWhiteSpace(DNX);
            }
        }

        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            set
            {
                _isRunning = value;
                OnPropertyChanged();
            }
        }

        public int ProcessId
        {
            get
            {
                return _processId;
            }
            set
            {
                _processId = value;
                OnPropertyChanged();
            }
        }

        public string SelectedProject
        {
            get
            {
                return _selectedProject;
            }
            set
            {
                _selectedProject = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReady));
            }
        }

        public string Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public ICommand SelectDNX { get; }

        private bool CanExecuteSelectDNX(object _)
        {
            return !IsRunning;
        }

        private void ExecuteSelectDNX(object _)
        {
            var dialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                DefaultExt = ".exe",
                Filter = "dnx Executable (dnx.exe)|dnx.exe",
                InitialDirectory = string.IsNullOrEmpty(DNX) ? Client.DNX.FindDnxDirectory() : Path.GetDirectoryName(DNX),
                Title = "DNX"
            };

            if (dialog.ShowDialog(_window) == true)
            {
                DNX = dialog.FileName;
            }
        }

        public ICommand SelectProject { get; }

        private bool CanExecuteSelectProject(object _)
        {
            return !IsRunning;
        }

        private void ExecuteSelectProject(object _)
        {
            var dialog = new OpenFileDialog()
            {
                CheckFileExists = true,
                DefaultExt = ".json",
                Filter = "Test Project Files (project.json)|project.json",
                InitialDirectory = string.IsNullOrEmpty(SelectedProject) ? null : Path.GetDirectoryName(SelectedProject),
                Title = "Select Test Project"
            };

            if (dialog.ShowDialog(_window) == true)
            {
                SelectedProject = dialog.FileName;
            }
        }

        public ICommand StartTestHost { get; }

        private bool CanExecuteStartTestHost(object _)
        {
            return !IsRunning && IsReady;
        }

        private void ExecuteStartTestHost(object _)
        {
            _console.Text = string.Empty;
            _messages.Text = string.Empty;

            var host = new TestHostWrapper(SelectedProject, DNX, Debug);

            int dthPort;
            if (!string.IsNullOrEmpty(DTHPort) && int.TryParse(DTHPort, out dthPort))
            {
                host.DTHPort = dthPort;
            }

            host.ConsoleOutputReceived += TestHost_ConsoleOutputReceived;
            host.MessageReceived += TestHost_MessageReceived;

            try
            {
                IsRunning = true;

                _host = host;
                Status = "Starting TestHost...";

                var timer = Stopwatch.StartNew();
                var task = host.StartAsync();

                task.ContinueWith((t) =>
                {
                    if (t.IsFaulted)
                    {
                        Status = "Starting TestHost failed.";
                        ShowErrorDialog(t.Exception);
                        Reset();
                    }
                    else
                    {
                        Status = string.Format("Started: pid {0} in {1}ms.", _host.Process.Id, timer.ElapsedMilliseconds);
                        ProcessId = _host.Process.Id;
                    }

                }, _scheduler);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
                Reset();
            }
        }

        public ICommand StopTestHost { get; }

        private bool CanExecuteStopTestHost(object _)
        {
            return IsRunning && IsReady;
        }

        private void ExecuteStopTestHost(object _)
        {
            if (_host == null)
            {
                return;
            }

            try
            {
                _host.Process.Kill();
                Reset();
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
                Reset();
            }
        }

        public ICommand DiscoverTests { get; }

        private bool CanExecuteDiscoverTests(object _)
        {
            return IsRunning && IsReady && ProcessId > 0;
        }

        private void ExecuteDiscoverTests(object _)
        {
            try
            {
                Status = "Discovering Tests...";

                var timer = Stopwatch.StartNew();
                var task = _host.ListTestsAsync();

                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Status = "Discovering Tests failed.";
                        ShowErrorDialog(t.Exception);
                    }
                    else
                    {
                        Status = string.Format("Discovered Tests in {0}ms", timer.ElapsedMilliseconds);
                    }

                    Reset();
                }, _scheduler);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
                Reset();
            }
        }

        public ICommand RunAllTests { get; }

        private bool CanExecuteRunAllTests(object _)
        {
            return IsRunning && IsReady && ProcessId > 0;
        }

        private void ExecuteRunAllTests(object _)
        {
            try
            {
                Status = "Running All Tests...";

                var timer = Stopwatch.StartNew();
                var task = _host.RunTestsAsync();

                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Status = "Running All Tests failed.";
                        ShowErrorDialog(t.Exception);
                    }
                    else
                    {
                        Status = string.Format("Ran All Tests in {0}ms", timer.ElapsedMilliseconds);
                    }

                    Reset();
                }, _scheduler);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
                Reset();
            }
        }

        public ICommand RunSelectedTests { get; }

        private bool CanExecuteRunSelectedTests(object _)
        {
            return IsRunning && IsReady && ProcessId > 0;
        }

        private async void ExecuteRunSelectedTests(object _)
        {
            await Task.Delay(1);
        }

        private void TestHost_ConsoleOutputReceived(object sender, DataReceivedEventArgs e)
        {
            _console.Dispatcher.BeginInvoke(new Action<string>(_console.AppendText), e.Data + Environment.NewLine);
        }

        private void TestHost_MessageReceived(object sender, Message e)
        {
            _messages.Dispatcher.BeginInvoke(new Action<string>(_messages.AppendText), e.ToString() + Environment.NewLine);
        }

        private void ShowErrorDialog(Exception ex)
        {
            MessageBox.Show(_window, ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void Reset()
        {
            IsRunning = false;
            ProcessId = 0;

            if (_host != null)
            {
                _host.Dispose();
                _host = null;
            }
        }
    }
}
