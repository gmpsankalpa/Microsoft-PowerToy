// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "ClassFactory.h"
//#include "../powerpreview/CLSID.h"

// TODO(stefan) remove
#include "Reg.h"
// {78A573CA-297E-4D9F-A5FC-7F6E5EEA6FC9}
// When you write your own handler, you must create a new CLSID by using the
// "Create GUID" tool in the Tools menu, and specify the CLSID value here.
const CLSID CLSID_RecipePreviewHandler = { 0x78A573CA, 0x297E, 0x4D9F, { 0xA5, 0xFC, 0x7F, 0x6E, 0x5E, 0xEA, 0x6F, 0xC9 } };

// {2992DE27-3526-48C5-B765-E55278ECBE9D}
// When you write your own handler, you must create a new CLSID by using the
// "Create GUID" tool in the Tools menu, and specify the CLSID value here.
const GUID APPID_RecipePreviewHandler = { 0x2992DE27, 0x3526, 0x48C5, { 0xB7, 0x65, 0xE5, 0x52, 0x78, 0xEC, 0xBE, 0x9D } };

// end remove

HINSTANCE   g_hInst     = NULL;
long        g_cDllRef   = 0;

#pragma unmanaged
BOOL APIENTRY DllMain(HMODULE hModule, DWORD dwReason, LPVOID lpReserved)
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        g_hInst = hModule;
        DisableThreadLibraryCalls(hModule);
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

STDAPI DllGetClassObject(REFCLSID rclsid, REFIID riid, void **ppv)
{
    HRESULT hr = CLASS_E_CLASSNOTAVAILABLE;

    if (IsEqualCLSID(CLSID_RecipePreviewHandler, rclsid))
    {
        hr = E_OUTOFMEMORY;

        ClassFactory* pClassFactory = new ClassFactory();
        if (pClassFactory)
        {
            hr = pClassFactory->QueryInterface(riid, ppv);
            pClassFactory->Release();
        }
    }

    return hr;
}

STDAPI DllCanUnloadNow(void)
{
    return g_cDllRef > 0 ? S_FALSE : S_OK;
}


// TODO(stefan): Remove in the end and reuse old register logic

//
//   FUNCTION: DllRegisterServer
//
//   PURPOSE: Register the COM server and the context menu handler.
//
STDAPI DllRegisterServer(void)
{
    HRESULT hr;

    wchar_t szModule[MAX_PATH];
    if (GetModuleFileName(g_hInst, szModule, ARRAYSIZE(szModule)) == 0)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        return hr;
    }

    // Register the component.
    hr = RegisterInprocServer(szModule, CLSID_RecipePreviewHandler, L"CppShellExtPreviewHandler.RecipePreviewHandler Class", L"Apartment", APPID_RecipePreviewHandler);
    if (SUCCEEDED(hr))
    {
        // Register the preview handler. The preview handler is associated
        // with the .recipe file class.
        hr = RegisterShellExtPreviewHandler(L".recipe", CLSID_RecipePreviewHandler, L"RecipePreviewHandler");
    }

    return hr;
}

//
//   FUNCTION: DllUnregisterServer
//
//   PURPOSE: Unregister the COM server and the context menu handler.
//
STDAPI DllUnregisterServer(void)
{
    HRESULT hr = S_OK;

    wchar_t szModule[MAX_PATH];
    if (GetModuleFileName(g_hInst, szModule, ARRAYSIZE(szModule)) == 0)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        return hr;
    }

    // Unregister the component.
    hr = UnregisterInprocServer(CLSID_RecipePreviewHandler,
                                APPID_RecipePreviewHandler);
    if (SUCCEEDED(hr))
    {
        // Unregister the context menu handler.
        hr = UnregisterShellExtPreviewHandler(L".recipe",
                                              CLSID_RecipePreviewHandler);
    }

    return hr;
}