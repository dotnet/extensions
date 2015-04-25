// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Expiration.Interfaces;
using Xunit;

namespace Microsoft.AspNet.FileProviders
{
    public class PhysicalFileProviderTests
    {
        private const int WAIT_TIME_FOR_TRIGGER_TO_FIRE = 2 * 100;

        [Fact]
        public void ExistingFilesReturnTrue()
        {
            var provider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
            var info = provider.GetFileInfo("File.txt");
            Assert.NotNull(info);
            Assert.True(info.Exists);

            info = provider.GetFileInfo("/File.txt");
            Assert.NotNull(info);
            Assert.True(info.Exists);
        }

        [Fact]
        public async Task ModifyContent_And_Delete_File_Succeeds_And_Callsback_Registered_Triggers()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "OldContent");
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var fileInfo = provider.GetFileInfo(fileName);
            Assert.Equal(new FileInfo(fileInfo.PhysicalPath).Length, fileInfo.Length);
            Assert.True(fileInfo.Exists);

            IExpirationTrigger trigger3 = null, trigger4 = null;
            var trigger1 = provider.Watch(fileName);
            var trigger2 = provider.Watch(fileName);

            // Valid trigger1 created.
            Assert.NotNull(trigger1);
            Assert.False(trigger1.IsExpired);
            Assert.True(trigger1.ActiveExpirationCallbacks);

            // Valid trigger2 created.
            Assert.NotNull(trigger2);
            Assert.False(trigger2.IsExpired);
            Assert.True(trigger2.ActiveExpirationCallbacks);

            // Trigger is the same for a specific file.
            Assert.Equal(trigger2, trigger1);

            trigger1.RegisterExpirationCallback(state =>
            {
                var infoFromState = state as IFileInfo;
                trigger3 = provider.Watch(infoFromState.Name);
                Assert.NotNull(trigger3);
                trigger3.RegisterExpirationCallback(_ => { }, null);
                Assert.False(trigger3.IsExpired);
            }, state: fileInfo);

            trigger2.RegisterExpirationCallback(state =>
            {
                var infoFromState = state as IFileInfo;
                trigger4 = provider.Watch(infoFromState.Name);
                Assert.NotNull(trigger4);
                trigger4.RegisterExpirationCallback(_ => { }, null);
                Assert.False(trigger4.IsExpired);
            }, state: fileInfo);

            // Write new content.
            File.WriteAllText(fileLocation, "OldContent + NewContent");
            Assert.True(fileInfo.Exists);
            // Wait for callbacks to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            Assert.True(trigger1.IsExpired);
            Assert.True(trigger2.IsExpired);

            // Trigger is the same for a specific file.
            Assert.Equal(trigger4, trigger3);
            // A new trigger is created.
            Assert.NotEqual(trigger1, trigger3);

            // Delete the file and verify file info is updated.
            File.Delete(fileLocation);
            fileInfo = provider.GetFileInfo(fileName);
            Assert.False(fileInfo.Exists);
            Assert.False(new FileInfo(fileLocation).Exists);

            // Wait for callbacks to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            Assert.True(trigger3.IsExpired);
            Assert.True(trigger4.IsExpired);
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

            var file = provider.GetFileInfo(Guid.NewGuid().ToString());
            Assert.False(file.Exists);
            Assert.Throws<FileNotFoundException>(() => file.CreateReadStream());

            file = provider.GetFileInfo(hiddenFileName);
            Assert.False(file.Exists);
            Assert.Throws<FileNotFoundException>(() => file.CreateReadStream());

