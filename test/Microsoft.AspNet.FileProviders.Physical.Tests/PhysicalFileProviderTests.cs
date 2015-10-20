// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.FileProviders
{
    public class PhysicalFileProviderTests
    {
        private const int WaitTimeForTokenToFire = 2 * 100;

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
        public async Task ModifyContent_And_Delete_File_Succeeds_And_Callsback_RegisteredTokens()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "OldContent");
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var fileInfo = provider.GetFileInfo(fileName);
            Assert.Equal(new FileInfo(fileInfo.PhysicalPath).Length, fileInfo.Length);
            Assert.True(fileInfo.Exists);

            var token1 = provider.Watch(fileName);
            var token2 = provider.Watch(fileName);

            // Valid token1 created.
            Assert.NotNull(token1);
            Assert.False(token1.HasChanged);
            Assert.True(token1.ActiveChangeCallbacks);

            // Valid token2 created.
            Assert.NotNull(token2);
            Assert.False(token2.HasChanged);
            Assert.True(token2.ActiveChangeCallbacks);

            // token is the same for a specific file.
            Assert.Equal(token2, token1);

            IChangeToken token3 = null;
            IChangeToken token4 = null;
            token1.RegisterChangeCallback(state =>
            {
                var infoFromState = state as IFileInfo;
                token3 = provider.Watch(infoFromState.Name);
                Assert.NotNull(token3);
                token3.RegisterChangeCallback(_ => { }, null);
                Assert.False(token3.HasChanged);
            }, state: fileInfo);

            token2.RegisterChangeCallback(state =>
            {
                var infoFromState = state as IFileInfo;
                token4 = provider.Watch(infoFromState.Name);
                Assert.NotNull(token4);
                token4.RegisterChangeCallback(_ => { }, null);
                Assert.False(token4.HasChanged);
            }, state: fileInfo);

            // Write new content.
            File.WriteAllText(fileLocation, "OldContent + NewContent");
            Assert.True(fileInfo.Exists);
            // Wait for callbacks to be fired.
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.True(token1.HasChanged);
            Assert.True(token2.HasChanged);

            // token is the same for a specific file.
            Assert.Same(token4, token3);
            // A new token is created.
            Assert.NotEqual(token1, token3);

            // Delete the file and verify file info is updated.
            File.Delete(fileLocation);
            fileInfo = provider.GetFileInfo(fileName);
            Assert.False(fileInfo.Exists);
            Assert.False(new FileInfo(fileLocation).Exists);

            // Wait for callbacks to be fired.
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.True(token3.HasChanged);
            Assert.True(token4.HasChanged);
        }

        [Fact]
        public void Exists_WithNonExistingFile_ReturnsFalse()
        {
            // Set stuff up on disk (nothing to set up here because we're testing a non-existing file)
            var root = Path.GetTempPath();
            var nonExistingFileName = Guid.NewGuid().ToString();

            // Use the file provider to try to read the file info back
            var provider = new PhysicalFileProvider(root);
            var file = provider.GetFileInfo(nonExistingFileName);

            Assert.False(file.Exists);
            Assert.Throws<FileNotFoundException>(() => file.CreateReadStream());
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Hidden files only make sense on Windows.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Hidden files only make sense on Windows.")]
        public void Exists_WithHiddenFile_ReturnsFalse()
        {
            // Set stuff up on disk
            var root = Path.GetTempPath();
            var tempFileName = Guid.NewGuid().ToString();
            var physicalHiddenFileName = Path.Combine(root, tempFileName);
            File.WriteAllText(physicalHiddenFileName, "Content");
            var fileInfo = new FileInfo(physicalHiddenFileName);
            File.SetAttributes(physicalHiddenFileName, fileInfo.Attributes | FileAttributes.Hidden);

            // Use the file provider to try to read the file info back
            var provider = new PhysicalFileProvider(root);
            var file = provider.GetFileInfo(tempFileName);

            Assert.False(file.Exists);
            Assert.Throws<FileNotFoundException>(() => file.CreateReadStream());
        }

        [Fact]
        public void Exists_WithFileStartingWithPeriod_ReturnsFalse()
        {
            // Set stuff up on disk
            var root = Path.GetTempPath();
            var fileNameStartingWithPeriod = "." + Guid.NewGuid().ToString();
            var physicalFileNameStartingWithPeriod = Path.Combine(root, fileNameStartingWithPeriod);
            File.WriteAllText(physicalFileNameStartingWithPeriod, "Content");

            // Use the file provider to try to read the file info back
            var provider = new PhysicalFileProvider(root);
            var file = provider.GetFileInfo(fileNameStartingWithPeriod);

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
        public async Task CreatedToken_Same_For_A_File_And_Callsback_AllRegisteredTokens_OnChange()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "Content");
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            var count = 10;
            var tasks = new List<Task>(count);
            var tokens = new IChangeToken[count];
            var callbackResults = new bool[count];

            for (int i = 0; i < count; i++)
            {
                tasks.Add(new Task(index =>
                {
                    var changeToken = provider.Watch(fileName);
                    tokens[(int)index] = changeToken;
                    Assert.NotNull(changeToken);
                    Assert.False(changeToken.HasChanged);
                    changeToken.RegisterChangeCallback(_ => { callbackResults[(int)index] = true; }, index);
                }, state: i));
            }

            // Simulating multiple concurrent requests to the same file.
            Parallel.ForEach(tasks, task => task.Start());
            await Task.WhenAll(tasks);
            File.AppendAllText(fileLocation, "UpdatedContent");

            // Some warm up time for the callbacks to be fired.
            await Task.Delay(WaitTimeForTokenToFire);

            for (int index = 1; index < count; index++)
            {
                Assert.Equal(tokens[index - 1], tokens[index]);
            }

            Assert.True(callbackResults.All(c => c));

            File.Delete(fileLocation);
        }

        [Fact]
        public async Task FileChangeToken_NotNotified_After_Expiry()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "Content");
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            var changeToken = provider.Watch(fileName);
            int invocationCount = 0;
            changeToken.RegisterChangeCallback(_ => { invocationCount++; }, null);

            // Callback expected for this change.
            File.AppendAllText(fileLocation, "UpdatedContent1");

            // Callback not expected for this change.
            File.AppendAllText(fileLocation, "UpdatedContent2");

            // Wait for callbacks to be fired.
            await Task.Delay(WaitTimeForTokenToFire);

            Assert.Equal(1, invocationCount);

            File.Delete(fileLocation);
        }

        [Fact]
        public void Token_Is_FileName_Case_Insensitive()
        {
            var fileName = Guid.NewGuid().ToString() + 'A';
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "Content");
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            var changeToken = provider.Watch(fileName);
            var lowerCaseChangeToken = provider.Watch(fileName.ToLowerInvariant());
            Assert.Equal(lowerCaseChangeToken, changeToken);

            File.Delete(fileLocation);
        }

        [Fact]
        public async Task Token_With_MultipleFiles()
        {
            var fileName1 = Guid.NewGuid().ToString();
            var fileName2 = Guid.NewGuid().ToString();
            var fileLocation1 = Path.Combine(Path.GetTempPath(), fileName1);
            var fileLocation2 = Path.Combine(Path.GetTempPath(), fileName2);
            File.WriteAllText(fileLocation1, "Content1");
            File.WriteAllText(fileLocation2, "Content2");
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            int invocationCount1 = 0, invocationCount2 = 0;
            var token1 = provider.Watch(fileName1);
            token1.RegisterChangeCallback(_ => { invocationCount1++; }, null);
            var token2 = provider.Watch(fileName2);
            token2.RegisterChangeCallback(_ => { invocationCount2++; }, null);

            Assert.NotNull(token1);
            Assert.False(token1.HasChanged);
            Assert.True(token1.ActiveChangeCallbacks);

            Assert.NotNull(token2);
            Assert.False(token2.HasChanged);
            Assert.True(token2.ActiveChangeCallbacks);

            Assert.NotEqual(token2, token1);

            File.AppendAllText(fileLocation1, "Update1");
            File.AppendAllText(fileLocation2, "Update2");

            // Wait for callbacks to be fired.
            await Task.Delay(WaitTimeForTokenToFire);

            Assert.Equal(1, invocationCount1);
            Assert.Equal(1, invocationCount2);
            Assert.True(token1.HasChanged);
            Assert.True(token2.HasChanged);

            File.Delete(fileLocation1);
            File.Delete(fileLocation2);

            // Callbacks not invoked on changed tokens.
            Assert.Equal(1, invocationCount1);
            Assert.Equal(1, invocationCount2);
        }

        [Fact]
        public async Task Token_Callbacks_Are_Async_And_TokenNotAffected_By_Exceptions()
        {
            var fileName = Guid.NewGuid().ToString();
            var fileLocation = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(fileLocation, "Content");
            var provider = new PhysicalFileProvider(Path.GetTempPath());

            var changeToken = provider.Watch(fileName);
            changeToken.RegisterChangeCallback(async _ =>
            {
                await Task.Delay(10 * 1000);
                throw new Exception("Callback throwing exception");
            }, null);

            File.AppendAllText(fileLocation, "UpdatedContent");
            // Wait for callback to be fired.
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.True(changeToken.HasChanged);

            // Verify file system watcher is stable.
            int callbackCount = 0;
            var changeTokenAfterCallbackException = provider.Watch(fileName);
            changeTokenAfterCallbackException.RegisterChangeCallback(_ => { callbackCount++; }, null);
            File.AppendAllText(fileLocation, "UpdatedContent");

            // Wait for callback to be fired.
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.True(changeToken.HasChanged);
            Assert.Equal(1, callbackCount);

            File.Delete(fileLocation);
        }

        [Fact]
        public void Token_For_Null_Filter()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var token = provider.Watch(null);

            Assert.Same(NoopChangeToken.Singleton, token);
        }

        [Fact]
        public void Token_For_Empty_Filter()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var token = provider.Watch(string.Empty);

            Assert.False(token.HasChanged);
            Assert.True(token.ActiveChangeCallbacks);
        }

        [Fact]
        public void Token_For_Whitespace_Filters()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var token = provider.Watch("  ");

            Assert.False(token.HasChanged);
            Assert.True(token.ActiveChangeCallbacks);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Skipping until #104 is resolved.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Skipping until #104 is resolved.")]
        public void Token_For_AbsolutePath_Filters()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var path = Path.Combine(Path.GetTempPath(), "filename");
            var token = provider.Watch(path);

            Assert.Same(NoopChangeToken.Singleton, token);
        }

        [Fact]
        public async Task Token_Fired_For_File_Or_Directory_Create_And_Delete()
        {
            var root = Path.GetTempPath();
            var provider = new PhysicalFileProvider(root);
            string fileName = Guid.NewGuid().ToString();
            string directoryName = Guid.NewGuid().ToString();

            int tokenCount = 0;
            var filetoken = provider.Watch(fileName);
            filetoken.RegisterChangeCallback(_ => { tokenCount++; }, null);
            var directorytoken = provider.Watch(directoryName);
            directorytoken.RegisterChangeCallback(_ => { tokenCount++; }, null);

            Assert.NotEqual(directorytoken, filetoken);

            File.WriteAllText(Path.Combine(root, fileName), "Content");
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), directoryName));

            // Wait for tokens to fire.
            await Task.Delay(WaitTimeForTokenToFire);

            Assert.Equal(2, tokenCount);

            Assert.True(filetoken.HasChanged);
            Assert.True(directorytoken.HasChanged);

            filetoken = provider.Watch(fileName);
            filetoken.RegisterChangeCallback(_ => { tokenCount++; }, null);
            directorytoken = provider.Watch(directoryName);
            directorytoken.RegisterChangeCallback(_ => { tokenCount++; }, null);

            File.Delete(Path.Combine(root, fileName));
            Directory.Delete(Path.Combine(Path.GetTempPath(), directoryName));

            // Wait for tokens to fire.
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(4, tokenCount);
        }

        [Fact]
        public async Task Tokens_With_Path_Ending_With_Slash()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            string fileName = Guid.NewGuid().ToString();
            string folderName = Guid.NewGuid().ToString();

            int tokenCount = 0;
            var filetoken = provider.Watch("/" + folderName + "/");
            filetoken.RegisterChangeCallback(_ => { tokenCount++; }, null);

            var folderPath = Path.Combine(Path.GetTempPath(), folderName);
            Directory.CreateDirectory(folderPath);
            File.WriteAllText(Path.Combine(folderPath, fileName), "Content");

            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(1, tokenCount);

            filetoken = provider.Watch("/" + folderName + "/");
            filetoken.RegisterChangeCallback(_ => { tokenCount++; }, null);

            File.AppendAllText(Path.Combine(folderPath, fileName), "UpdatedContent");
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(2, tokenCount);

            filetoken = provider.Watch("/" + folderName + "/");
            filetoken.RegisterChangeCallback(_ => { tokenCount++; }, null);

            File.Delete(Path.Combine(folderPath, fileName));
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(3, tokenCount);
        }

        [Fact]
        public async Task Tokens_With_Path_Not_Ending_With_Slash()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            string directoryName = Guid.NewGuid().ToString();
            string fileName = Guid.NewGuid().ToString();

            int tokenCount = 0;
            // Matches file/directory with this name.
            var filetoken = provider.Watch("/" + directoryName);
            filetoken.RegisterChangeCallback(_ => { tokenCount++; }, null);

            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), directoryName));
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(1, tokenCount);

            // Matches file/directory with this name.
            filetoken = provider.Watch("/" + fileName);
            filetoken.RegisterChangeCallback(_ => { tokenCount++; }, null);

            File.WriteAllText(Path.Combine(Path.GetTempPath(), fileName), "Content");
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(2, tokenCount);
        }

        [Fact]
        public async Task Tokens_With_Regular_Expressions()
        {
            var pattern1 = "**/*";
            var pattern2 = "*.cshtml";
            var root = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString();
            var subFolder = Path.Combine(root, Guid.NewGuid().ToString());
            Directory.CreateDirectory(subFolder);

            int pattern1tokenCount = 0, pattern2tokenCount = 0;
            Action<object> callback1 = _ => { pattern1tokenCount++; };
            Action<object> callback2 = _ => { pattern2tokenCount++; };

            var provider = new PhysicalFileProvider(root);
            var token1 = provider.Watch(pattern1);
            token1.RegisterChangeCallback(callback1, null);
            var token2 = provider.Watch(pattern2);
            token2.RegisterChangeCallback(callback2, null);

            File.WriteAllText(Path.Combine(root, fileName + ".cshtml"), "Content");

            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(1, pattern1tokenCount);
            Assert.Equal(1, pattern2tokenCount);

            token1 = provider.Watch(pattern1);
            token1.RegisterChangeCallback(callback1, null);
            token2 = provider.Watch(pattern2);
            token2.RegisterChangeCallback(callback2, null);
            File.WriteAllText(Path.Combine(subFolder, fileName + ".txt"), "Content");

            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(2, pattern1tokenCount);
            Assert.Equal(1, pattern2tokenCount);

            Directory.Delete(subFolder, true);
            File.Delete(Path.Combine(root, fileName + ".cshtml"));
        }

        [Fact]
        public async Task Tokens_With_Regular_Expression_Filters()
        {
            var pattern1 = "**/*";
            var pattern2 = "*.cshtml";
            var root = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString();
            var subFolder = Path.Combine(root, Guid.NewGuid().ToString());
            Directory.CreateDirectory(subFolder);

            int pattern1tokenCount = 0, pattern2tokenCount = 0;
            Action<object> callback1 = _ => { pattern1tokenCount++; };
            Action<object> callback2 = _ => { pattern2tokenCount++; };

            var provider = new PhysicalFileProvider(root);
            var token1 = provider.Watch(pattern1);
            token1.RegisterChangeCallback(callback1, null);
            var token2 = provider.Watch(pattern2);
            token2.RegisterChangeCallback(callback2, null);

            File.WriteAllText(Path.Combine(root, fileName + ".cshtml"), "Content");

            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(1, pattern1tokenCount);
            Assert.Equal(1, pattern2tokenCount);

            token1 = provider.Watch(pattern1);
            token1.RegisterChangeCallback(callback1, null);
            token2 = provider.Watch(pattern2);
            token2.RegisterChangeCallback(callback2, null);
            File.WriteAllText(Path.Combine(subFolder, fileName + ".txt"), "Content");

            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(2, pattern1tokenCount);
            Assert.Equal(1, pattern2tokenCount);

            Directory.Delete(subFolder, true);
            File.Delete(Path.Combine(root, fileName + ".cshtml"));
        }

        [Fact]
        public async Task Tokens_With_Regular_Expression_Pointing_To_SubFolder()
        {
            var subFolderName = Guid.NewGuid().ToString();
            var pattern1 = "**/*";
            var pattern2 = string.Format("{0}/**/*.cshtml", subFolderName);
            var root = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString();
            var subFolder = Path.Combine(root, subFolderName);
            Directory.CreateDirectory(subFolder);

            int pattern1tokenCount = 0, pattern2tokenCount = 0;
            var provider = new PhysicalFileProvider(root);
            var token1 = provider.Watch(pattern1);
            token1.RegisterChangeCallback(_ => { pattern1tokenCount++; }, null);
            var token2 = provider.Watch(pattern2);
            token2.RegisterChangeCallback(_ => { pattern2tokenCount++; }, null);

            File.WriteAllText(Path.Combine(root, fileName + ".cshtml"), "Content");

            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(1, pattern1tokenCount);
            Assert.Equal(0, pattern2tokenCount);

            token1 = provider.Watch(pattern1);
            token1.RegisterChangeCallback(_ => { pattern1tokenCount++; }, null);
            // Register this token again.
            var token3 = provider.Watch(pattern2);
            token3.RegisterChangeCallback(_ => { pattern2tokenCount++; }, null);
            Assert.Equal(token2, token3);
            File.WriteAllText(Path.Combine(subFolder, fileName + ".cshtml"), "Content");

            await Task.Delay(WaitTimeForTokenToFire);
            Assert.Equal(2, pattern1tokenCount);
            Assert.Equal(2, pattern2tokenCount);

            Directory.Delete(subFolder, true);
            File.Delete(Path.Combine(root, fileName + ".cshtml"));
        }

        [Fact]
        public void Tokens_With_Forward_And_Backward_Slash()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var token1 = provider.Watch("/a/b");
            var token2 = provider.Watch("a/b");
            var token3 = provider.Watch(@"a\b");

            Assert.Equal(token2, token1);
            Assert.Equal(token3, token2);

            Assert.True(token1.ActiveChangeCallbacks);
            Assert.True(token2.ActiveChangeCallbacks);
            Assert.True(token3.ActiveChangeCallbacks);

            Assert.False(token1.HasChanged);
            Assert.False(token2.HasChanged);
            Assert.False(token3.HasChanged);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task Token_Fired_On_Directory_Name_Change()
        {
            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var oldDirectoryName = Guid.NewGuid().ToString();
            var newDirectoryName = Guid.NewGuid().ToString();
            var oldDirectoryFullPath = Path.Combine(Path.GetTempPath(), oldDirectoryName);
            var newDirectoryFullPath = Path.Combine(Path.GetTempPath(), newDirectoryName);

            Directory.CreateDirectory(oldDirectoryFullPath);
            var oldDirectorytoken = provider.Watch("**/" + oldDirectoryName);
            var newDirectorytoken = provider.Watch("**/" + newDirectoryName);
            var oldtokens = new List<IChangeToken>();
            var newtokens = new List<IChangeToken>();

            oldtokens.Add(provider.Watch(Path.Combine("**", oldDirectoryName, "*.txt")));
            newtokens.Add(provider.Watch(Path.Combine("**", newDirectoryName, "*.txt")));

            for (int i = 0; i < 5; i++)
            {
                var fileName = string.Format("test{0}.txt", i);
                File.WriteAllText(Path.Combine(oldDirectoryFullPath, fileName), "test content");
                oldtokens.Add(provider.Watch(Path.Combine("**", oldDirectoryName, fileName)));
                newtokens.Add(provider.Watch(Path.Combine("**", newDirectoryName, fileName)));
            }

            await Task.Delay(2 * 100); // Give it a while before trying rename.
            Directory.Move(oldDirectoryFullPath, newDirectoryFullPath);

            // Wait for tokens to fire.
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.True(oldDirectorytoken.HasChanged);
            Assert.True(newDirectorytoken.HasChanged);
            oldtokens.ForEach(t => Assert.True(t.HasChanged));
            newtokens.ForEach(t => Assert.True(t.HasChanged));

            newDirectorytoken = provider.Watch(newDirectoryName);
            newtokens = new List<IChangeToken>();

            newtokens.Add(provider.Watch(Path.Combine("**", newDirectoryName, "*.txt")));
            for (int i = 0; i < 5; i++)
            {
                var fileName = string.Format("test{0}.txt", i);
                newtokens.Add(provider.Watch(Path.Combine("**", newDirectoryName, fileName)));
            }

            Directory.Delete(newDirectoryFullPath, true);

            // Wait for tokens to fire.
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.True(newDirectorytoken.HasChanged);
            newtokens.ForEach(t => Assert.True(t.HasChanged));
        }

        [Fact]
        public async Task Tokens_NotFired_For_FileNames_Starting_With_Period()
        {
            var root = Path.GetTempPath();
            var fileNameStartingWithPeriod = Path.Combine(root, "." + Guid.NewGuid().ToString());
            File.WriteAllText(fileNameStartingWithPeriod, "Content");

            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var tokenFileNameStartingPeriod = provider.Watch(Path.GetFileName(fileNameStartingWithPeriod));

            Assert.False(tokenFileNameStartingPeriod.HasChanged);

            File.WriteAllText(fileNameStartingWithPeriod, "Updated Contents");

            // Wait for tokens to fire.
            await Task.Delay(WaitTimeForTokenToFire);

            Assert.False(tokenFileNameStartingPeriod.HasChanged);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Hidden and system files only make sense on Windows.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Hidden and system files only make sense on Windows.")]
        public async Task Tokens_NotFired_For_Hidden_And_System_Files()
        {
            var root = Path.GetTempPath();
            var hiddenFileName = Path.Combine(root, Guid.NewGuid().ToString());
            File.WriteAllText(hiddenFileName, "Content");
            var systemFileName = Path.Combine(root, Guid.NewGuid().ToString());
            File.WriteAllText(systemFileName, "Content");
            var fileInfo = new FileInfo(hiddenFileName);
            File.SetAttributes(hiddenFileName, fileInfo.Attributes | FileAttributes.Hidden);
            fileInfo = new FileInfo(systemFileName);
            File.SetAttributes(systemFileName, fileInfo.Attributes | FileAttributes.System);

            var provider = new PhysicalFileProvider(Path.GetTempPath());
            var hiddenFiletoken = provider.Watch(Path.GetFileName(hiddenFileName));
            var systemFiletoken = provider.Watch(Path.GetFileName(systemFileName));

            Assert.False(hiddenFiletoken.HasChanged);
            Assert.False(systemFiletoken.HasChanged);

            File.AppendAllText(hiddenFileName, "Appending text");
            File.AppendAllText(systemFileName, "Appending text");

            // Wait for tokens to fire.
            await Task.Delay(WaitTimeForTokenToFire);

            Assert.False(hiddenFiletoken.HasChanged);
            Assert.False(systemFiletoken.HasChanged);
        }

    }
}
