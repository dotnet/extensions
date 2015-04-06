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

        private string _dnx;
        private bool _isRunning;
        private int _processId;
        private string _selectedProject;

        public MainWindowViewModel(Window window)
        {
            _window = window;
            _console = (TextBox)window.FindName("_consoleBuffer");
            _messages = (TextBox)window.FindName("_messageBuffer");

            DiscoverTests = new RelayCommand(ExecuteDiscoverTests, CanExecuteDiscoverTests);
            SelectDNX = new RelayCommand(ExecuteSelectDNX, CanExecuteSelectDNX);
            SelectProject = new RelayCommand(ExecuteSelectProject, CanExecuteSelectProject);
            StartTestHost = new RelayCommand(ExecuteStartTestHost, CanExecuteStartTestHost);
            RunAllTests = new RelayCommand(ExecuteRunAllTests, CanExecuteRunAllTests);
            RunSelectedTests = new RelayCommand(ExecuteRunSelectedTests, CanExecuteRunSelectedTests);
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
                InitialDirectory = DNX == null ? null : Path.GetDirectoryName(DNX),
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
                InitialDirectory = SelectedProject == null ? null : Path.GetDirectoryName(SelectedProject),
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

        }

        public ICommand DiscoverTests { get; }

        private bool CanExecuteDiscoverTests(object _)
        {
            return !IsRunning && IsReady;
        }

        private async void ExecuteDiscoverTests(object _)
        {
            var wrapper = new TestHostWrapper(DNX);
            _console.Text = string.Empty;
            _messages.Text = string.Empty;
            wrapper.ConsoleOutputReceived += TestHost_ConsoleOutputReceived;
            wrapper.MessageReceived += TestHost_MessageReceived;

            try
            {
                IsRunning = true;
                await wrapper.RunListAsync(SelectedProject);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
            }
            finally
            {
                wrapper.MessageReceived -= TestHost_MessageReceived;
                wrapper.ConsoleOutputReceived -= TestHost_ConsoleOutputReceived;
                IsRunning = false;
            }
        }

        public ICommand RunAllTests { get; }

        private bool CanExecuteRunAllTests(object _)
        {
            return !IsRunning && IsReady;
        }

        private async void ExecuteRunAllTests(object _)
        {
            var wrapper = new TestHostWrapper(DNX);
            _console.Text = string.Empty;
            _messages.Text = string.Empty;
            wrapper.ConsoleOutputReceived += TestHost_ConsoleOutputReceived;
            wrapper.MessageReceived += TestHost_MessageReceived;

            try
            {
                IsRunning = true;    
                await wrapper.RunTestsAsync(SelectedProject);
            }
            catch (Exception ex)
            {
                ShowErrorDialog(ex);
            }
            finally
            {
                wrapper.MessageReceived -= TestHost_MessageReceived;
                wrapper.ConsoleOutputReceived -= TestHost_ConsoleOutputReceived;
                IsRunning = false;
            }
        }

        public ICommand RunSelectedTests { get; }

        private bool CanExecuteRunSelectedTests(object _)
        {
            return !IsRunning && IsReady;
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
    }
}
