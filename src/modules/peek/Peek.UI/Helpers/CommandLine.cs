// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using Microsoft.PowerToys.Settings.UI.Library;
using Microsoft.PowerToys.Settings.UI.Library.Interfaces;

namespace Peek.UI.Helpers
{
    public class CommandLine : ISettingsConfig
    {
        public string? CurrentDirectory { get; set; }

        public string[] Args { get; set; }

        public CommandLine()
        {
            Args = Array.Empty<string>();
        }

        public string ToJsonString()
        {
            return JsonSerializer.Serialize(this);
        }

        public string GetModuleName()
        {
            return PeekSettings.ModuleName;
        }

        public bool UpgradeSettingsConfiguration()
        {
            return false;
        }
    }
}
