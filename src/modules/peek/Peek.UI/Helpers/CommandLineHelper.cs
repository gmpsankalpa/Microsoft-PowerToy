// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.PowerToys.Settings.UI.Library;
using Peek.Common.Models;
using Peek.UI.Native;

namespace Peek.UI.Helpers
{
    public class CommandLineHelper
    {
        private const string FileName = "last-command-line.json";

        public static bool SerializeCommandLine()
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray(); // Skip the first argument, which is the executable path
            if (args.Length == 0)
            {
                return false;
            }

            var commandLine = new CommandLine
            {
                CurrentDirectory = Environment.CurrentDirectory,
                Args = args, // Skip the first argument, which is the executable path
                Toggle = args.Contains("--toggle"), // TODO fix
            };

            new SettingsUtils().SaveSettings(commandLine.ToJsonString(), PeekSettings.ModuleName, FileName);
            return true;
        }

        public static IEnumerable<IFileSystemItem> GetItemsFromCommandLine()
        {
            var commandLine = new SettingsUtils().GetSettingsOrDefault<CommandLine>(PeekSettings.ModuleName, FileName);

            foreach (var p in commandLine.Args)
            {
                var path = p;

                if (!Path.IsPathFullyQualified(path) && commandLine.CurrentDirectory != null)
                {
                    path = Path.Join(commandLine.CurrentDirectory, path);
                }

                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);

                    // This would be nice once implemented: https://github.com/dotnet/runtime/issues/14321
                    var displayName = null as string;
                    var shFileInfo = default(SHFILEINFO);
                    if (NativeMethods.SHGetFileInfo(fileInfo.FullName, 0, ref shFileInfo, (uint)Marshal.SizeOf(shFileInfo), NativeMethods.SHGFI_DISPLAYNAME) != IntPtr.Zero)
                    {
                        displayName = shFileInfo.SzDisplayName;
                    }

                    yield return new FileItem(fileInfo.FullName, displayName ?? fileInfo.Name);
                }
                else if (Directory.Exists(path))
                {
                    var directoryInfo = new DirectoryInfo(path);

                    // This would be nice once implemented: https://github.com/dotnet/runtime/issues/14321
                    var displayName = null as string;
                    var shFileInfo = default(SHFILEINFO);
                    if (NativeMethods.SHGetFileInfo(directoryInfo.FullName, 0, ref shFileInfo, (uint)Marshal.SizeOf(shFileInfo), NativeMethods.SHGFI_DISPLAYNAME) != IntPtr.Zero)
                    {
                        displayName = shFileInfo.SzDisplayName;
                    }

                    yield return new FolderItem(directoryInfo.FullName, displayName ?? directoryInfo.Name);
                }
            }
        }

        public static IEnumerable<IFileSystemItem> GetItems(string path)
        {
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);

                // This would be nice once implemented: https://github.com/dotnet/runtime/issues/14321
                var displayName = null as string;
                var shFileInfo = default(SHFILEINFO);
                if (NativeMethods.SHGetFileInfo(fileInfo.FullName, 0, ref shFileInfo, (uint)Marshal.SizeOf(shFileInfo), NativeMethods.SHGFI_DISPLAYNAME) != IntPtr.Zero)
                {
                    displayName = shFileInfo.SzDisplayName;
                }

                yield return new FileItem(fileInfo.FullName, displayName ?? fileInfo.Name);
            }
            else if (Directory.Exists(path))
            {
                var directoryInfo = new DirectoryInfo(path);

                // This would be nice once implemented: https://github.com/dotnet/runtime/issues/14321
                var displayName = null as string;
                var shFileInfo = default(SHFILEINFO);
                if (NativeMethods.SHGetFileInfo(directoryInfo.FullName, 0, ref shFileInfo, (uint)Marshal.SizeOf(shFileInfo), NativeMethods.SHGFI_DISPLAYNAME) != IntPtr.Zero)
                {
                    displayName = shFileInfo.SzDisplayName;
                }

                yield return new FolderItem(directoryInfo.FullName, displayName ?? directoryInfo.Name);
            }
        }
    }
}
