// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.Expiration.Interfaces;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.FileProviders
{
    public class PhysicalFileProviderTests
    {
        private const int WAIT_TIME_FOR_TRIGGER_TO_FIRE = 2 * 100;

        [Fact]
        public void ExistingFilesReturnTrue()
        {
            var provider = new PhysicalFileProvider(Environment.CurrentDirectory);
            var info = provider.GetFileInfo("File.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(true);
            info.IsReadOnly.ShouldBe(false);

            info = provider.GetFileInfo("/File.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(true);
            info.IsReadOnly.ShouldBe(false);
        }

        [Fact]
        public async Task ModifyContent_And_Delete_File_Succeeds_And_Callsback_Registered_Triggers()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "OldContent");
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var fileInfo = provider.GetFileInfo(fileName);
            fileInfo.Length.ShouldBe(new FileInfo(fileInfo.PhysicalPath).Length);
            fileInfo.Exists.ShouldBe(true);

            IExpirationTrigger trigger3 = null, trigger4 = null;
            var trigger1 = provider.Watch(fileName);
            var trigger2 = provider.Watch(fileName);

            // Valid trigger1 created.
            trigger1.ShouldNotBe(null);
            trigger1.IsExpired.ShouldBe(false);
            trigger1.ActiveExpirationCallbacks.ShouldBe(true);

            // Valid trigger2 created.
            trigger2.ShouldNotBe(null);
            trigger2.IsExpired.ShouldBe(false);
            trigger2.ActiveExpirationCallbacks.ShouldBe(true);

            // Trigger is the same for a specific file.
            trigger1.ShouldBe(trigger2);

            trigger1.RegisterExpirationCallback(state =>
            {
                var infoFromState = state as IFileInfo;
                trigger3 = provider.Watch(infoFromState.Name);
                trigger3.ShouldNotBe(null);
                trigger3.RegisterExpirationCallback(_ => { }, null);
                trigger3.IsExpired.ShouldBe(false);
            }, state: fileInfo);

            trigger2.RegisterExpirationCallback(state =>
            {
                var infoFromState = state as IFileInfo;
                trigger4 = provider.Watch(infoFromState.Name);
                trigger4.ShouldNotBe(null);
                trigger4.RegisterExpirationCallback(_ => { }, null);
                trigger4.IsExpired.ShouldBe(false);
            }, state: fileInfo);

            // Write new content.
            var newData = Encoding.UTF8.GetBytes("OldContent + NewContent");
            fileInfo.WriteContent(newData);
            fileInfo.Exists.ShouldBe(true);
            fileInfo.Length.ShouldBe(newData.Length);
            // Wait for callbacks to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            trigger1.IsExpired.ShouldBe(true);
            trigger2.IsExpired.ShouldBe(true);

            // Trigger is the same for a specific file.
            trigger3.ShouldBe(trigger4);
            // A new trigger is created.
            trigger3.ShouldNotBe(trigger1);

            // Delete the file and verify file info is updated.
            fileInfo.Delete();
            fileInfo.Exists.ShouldBe(false);
            new FileInfo(fileLocation).Exists.ShouldBe(false);

            // Wait for callbacks to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            trigger3.IsExpired.ShouldBe(true);
            trigger4.IsExpired.ShouldBe(true);
        }

        [Fact]
        public void Missing_Hidden_And_FilesStartingWithPeriod_ReturnFalse()
        {
            var root = Path.GetTempPath();
            var hiddenFileName = Path.Combine(root, Guid.NewGuid().ToString());
            File.WriteAllText(hiddenFileName, "Content");
            var fileNameStartingWithPeriod = Path.Combine(root, ".", Guid.NewGuid().ToString());
            File.WriteAllText(fileNameStartingWithPeriod, "Content");
            var fileInfo = new FileInfo(hiddenFileName);
            File.SetAttributes(hiddenFileName, fileInfo.Attributes | FileAttributes.Hidden);

            var provider = new PhysicalFileProvider(Path.GetTempPath());

            provider.GetFileInfo(Guid.NewGuid().ToString()).Exists.ShouldBe(false);
            provider.GetFileInfo(hiddenFileName).Exists.ShouldBe(false);
            provider.GetFileInfo(fileNameStartingWithPeriod).Exists.ShouldBe(false);
        }

        [Fact]
        public void SubPathActsAsRoot()
        {
            var provider = new PhysicalFileProvider(Path.Combine(Environment.CurrentDirectory, "sub"));
            var info = provider.GetFileInfo("File2.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(true);
        }

        [Fact]
        public void GetDirectoryContents_FromRootPath_ForEmptyDirectoryName()
        {
            var provider = new PhysicalFileProvider(Path.Combine(Environment.CurrentDirectory, "sub"));
            var info = provider.GetDirectoryContents(string.Empty);
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(true);
            var firstDirectory = info.Where(f => f.IsDirectory).Where(f => f.Exists).FirstOrDefault();
            Should.Throw<InvalidOperationException>(() => firstDirectory.CreateReadStream());
            Should.Throw<InvalidOperationException>(() => firstDirectory.WriteContent(new byte[10]));

            var fileInfo = info.Where(f => f.Name == "File2.txt").FirstOrDefault();
            fileInfo.ShouldNotBe(null);
            fileInfo.Exists.ShouldBe(true);
        }

        [Fact]
        public void NotFoundFileInfo_BasicTests()
        {
            var info = new NotFoundFileInfo("NotFoundFile.txt");
            Should.Throw<InvalidOperationException>(() => info.CreateReadStream());
            Should.Throw<InvalidOperationException>(() => info.WriteContent(new byte[10]));
            Should.Throw<InvalidOperationException>(() => info.Delete());
        }

        [Fact]
        public void RelativePathPastRootNotAllowed()
        {
            var serviceProvider = CallContextServiceLocator.Locator.ServiceProvider;
            var appEnvironment = (IApplicationEnvironment)serviceProvider.GetService(typeof(IApplicationEnvironment));

            var provider = new PhysicalFileProvider(Path.Combine(Environment.CurrentDirectory, "sub"));

            var info = provider.GetFileInfo("..\\File.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);

            info = provider.GetFileInfo(".\\..\\File.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);

            info = provider.GetFileInfo("File2.txt");
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(true);
            info.PhysicalPath.ShouldBe(Path.Combine(appEnvironment.ApplicationBasePath, "sub", "File2.txt"));
        }

        [Fact]
        public void AbsolutePathNotAllowed()
        {
            var serviceProvider = CallContextServiceLocator.Locator.ServiceProvider;
            var appEnvironment = (IApplicationEnvironment)serviceProvider.GetService(typeof(IApplicationEnvironment));

            var provider = new PhysicalFileProvider(Path.Combine(Environment.CurrentDirectory, "sub"));

            var applicationBase = appEnvironment.ApplicationBasePath;
            var file1 = Path.Combine(applicationBase, "File.txt");

            var info = provider.GetFileInfo(file1);
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);

            var file2 = Path.Combine(applicationBase, "sub", "File2.txt");
            info = provider.GetFileInfo(file2);
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);

            var directory1 = Path.Combine(applicationBase, "sub");
            var directoryContents = provider.GetDirectoryContents(directory1);
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);

            var directory2 = Path.Combine(applicationBase, "Does_Not_Exists");
            directoryContents = provider.GetDirectoryContents(directory2);
            info.ShouldNotBe(null);
            info.Exists.ShouldBe(false);
        }

        [Fact]
        public async Task Createdtrigger_Same_For_A_File_And_Callsback_AllRegisteredTriggers_OnChange()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "Content");
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            var count = 10;
            var tasks = new List<Task>(count);
            var triggers = new IExpirationTrigger[count];
            var callbackResults = new bool[count];

            for (int i = 0; i < count; i++)
            {
                tasks.Add(new Task(index =>
                {
                    var expirationTrigger = provider.Watch(fileName);
                    triggers[(int)index] = expirationTrigger;
                    expirationTrigger.ShouldNotBe(null);
                    expirationTrigger.IsExpired.ShouldNotBe(true);
                    expirationTrigger.RegisterExpirationCallback(_ => { callbackResults[(int)index] = true; }, index);
                }, state: i));
            }

            // Simulating multiple concurrent requests to the same file.
            Parallel.ForEach(tasks, task => task.Start());
            await Task.WhenAll(tasks);
            File.AppendAllText(fileLocation, "UpdatedContent");

            // Some warm up time for the callbacks to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);

            for (int index = 1; index < count; index++)
            {
                triggers[index].ShouldBe(triggers[index - 1]);
            }

            callbackResults.All(c => c == true).ShouldBe(true);

            provider.GetFileInfo(fileName).Delete();
        }

        [Fact]
        public async Task FileTrigger_NotTriggered_After_Expiry()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "Content");
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            var expirationTrigger = provider.Watch(fileName);
            int invocationCount = 0;
            expirationTrigger.RegisterExpirationCallback(_ => { invocationCount++; }, null);

            // Callback expected for this change.
            File.AppendAllText(fileLocation, "UpdatedContent1");

            // Callback not expected for this change.
            File.AppendAllText(fileLocation, "UpdatedContent2");

            // Wait for callbacks to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);

            invocationCount.ShouldBe(1);

            provider.GetFileInfo(fileName).Delete();
        }

        [Fact]
        public void Trigger_Is_FileName_Case_Insensitive()
        {
            var fileName = Guid.NewGuid().ToString() + 'A';
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "Content");
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            var expirationTrigger = provider.Watch(fileName);
            var lowerCaseExpirationTrigger = provider.Watch(fileName.ToLowerInvariant());
            expirationTrigger.ShouldBe(lowerCaseExpirationTrigger);

            provider.GetFileInfo(fileName).Delete();
        }

        [Fact]
        public async Task Trigger_With_MultipleFiles()
        {
            var fileName1 = Guid.NewGuid().ToString();
            var fileName2 = Guid.NewGuid().ToString();
            var fileLocation1 = Path.Combine(Path.GetTempPath(), fileName1);
            var fileLocation2 = Path.Combine(Path.GetTempPath(), fileName2);
            File.WriteAllText(fileLocation1, "Content1");
            File.WriteAllText(fileLocation2, "Content2");
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            int invocationCount1 = 0, invocationCount2 = 0;
            var trigger1 = provider.Watch(fileName1);
            trigger1.RegisterExpirationCallback(_ => { invocationCount1++; }, null);
            var trigger2 = provider.Watch(fileName2);
            trigger2.RegisterExpirationCallback(_ => { invocationCount2++; }, null);

            trigger1.ShouldNotBe(null);
            trigger1.IsExpired.ShouldNotBe(true);
            trigger1.ActiveExpirationCallbacks.ShouldBe(true);

            trigger2.ShouldNotBe(null);
            trigger2.IsExpired.ShouldNotBe(true);
            trigger2.ActiveExpirationCallbacks.ShouldBe(true);

            trigger1.ShouldNotBe(trigger2);

            File.AppendAllText(fileLocation1, "Update1");
            File.AppendAllText(fileLocation2, "Update2");

            // Wait for callbacks to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);

            invocationCount1.ShouldBe(1);
            invocationCount2.ShouldBe(1);
            trigger1.IsExpired.ShouldBe(true);
            trigger2.IsExpired.ShouldBe(true);

            provider.GetFileInfo(fileName1).Delete();
            provider.GetFileInfo(fileName2).Delete();

            // Callbacks not invoked on expired triggers.
            invocationCount1.ShouldBe(1);
            invocationCount2.ShouldBe(1);
        }

        [Fact]
        public async Task Trigger_Callbacks_Are_Async_And_TriggerNotAffected_By_Exceptions()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "Content");
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            var expirationTrigger = provider.Watch(fileName);
            expirationTrigger.RegisterExpirationCallback(async _ =>
            {
                await Task.Delay(10 * 1000);
                throw new Exception("Callback throwing exception");
            }, null);

            File.AppendAllText(fileLocation, "UpdatedContent");
            // Wait for callback to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            expirationTrigger.IsExpired.ShouldBe(true);

            // Verify file system watcher is stable.
            int callbackCount = 0;
            var expirationTriggerAfterCallbackException = provider.Watch(fileName);
            expirationTriggerAfterCallbackException.RegisterExpirationCallback(_ => { callbackCount++; }, null);
            File.AppendAllText(fileLocation, "UpdatedContent");

            // Wait for callback to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            expirationTrigger.IsExpired.ShouldBe(true);
            callbackCount.ShouldBe(1);

            provider.GetFileInfo(fileName).Delete();
        }

        [Fact]
        public void Trigger_For_Null_Empty_Whitespace_Filters()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            var trigger = provider.Watch(null);
            trigger.IsExpired.ShouldBe(false);
            trigger.ActiveExpirationCallbacks.ShouldBe(false);

            trigger = provider.Watch(string.Empty);
            trigger.IsExpired.ShouldBe(false);
            trigger.ActiveExpirationCallbacks.ShouldBe(true);

            // White space.
            trigger = provider.Watch("  ");
            trigger.IsExpired.ShouldBe(false);
            trigger.ActiveExpirationCallbacks.ShouldBe(true);

            // Absolute path.
            trigger = provider.Watch(Path.Combine(Path.GetTempPath() + "filename"));
            trigger.IsExpired.ShouldBe(false);
            trigger.ActiveExpirationCallbacks.ShouldBe(false);
        }

        [Fact]
        public async Task Trigger_Fired_For_File_Or_Directory_Create_And_Delete()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            string fileName = Guid.NewGuid().ToString();
            string directoryName = Guid.NewGuid().ToString();

            int triggerCount = 0;
            var fileTrigger = provider.Watch(fileName);
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);
            var directoryTrigger = provider.Watch(directoryName);
            directoryTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            fileTrigger.ShouldNotBe(directoryTrigger);

            File.WriteAllText(Path.Combine(Path.GetTempPath(), fileName), "Content");
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), directoryName));

            // Wait for triggers to fire.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);

            triggerCount.ShouldBe(2);

            fileTrigger.IsExpired.ShouldBe(true);
            directoryTrigger.IsExpired.ShouldBe(true);

            fileTrigger = provider.Watch(fileName);
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);
            directoryTrigger = provider.Watch(directoryName);
            directoryTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            provider.GetFileInfo(fileName).Delete();
            Directory.Delete(Path.Combine(Path.GetTempPath(), directoryName));

            // Wait for triggers to fire.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            triggerCount.ShouldBe(4);
        }

        [Fact]
        public async Task Triggers_With_Path_Ending_With_Slash()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            string fileName = Guid.NewGuid().ToString();
            string folderName = Guid.NewGuid().ToString();

            int triggerCount = 0;
            var fileTrigger = provider.Watch("/" + folderName + "/");
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            var folderPath = Path.Combine(Path.GetTempPath(), folderName);
            Directory.CreateDirectory(folderPath);
            File.WriteAllText(Path.Combine(folderPath, fileName), "Content");

            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            triggerCount.ShouldBe(1);

            fileTrigger = provider.Watch("/" + folderName + "/");
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            File.AppendAllText(Path.Combine(folderPath, fileName), "UpdatedContent");
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            triggerCount.ShouldBe(2);

            fileTrigger = provider.Watch("/" + folderName + "/");
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            File.Delete(Path.Combine(folderPath, fileName));
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            triggerCount.ShouldBe(3);
        }

        [Fact]
        public async Task Triggers_With_Path_Not_Ending_With_Slash()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            string directoryName = Guid.NewGuid().ToString();
            string fileName = Guid.NewGuid().ToString();

            int triggerCount = 0;
            // Matches file/directory with this name.
            var fileTrigger = provider.Watch("/" + directoryName);
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), directoryName));
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            triggerCount.ShouldBe(1);

            // Matches file/directory with this name.
            fileTrigger = provider.Watch("/" + fileName);
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            File.WriteAllText(Path.Combine(Path.GetTempPath(), fileName), "Content");
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            triggerCount.ShouldBe(2);
        }

        [Fact]
        public async Task Triggers_With_Regular_Expressions()
        {
            var pattern1 = "**/*";
            var pattern2 = "*.cshtml";
            var root = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString();
            var subFolder = Path.Combine(root, Guid.NewGuid().ToString());
            Directory.CreateDirectory(subFolder);

            int pattern1TriggerCount = 0, pattern2TriggerCount = 0;
            Action<object> callback1 = _ => { pattern1TriggerCount++; };
            Action<object> callback2 = _ => { pattern2TriggerCount++; };

            var provider = new PhysicalFileProvider(root);
            var trigger1 = provider.Watch(pattern1);
            trigger1.RegisterExpirationCallback(callback1, null);
            var trigger2 = provider.Watch(pattern2);
            trigger2.RegisterExpirationCallback(callback2, null);

            File.WriteAllText(Path.Combine(root, fileName + ".cshtml"), "Content");

            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            pattern1TriggerCount.ShouldBe(1);
            pattern2TriggerCount.ShouldBe(1);

            trigger1 = provider.Watch(pattern1);
            trigger1.RegisterExpirationCallback(callback1, null);
            trigger2 = provider.Watch(pattern2);
            trigger2.RegisterExpirationCallback(callback2, null);
            File.WriteAllText(Path.Combine(subFolder, fileName + ".txt"), "Content");

            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            pattern1TriggerCount.ShouldBe(2);
            pattern2TriggerCount.ShouldBe(1);

            Directory.Delete(subFolder, true);
            provider.GetFileInfo(fileName + ".cshtml").Delete();
        }

        [Fact]
        public async Task Triggers_With_Regular_Expression_Filters()
        {
            var pattern1 = "**/*";
            var pattern2 = "*.cshtml";
            var root = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString();
            var subFolder = Path.Combine(root, Guid.NewGuid().ToString());
            Directory.CreateDirectory(subFolder);

            int pattern1TriggerCount = 0, pattern2TriggerCount = 0;
            Action<object> callback1 = _ => { pattern1TriggerCount++; };
            Action<object> callback2 = _ => { pattern2TriggerCount++; };

            var provider = new PhysicalFileProvider(root);
            var trigger1 = provider.Watch(pattern1);
            trigger1.RegisterExpirationCallback(callback1, null);
            var trigger2 = provider.Watch(pattern2);
            trigger2.RegisterExpirationCallback(callback2, null);

            File.WriteAllText(Path.Combine(root, fileName + ".cshtml"), "Content");

            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            pattern1TriggerCount.ShouldBe(1);
            pattern2TriggerCount.ShouldBe(1);

            trigger1 = provider.Watch(pattern1);
            trigger1.RegisterExpirationCallback(callback1, null);
            trigger2 = provider.Watch(pattern2);
            trigger2.RegisterExpirationCallback(callback2, null);
            File.WriteAllText(Path.Combine(subFolder, fileName + ".txt"), "Content");

            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            pattern1TriggerCount.ShouldBe(2);
            pattern2TriggerCount.ShouldBe(1);

            Directory.Delete(subFolder, true);
            provider.GetFileInfo(fileName + ".cshtml").Delete();
        }

        [Fact]
        public async Task Triggers_With_Regular_Expression_Pointing_To_SubFolder()
        {
            var subFolderName = Guid.NewGuid().ToString();
            var pattern1 = "**/*";
            var pattern2 = string.Format("{0}/**/*.cshtml", subFolderName);
            var root = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString();
            var subFolder = Path.Combine(root, subFolderName);
            Directory.CreateDirectory(subFolder);

            int pattern1TriggerCount = 0, pattern2TriggerCount = 0;
            var provider = new PhysicalFileProvider(root);
            var trigger1 = provider.Watch(pattern1);
            trigger1.RegisterExpirationCallback(_ => { pattern1TriggerCount++; }, null);
            var trigger2 = provider.Watch(pattern2);
            trigger2.RegisterExpirationCallback(_ => { pattern2TriggerCount++; }, null);

            File.WriteAllText(Path.Combine(root, fileName + ".cshtml"), "Content");

            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            pattern1TriggerCount.ShouldBe(1);
            pattern2TriggerCount.ShouldBe(0);

            trigger1 = provider.Watch(pattern1);
            trigger1.RegisterExpirationCallback(_ => { pattern1TriggerCount++; }, null);
            // Register this trigger again.
            var trigger3 = provider.Watch(pattern2);
            trigger3.RegisterExpirationCallback(_ => { pattern2TriggerCount++; }, null);
            trigger3.ShouldBe(trigger2);
            File.WriteAllText(Path.Combine(subFolder, fileName + ".cshtml"), "Content");

            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            pattern1TriggerCount.ShouldBe(2);
            pattern2TriggerCount.ShouldBe(2);

            Directory.Delete(subFolder, true);
            provider.GetFileInfo(fileName + ".cshtml").Delete();
        }

        [Fact]
        public void Triggers_With_Forward_And_Backward_Slash()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var trigger1 = provider.Watch("/a/b");
            var trigger2 = provider.Watch("a/b");
            var trigger3 = provider.Watch(@"a\b");

            trigger1.ShouldBe(trigger2);
            trigger2.ShouldBe(trigger3);

            trigger1.ActiveExpirationCallbacks.ShouldBe(true);
            trigger2.ActiveExpirationCallbacks.ShouldBe(true);
            trigger3.ActiveExpirationCallbacks.ShouldBe(true);

            trigger1.IsExpired.ShouldBe(false);
            trigger2.IsExpired.ShouldBe(false);
            trigger3.IsExpired.ShouldBe(false);
        }

        [Fact]
        public async Task Trigger_Fired_On_Directory_Name_Change()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var oldDirectoryName = Guid.NewGuid().ToString();
            var newDirectoryName = Guid.NewGuid().ToString();
            var oldDirectoryFullPath = Path.Combine(Path.GetTempPath(), oldDirectoryName);
            var newDirectoryFullPath = Path.Combine(Path.GetTempPath(), newDirectoryName);

            Directory.CreateDirectory(oldDirectoryFullPath);
            var oldDirectoryTrigger = provider.Watch("**/" + oldDirectoryName);
            var newDirectoryTrigger = provider.Watch("**/" + newDirectoryName);
            var oldTriggers = new List<IExpirationTrigger>();
            var newTriggers = new List<IExpirationTrigger>();

            oldTriggers.Add(provider.Watch(Path.Combine("**", oldDirectoryName, "*.txt")));
            newTriggers.Add(provider.Watch(Path.Combine("**", newDirectoryName, "*.txt")));

            for (int i = 0; i < 5; i++)
            {
                var fileName = string.Format("test{0}.txt", i);
                File.WriteAllText(Path.Combine(oldDirectoryFullPath, fileName), "test content");
                oldTriggers.Add(provider.Watch(Path.Combine("**", oldDirectoryName, fileName)));
                newTriggers.Add(provider.Watch(Path.Combine("**", newDirectoryName, fileName)));
            }

            await Task.Delay(2 * 100); // Give it a while before trying rename.
            Directory.Move(oldDirectoryFullPath, newDirectoryFullPath);

            // Wait for triggers to fire.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            oldDirectoryTrigger.IsExpired.ShouldBe(true);
            newDirectoryTrigger.IsExpired.ShouldBe(true);
            oldTriggers.All(t => t.IsExpired).ShouldBe(true);
            newTriggers.All(t => t.IsExpired).ShouldBe(true);

            newDirectoryTrigger = provider.Watch(newDirectoryName);
            newTriggers = new List<IExpirationTrigger>();

            newTriggers.Add(provider.Watch(Path.Combine("**", newDirectoryName, "*.txt")));
            for (int i = 0; i < 5; i++)
            {
                var fileName = string.Format("test{0}.txt", i);
                newTriggers.Add(provider.Watch(Path.Combine("**", newDirectoryName, fileName)));
            }

            Directory.Delete(newDirectoryFullPath, true);

            // Wait for triggers to fire.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            newDirectoryTrigger.IsExpired.ShouldBe(true);
            newTriggers.All(t => t.IsExpired).ShouldBe(true);
        }

        [Fact]
        public async Task Triggers_NotFired_For_FileNames_Starting_With_Period_And_Hidden_Files()
        {
            var root = Path.GetTempPath();
            var hiddenFileName = Path.Combine(root, Guid.NewGuid().ToString());
            File.WriteAllText(hiddenFileName, "Content");
            var systemFileName = Path.Combine(root, Guid.NewGuid().ToString());
            File.WriteAllText(systemFileName, "Content");
            var fileNameStartingWithPeriod = Path.Combine(root, "." + Guid.NewGuid().ToString());
            File.WriteAllText(fileNameStartingWithPeriod, "Content");
            var fileInfo = new FileInfo(hiddenFileName);
            File.SetAttributes(hiddenFileName, fileInfo.Attributes | FileAttributes.Hidden);
            fileInfo = new FileInfo(systemFileName);
            File.SetAttributes(systemFileName, fileInfo.Attributes | FileAttributes.System);

            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var hiddenFileTrigger = provider.Watch(Path.GetFileName(hiddenFileName));
            var triggerFileNameStartingPeriod = provider.Watch(Path.GetFileName(fileNameStartingWithPeriod));
            var systemFileTrigger = provider.Watch(Path.GetFileName(systemFileName));

            hiddenFileTrigger.IsExpired.ShouldBe(false);
            triggerFileNameStartingPeriod.IsExpired.ShouldBe(false);
            systemFileTrigger.IsExpired.ShouldBe(false);

            File.AppendAllText(hiddenFileName, "Appending text");
            File.WriteAllText(fileNameStartingWithPeriod, "Updated Contents");
            File.AppendAllText(systemFileName, "Appending text");

            // Wait for triggers to fire.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);

            hiddenFileTrigger.IsExpired.ShouldBe(false);
            triggerFileNameStartingPeriod.IsExpired.ShouldBe(false);
            systemFileTrigger.IsExpired.ShouldBe(false);
        }
    }
}