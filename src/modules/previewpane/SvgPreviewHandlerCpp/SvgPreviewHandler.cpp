#include "pch.h"
#include "SvgPreviewHandler.h"

#include <assert.h>
#include <exception>
#include <Shlwapi.h>
#include <string>
#include <vector>
#include <wrl.h>

#include <WebView2.h>
#include <wil/com.h>

using namespace Microsoft::WRL;

extern HINSTANCE g_hInst;
extern long g_cDllRef;

namespace
{

const std::vector<std::wstring> BlockedElementsName = { L"script", L"image", L"feimage" };

inline int RECTWIDTH(const RECT& rc)
{
    return (rc.right - rc.left);
}

inline int RECTHEIGHT(const RECT& rc)
{
    return (rc.bottom - rc.top);
}
}

SvgPreviewHandler::SvgPreviewHandler() :
    m_cRef(1), m_pStream(NULL), m_hwndParent(NULL), m_rcParent(), m_hwndPreview(NULL), m_punkSite(NULL)
{
    InterlockedIncrement(&g_cDllRef);
}

SvgPreviewHandler::~SvgPreviewHandler()
{
    if (m_hwndParent)
    {
        DestroyWindow(m_hwndPreview);
        m_hwndPreview = NULL;
    }
    if (m_punkSite)
    {
        m_punkSite->Release();
        m_punkSite = NULL;
    }
    if (m_pStream)
    {
        m_pStream->Release();
        m_pStream = NULL;
    }

    InterlockedDecrement(&g_cDllRef);
}

#pragma region IUnknown

IFACEMETHODIMP SvgPreviewHandler::QueryInterface(REFIID riid, void **ppv)
{
    static const QITAB qit[] = {
        QITABENT(SvgPreviewHandler, IPreviewHandler),
        QITABENT(SvgPreviewHandler, IInitializeWithStream),
        QITABENT(SvgPreviewHandler, IPreviewHandlerVisuals),
        QITABENT(SvgPreviewHandler, IOleWindow),
        QITABENT(SvgPreviewHandler, IObjectWithSite),
        { 0 },
    };
    return QISearch(this, qit, riid, ppv);
}

IFACEMETHODIMP_(ULONG) SvgPreviewHandler::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}

IFACEMETHODIMP_(ULONG) SvgPreviewHandler::Release()
{
    ULONG cRef = InterlockedDecrement(&m_cRef);
    if (0 == cRef)
    {
        delete this;
    }

    return cRef;
}

#pragma endregion

#pragma region IInitializeWithStream

IFACEMETHODIMP SvgPreviewHandler::Initialize(IStream *pStream, DWORD grfMode)
{
    HRESULT hr = E_INVALIDARG;
    if (pStream)
    {
        if (m_pStream)
        {
            m_pStream->Release();
            m_pStream = NULL;
        }

        m_pStream = pStream;
        m_pStream->AddRef();
        hr = S_OK;
    }
    return hr;
}

#pragma endregion

#pragma region IPreviewHandler

