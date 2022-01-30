#pragma once
#include "Window.h"
#include "Drawing.h"
#include "Settings.h"


class IZoneTitleBar
{
public:
    virtual ~IZoneTitleBar() = default;
    virtual void UpdateZoneWindows(std::vector<HWND> zoneWindows) = 0;
    virtual void ReadjustPos() = 0;
    virtual int GetHeight() const = 0;
};

std::unique_ptr<IZoneTitleBar> MakeZoneTitleBar(ZoneTitleBarStyle style, HINSTANCE hinstance, FancyZonesUtils::Rect zone);