            file = provider.GetFileInfo(fileNameStartingWithPeriod);
            Assert.False(file.Exists);
            Assert.Throws<FileNotFoundException>(() => file.CreateReadStream());
        }

        [Fact]
        public void SubPathActsAsRoot()
        {
            var provider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "sub"));
            var info = provider.GetFileInfo("File2.txt");
            Assert.NotNull(info);
            Assert.True(info.Exists);
        }

        [Fact]
        public void GetDirectoryContents_FromRootPath_ForEmptyDirectoryName()
        {
            var provider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "sub"));
            var info = provider.GetDirectoryContents(string.Empty);
            Assert.NotNull(info);
            Assert.True(info.Exists);
            var firstDirectory = info.Where(f => f.IsDirectory).Where(f => f.Exists).FirstOrDefault();
            Assert.Throws<InvalidOperationException>(() => firstDirectory.CreateReadStream());

            var fileInfo = info.Where(f => f.Name == "File2.txt").FirstOrDefault();
            Assert.NotNull(fileInfo);
            Assert.True(fileInfo.Exists);
        }

        [Fact]
        public void RelativePathPastRootNotAllowed()
        {
            var provider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "sub"));

            var info = provider.GetFileInfo("..\\File.txt");
            Assert.NotNull(info);
            Assert.False(info.Exists);

            info = provider.GetFileInfo(".\\..\\File.txt");
            Assert.NotNull(info);
            Assert.False(info.Exists);

            info = provider.GetFileInfo("File2.txt");
            Assert.NotNull(info);
            Assert.True(info.Exists);
            Assert.Equal(Path.Combine(Directory.GetCurrentDirectory(), "sub", "File2.txt"), info.PhysicalPath);
        }

        [Fact]
        public void AbsolutePathNotAllowed()
        {
            var provider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "sub"));

            var applicationBase = Directory.GetCurrentDirectory();
            var file1 = Path.Combine(applicationBase, "File.txt");

            var info = provider.GetFileInfo(file1);
            Assert.NotNull(info);
            Assert.False(info.Exists);

            var file2 = Path.Combine(applicationBase, "sub", "File2.txt");
            info = provider.GetFileInfo(file2);
            Assert.NotNull(info);
            Assert.False(info.Exists);

            var directory1 = Path.Combine(applicationBase, "sub");
            var directoryContents = provider.GetDirectoryContents(directory1);
            Assert.NotNull(info);
            Assert.False(info.Exists);

            var directory2 = Path.Combine(applicationBase, "Does_Not_Exists");
            directoryContents = provider.GetDirectoryContents(directory2);
            Assert.NotNull(info);
            Assert.False(info.Exists);
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
                    Assert.NotNull(expirationTrigger);
                    Assert.False(expirationTrigger.IsExpired);
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
                Assert.Equal(triggers[index - 1], triggers[index]);
            }

            Assert.True(callbackResults.All(c => c));

            File.Delete(fileLocation);
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

            Assert.Equal(1, invocationCount);

            File.Delete(fileLocation);
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
            Assert.Equal(lowerCaseExpirationTrigger, expirationTrigger);

            File.Delete(fileLocation);
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

            Assert.NotNull(trigger1);
            Assert.False(trigger1.IsExpired);
            Assert.True(trigger1.ActiveExpirationCallbacks);

            Assert.NotNull(trigger2);
            Assert.False(trigger2.IsExpired);
            Assert.True(trigger2.ActiveExpirationCallbacks);

            Assert.NotEqual(trigger2, trigger1);

            File.AppendAllText(fileLocation1, "Update1");
            File.AppendAllText(fileLocation2, "Update2");

            // Wait for callbacks to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);

            Assert.Equal(1, invocationCount1);
            Assert.Equal(1, invocationCount2);
            Assert.True(trigger1.IsExpired);
            Assert.True(trigger2.IsExpired);

            File.Delete(fileLocation1);
            File.Delete(fileLocation2);

            // Callbacks not invoked on expired triggers.
            Assert.Equal(1, invocationCount1);
            Assert.Equal(1, invocationCount2);
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
            Assert.True(expirationTrigger.IsExpired);

            // Verify file system watcher is stable.
            int callbackCount = 0;
            var expirationTriggerAfterCallbackException = provider.Watch(fileName);
            expirationTriggerAfterCallbackException.RegisterExpirationCallback(_ => { callbackCount++; }, null);
            File.AppendAllText(fileLocation, "UpdatedContent");

            // Wait for callback to be fired.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            Assert.True(expirationTrigger.IsExpired);
            Assert.Equal(1, callbackCount);

            File.Delete(fileLocation);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void Trigger_For_Null_Empty_Whitespace_Filters()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            var trigger = provider.Watch(null);
            Assert.False(trigger.IsExpired);
            Assert.False(trigger.ActiveExpirationCallbacks);

            trigger = provider.Watch(string.Empty);
            Assert.False(trigger.IsExpired);
            Assert.True(trigger.ActiveExpirationCallbacks);

            // White space.
            trigger = provider.Watch("  ");
            Assert.False(trigger.IsExpired);
            Assert.True(trigger.ActiveExpirationCallbacks);

            // Absolute path.
            trigger = provider.Watch(Path.Combine(Path.GetTempPath() + "filename"));
            Assert.False(trigger.IsExpired);
            Assert.False(trigger.ActiveExpirationCallbacks);
        }

        [Fact]
        public async Task Trigger_Fired_For_File_Or_Directory_Create_And_Delete()
        {
            var root = Path.GetTempPath();
            var provider = new PhysicalFileProvider(root);
            string fileName = Guid.NewGuid().ToString();
            string directoryName = Guid.NewGuid().ToString();

            int triggerCount = 0;
            var fileTrigger = provider.Watch(fileName);
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);
            var directoryTrigger = provider.Watch(directoryName);
            directoryTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            Assert.NotEqual(directoryTrigger, fileTrigger);

            File.WriteAllText(Path.Combine(root, fileName), "Content");
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), directoryName));

            // Wait for triggers to fire.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);

            Assert.Equal(2, triggerCount);

            Assert.True(fileTrigger.IsExpired);
            Assert.True(directoryTrigger.IsExpired);

            fileTrigger = provider.Watch(fileName);
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);
            directoryTrigger = provider.Watch(directoryName);
            directoryTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            File.Delete(Path.Combine(root, fileName));
            Directory.Delete(Path.Combine(Path.GetTempPath(), directoryName));

            // Wait for triggers to fire.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            Assert.Equal(4, triggerCount);
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
            Assert.Equal(1, triggerCount);

            fileTrigger = provider.Watch("/" + folderName + "/");
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            File.AppendAllText(Path.Combine(folderPath, fileName), "UpdatedContent");
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            Assert.Equal(2, triggerCount);

            fileTrigger = provider.Watch("/" + folderName + "/");
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            File.Delete(Path.Combine(folderPath, fileName));
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            Assert.Equal(3, triggerCount);
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
            Assert.Equal(1, triggerCount);

            // Matches file/directory with this name.
            fileTrigger = provider.Watch("/" + fileName);
            fileTrigger.RegisterExpirationCallback(_ => { triggerCount++; }, null);

            File.WriteAllText(Path.Combine(Path.GetTempPath(), fileName), "Content");
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            Assert.Equal(2, triggerCount);
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
            Assert.Equal(1, pattern1TriggerCount);
            Assert.Equal(1, pattern2TriggerCount);

            trigger1 = provider.Watch(pattern1);
            trigger1.RegisterExpirationCallback(callback1, null);
            trigger2 = provider.Watch(pattern2);
            trigger2.RegisterExpirationCallback(callback2, null);
            File.WriteAllText(Path.Combine(subFolder, fileName + ".txt"), "Content");

            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            Assert.Equal(2, pattern1TriggerCount);
            Assert.Equal(1, pattern2TriggerCount);

            Directory.Delete(subFolder, true);
            File.Delete(Path.Combine(root, fileName + ".cshtml"));
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
            Assert.Equal(1, pattern1TriggerCount);
            Assert.Equal(1, pattern2TriggerCount);

            trigger1 = provider.Watch(pattern1);
            trigger1.RegisterExpirationCallback(callback1, null);
            trigger2 = provider.Watch(pattern2);
            trigger2.RegisterExpirationCallback(callback2, null);
            File.WriteAllText(Path.Combine(subFolder, fileName + ".txt"), "Content");

            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            Assert.Equal(2, pattern1TriggerCount);
            Assert.Equal(1, pattern2TriggerCount);

            Directory.Delete(subFolder, true);
            File.Delete(Path.Combine(root, fileName + ".cshtml"));
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
            Assert.Equal(1, pattern1TriggerCount);
            Assert.Equal(0, pattern2TriggerCount);

            trigger1 = provider.Watch(pattern1);
            trigger1.RegisterExpirationCallback(_ => { pattern1TriggerCount++; }, null);
            // Register this trigger again.
            var trigger3 = provider.Watch(pattern2);
            trigger3.RegisterExpirationCallback(_ => { pattern2TriggerCount++; }, null);
            Assert.Equal(trigger2, trigger3);
            File.WriteAllText(Path.Combine(subFolder, fileName + ".cshtml"), "Content");

            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);
            Assert.Equal(2, pattern1TriggerCount);
            Assert.Equal(2, pattern2TriggerCount);

            Directory.Delete(subFolder, true);
            File.Delete(Path.Combine(root, fileName + ".cshtml"));
        }

        [Fact]
        public void Triggers_With_Forward_And_Backward_Slash()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var trigger1 = provider.Watch("/a/b");
            var trigger2 = provider.Watch("a/b");
            var trigger3 = provider.Watch(@"a\b");

            Assert.Equal(trigger2, trigger1);
            Assert.Equal(trigger3, trigger2);

            Assert.True(trigger1.ActiveExpirationCallbacks);
            Assert.True(trigger2.ActiveExpirationCallbacks);
            Assert.True(trigger3.ActiveExpirationCallbacks);

            Assert.False(trigger1.IsExpired);
            Assert.False(trigger2.IsExpired);
            Assert.False(trigger3.IsExpired);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
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
            Assert.True(oldDirectoryTrigger.IsExpired);
            Assert.True(newDirectoryTrigger.IsExpired);
            oldTriggers.ForEach(t => Assert.True(t.IsExpired));
            newTriggers.ForEach(t => Assert.True(t.IsExpired));

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
            Assert.True(newDirectoryTrigger.IsExpired);
            newTriggers.ForEach(t => Assert.True(t.IsExpired));
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
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

            Assert.False(hiddenFileTrigger.IsExpired);
            Assert.False(triggerFileNameStartingPeriod.IsExpired);
            Assert.False(systemFileTrigger.IsExpired);

            File.AppendAllText(hiddenFileName, "Appending text");
            File.WriteAllText(fileNameStartingWithPeriod, "Updated Contents");
            File.AppendAllText(systemFileName, "Appending text");

            // Wait for triggers to fire.
            await Task.Delay(WAIT_TIME_FOR_TRIGGER_TO_FIRE);

            Assert.False(hiddenFileTrigger.IsExpired);
            Assert.False(triggerFileNameStartingPeriod.IsExpired);
            Assert.False(systemFileTrigger.IsExpired);
        }
    }
}