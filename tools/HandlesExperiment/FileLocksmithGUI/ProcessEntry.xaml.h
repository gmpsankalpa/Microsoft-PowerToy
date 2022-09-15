﻿#pragma once

#include "winrt/Microsoft.UI.Xaml.h"
#include "winrt/Microsoft.UI.Xaml.Markup.h"
#include "winrt/Microsoft.UI.Xaml.Controls.Primitives.h"
#include "ProcessEntry.g.h"

namespace winrt::FileLocksmithGUI::implementation
{
    struct ProcessEntry : ProcessEntryT<ProcessEntry>
    {
        ProcessEntry(const winrt::hstring& process, int pid);
    };
}

namespace winrt::FileLocksmithGUI::factory_implementation
{
    struct ProcessEntry : ProcessEntryT<ProcessEntry, implementation::ProcessEntry>
    {
    };
}
