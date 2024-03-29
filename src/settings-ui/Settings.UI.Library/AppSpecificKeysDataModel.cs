﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.PowerToys.Settings.UI.Library
{
    public class AppSpecificKeysDataModel : KeysDataModel
    {
        [JsonPropertyName("targetApp")]
        public string TargetApp { get; set; }

        public new List<string> GetMappedOriginalKeys()
        {
            return base.GetMappedOriginalKeys();
        }

        public new List<string> GetMappedNewRemapKeys()
        {
            return base.GetMappedNewRemapKeys();
        }

        public bool Compare(AppSpecificKeysDataModel arg)
        {
            ArgumentNullException.ThrowIfNull(arg);

            // Using Ordinal comparison for internal text
            return string.Equals(OriginalKeys, arg.OriginalKeys, StringComparison.Ordinal) &&
                   string.Equals(NewRemapKeys, arg.NewRemapKeys, StringComparison.Ordinal) &&
                   string.Equals(NewRemapString, arg.NewRemapString, StringComparison.Ordinal) &&
                   string.Equals(TargetApp, arg.TargetApp, StringComparison.Ordinal);
        }
    }
}