IFACEMETHODIMP SvgPreviewHandler::SetWindow(HWND hwnd, const RECT *prc)
{
    if (hwnd && prc)
    {
        m_hwndParent = hwnd;
        m_rcParent = *prc;

        if (m_hwndPreview)
        {
            SetParent(m_hwndPreview, m_hwndParent);
            SetWindowPos(m_hwndPreview, NULL, m_rcParent.left, m_rcParent.top,
                RECTWIDTH(m_rcParent), RECTHEIGHT(m_rcParent),
                SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }
    return S_OK;
}

IFACEMETHODIMP SvgPreviewHandler::SetFocus()
{
    HRESULT hr = S_FALSE;
    if (m_hwndPreview)
    {
        ::SetFocus(m_hwndPreview);
        hr = S_OK;
    }
    return hr;
}

IFACEMETHODIMP SvgPreviewHandler::QueryFocus(HWND *phwnd)
{
    HRESULT hr = E_INVALIDARG;
    if (phwnd)
    {
        *phwnd = ::GetFocus();
        if (*phwnd)
        {
            hr = S_OK;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
    }
    return hr;
}

IFACEMETHODIMP SvgPreviewHandler::TranslateAccelerator(MSG *pmsg)
{
    HRESULT hr = S_FALSE;
    IPreviewHandlerFrame* pFrame = NULL;
    if (m_punkSite && SUCCEEDED(m_punkSite->QueryInterface(&pFrame)))
    {
        hr = pFrame->TranslateAccelerator(pmsg);

        pFrame->Release();
    }
    return hr;
}

IFACEMETHODIMP SvgPreviewHandler::SetRect(const RECT *prc)
{
    HRESULT hr = E_INVALIDARG;
    if (prc != NULL)
    {
        m_rcParent = *prc;
        if (m_hwndPreview)
        {
            SetWindowPos(m_hwndPreview, NULL, m_rcParent.left, m_rcParent.top,
                RECTWIDTH(m_rcParent), RECTHEIGHT(m_rcParent),
                SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
        }
        hr = S_OK;
    }
    return hr;
}

IFACEMETHODIMP SvgPreviewHandler::DoPreview()
{
    HRESULT hr = E_FAIL;
    //CleanupWebView2UserDataFolder();

    std::string svgData;
    std::wstring wsvgData;
    bool blocked = false;

    try
    {
        char buffer[4096];
        ULONG asd;
        while (true)
        {
            auto result = m_pStream->Read(buffer, 4096, &asd);
            svgData.append(buffer, asd);
            if (result == S_FALSE)
            {
                break;
            }
        }

        wsvgData = std::wstring{ winrt::to_hstring(svgData) };
        blocked = CheckBlockedElements(wsvgData);
    }
    catch (std::exception ex)
    {
        //PreviewError(ex, dataSource);
        //return;
    }

    //try
    //{
    //    // Fixes #17527 - Inkscape v1.1 swapped order of default and svg namespaces in svg file (default first, svg after).
    //    // That resulted in parser being unable to parse it correctly and instead of svg, text was previewed.
    //    // MS Edge and Firefox also couldn't preview svg files with mentioned order of namespaces definitions.
    //    svgData = SvgPreviewHandlerHelper.SwapNamespaces(svgData);
    //    svgData = SvgPreviewHandlerHelper.AddStyleSVG(svgData);
    //}
    //catch (Exception ex)
    //{
    //    PowerToysTelemetry.Log.WriteEvent(new SvgFilePreviewError{ Message = ex.Message });
    //}

    //try
    //{
    //    _infoBarAdded = false;

    //    // Add a info bar on top of the Preview if any blocked element is present.
    //    if (blocked)
    //    {
    //        _infoBarAdded = true;
    //        AddTextBoxControl(Properties.Resource.BlockedElementInfoText);
    //    }

    AddWebViewControl(wsvgData);
    //    Resize += FormResized;
    //    base.DoPreview(dataSource);
    //    PowerToysTelemetry.Log.WriteEvent(new SvgFilePreviewed());
    //}
    //catch (Exception ex)
    //{
    //    PreviewError(ex, dataSource);
    //}

    return S_OK;
}

IFACEMETHODIMP SvgPreviewHandler::Unload()
{
    if (m_pStream)
    {
        m_pStream->Release();
        m_pStream = NULL;
    }

    if (m_hwndPreview)
    {
        DestroyWindow(m_hwndPreview);
        m_hwndPreview = NULL;
    }
    return S_OK;
}

#pragma endregion

#pragma region IPreviewHandlerVisuals

IFACEMETHODIMP SvgPreviewHandler::SetBackgroundColor(COLORREF color)
{
    return S_OK;
}

IFACEMETHODIMP SvgPreviewHandler::SetFont(const LOGFONTW *plf)
{
    return S_OK;
}

IFACEMETHODIMP SvgPreviewHandler::SetTextColor(COLORREF color)
{
    return S_OK;
}

#pragma endregion

#pragma region IOleWindow

IFACEMETHODIMP SvgPreviewHandler::GetWindow(HWND *phwnd)
{
    HRESULT hr = E_INVALIDARG;
    if (phwnd)
    {
        *phwnd = m_hwndParent;
        hr = S_OK;
    }
    return hr;
}

IFACEMETHODIMP SvgPreviewHandler::ContextSensitiveHelp(BOOL fEnterMode)
{
    return E_NOTIMPL;
}

#pragma endregion

#pragma region IObjectWithSite

IFACEMETHODIMP SvgPreviewHandler::SetSite(IUnknown *punkSite)
{
    if (m_punkSite)
    {
        m_punkSite->Release();
        m_punkSite = NULL;
    }
    return punkSite ? punkSite->QueryInterface(&m_punkSite) : S_OK;
}

IFACEMETHODIMP SvgPreviewHandler::GetSite(REFIID riid, void **ppv)
{
    *ppv = NULL;
    return m_punkSite ? m_punkSite->QueryInterface(riid, ppv) : E_FAIL;
}

#pragma endregion

#pragma region Helper Functions

BOOL SvgPreviewHandler::CheckBlockedElements(std::wstring svgData)
{
    bool foundBlockedElement = false;
    if (svgData.empty())
    {
        return foundBlockedElement;
    }

    // Check if any of the blocked element is present. If failed to parse or iterate over Svg return default false.
    // No need to throw because all the external content and script are blocked on the Web Browser Control itself.
    try
    {
        winrt::Windows::Data::Xml::Dom::XmlDocument doc;
        doc.LoadXml(svgData);

        for (const auto blockedElem : BlockedElementsName)
        {
            winrt::Windows::Data::Xml::Dom::XmlNodeList elems = doc.GetElementsByTagName(blockedElem);
            if (elems.Size() > 0)
            {
                foundBlockedElement = true;
                break;
                MessageBox(
                    NULL,
                    blockedElem.c_str(),
                    (LPCWSTR)L"Account Details",
                    MB_ICONWARNING | MB_CANCELTRYCONTINUE | MB_DEFBUTTON2);
            }
        }
    }
    catch (std::exception)
    {
    }

    return foundBlockedElement;
}

void SvgPreviewHandler::AddWebViewControl(std::wstring svgData)
{

    CreateCoreWebView2EnvironmentWithOptions(nullptr, L"C:\\Users\\stefa\\AppData\\LocalLow\\Microsoft\\PowerToys\\aaaa", nullptr,
        Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>([this, svgData](HRESULT result, ICoreWebView2Environment* env) -> HRESULT {
            // Create a CoreWebView2Controller and get the associated CoreWebView2 whose parent is the main window hWnd
            env->CreateCoreWebView2Controller(m_hwndParent, Callback<ICoreWebView2CreateCoreWebView2ControllerCompletedHandler>([this, svgData](HRESULT result, ICoreWebView2Controller* controller) -> HRESULT {
                if (controller != nullptr)
                {
                    webviewController = controller;
                    webviewController->get_CoreWebView2(&webviewWindow);
                }

                // Add a few settings for the webview
                // The demo step is redundant since the values are the default settings
                ICoreWebView2Settings* Settings;
                webviewWindow->get_Settings(&Settings);
                //Settings->put_IsScriptEnabled(TRUE);
                //Settings->put_AreDefaultScriptDialogsEnabled(TRUE);
                //Settings->put_IsWebMessageEnabled(TRUE);

                // Resize WebView to fit the bounds of the parent window
                RECT bounds;
                GetClientRect(m_hwndParent, &bounds);
                webviewController->put_Bounds(bounds);

                // Schedule an async task to navigate to Bing
                //webviewWindow->Navigate(L"https://www.bing.com");

                webviewWindow->NavigateToString(svgData.c_str());

                return S_OK;
            }).Get());
        return S_OK;
    }).Get());
}

#pragma endregion