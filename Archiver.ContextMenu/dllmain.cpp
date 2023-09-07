
#pragma comment( linker, "/subsystem:windows /entry:mainCRTStartup" )

#include "pch.h"
#include <wrl/module.h>
#include <wrl/implements.h>
#include <wrl/client.h>
#include <shobjidl_core.h>
#include <wil/resource.h>
#include <string>
#include <vector>
#include <sstream>

using namespace Microsoft::WRL;

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

HMODULE GetSelfModuleHandle() {
    MEMORY_BASIC_INFORMATION mbi;
    return ((::VirtualQuery(GetSelfModuleHandle, &mbi, sizeof(mbi)) != 0) ? (HMODULE)mbi.AllocationBase : NULL);
}

int GetDllPath(LPWSTR current) {
    GetModuleFileName(GetSelfModuleHandle(), current, 1024);
    return 0;
}

char* LPWString2CharPtr(LPWSTR lpwszStrIn) {
    LPSTR pszOut = NULL;
    try {
        if (lpwszStrIn != NULL) {
            int nInputStrLen = wcslen(lpwszStrIn);

            // Double NULL Termination  
            int nOutputStrLen = WideCharToMultiByte(CP_ACP, 0, lpwszStrIn, nInputStrLen, NULL, 0, 0, 0) + 2;
            pszOut = new char[nOutputStrLen];

            if (pszOut) {
                memset(pszOut, 0x00, nOutputStrLen);
                WideCharToMultiByte(CP_ACP, 0, lpwszStrIn, nInputStrLen, pszOut, nOutputStrLen, 0, 0);
            }
        }
    }
    catch (std::exception e) {
    }

    return pszOut;
}

class ExplorerCommandBase : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IExplorerCommand, IObjectWithSite>
{
public:
    virtual const wchar_t* Title() = 0;
    virtual const EXPCMDFLAGS Flags() { return ECF_DEFAULT; }
    virtual const EXPCMDSTATE State(_In_opt_ IShellItemArray* selection) { return ECS_ENABLED; }

    // IExplorerCommand
    IFACEMETHODIMP GetTitle(_In_opt_ IShellItemArray* items, _Outptr_result_nullonfailure_ PWSTR* name)
    {
        *name = nullptr;
        auto title = wil::make_cotaskmem_string_nothrow(Title());
        RETURN_IF_NULL_ALLOC(title);
        *name = title.release();
        return S_OK;
    }

    IFACEMETHODIMP GetIcon(_In_opt_ IShellItemArray*, _Outptr_result_nullonfailure_ PWSTR* icon) { *icon = nullptr; return E_NOTIMPL; }
    IFACEMETHODIMP GetToolTip(_In_opt_ IShellItemArray*, _Outptr_result_nullonfailure_ PWSTR* infoTip) { *infoTip = nullptr; return E_NOTIMPL; }
    IFACEMETHODIMP GetCanonicalName(_Out_ GUID* guidCommandName) { *guidCommandName = GUID_NULL;  return S_OK; }

    IFACEMETHODIMP GetState(_In_opt_ IShellItemArray* selection, _In_ BOOL okToBeSlow, _Out_ EXPCMDSTATE* cmdState)
    {
        *cmdState = State(selection);
        return S_OK;
    }

    IFACEMETHODIMP Invoke(_In_opt_ IShellItemArray* selection, _In_opt_ IBindCtx*) noexcept try
    {
        HWND parent = nullptr;
        if (m_site)
        {
            ComPtr<IOleWindow> oleWindow;
            RETURN_IF_FAILED(m_site.As(&oleWindow));
            RETURN_IF_FAILED(oleWindow->GetWindow(&parent));
        }

        std::wostringstream title;
        title << Title();

        if (selection)
        {
            DWORD count;
            RETURN_IF_FAILED(selection->GetCount(&count));
            title << L" (" << count << L" selected items)";

            IShellItem* shellItem = NULL;
            wchar_t _temp[1024] = { '\0' };
            LPWSTR dllpath = _temp;
            GetDllPath(dllpath);
            char* currEnc = LPWString2CharPtr(dllpath);
            int lengthOfDll = strlen(currEnc);
            currEnc[lengthOfDll - 24] = '\0';

            char basePath[1024] = { '\0' };
            char menuAdd[1024] = { '\0' };
            char exePath[1024] = { '\0' };
            strcpy(basePath, currEnc);
            strcpy(menuAdd, currEnc);
            strcpy(exePath, currEnc);
            strcat(menuAdd, "context-dirs");
            strcat(exePath, "archiver.exe -context-menu-compress");

            FILE* f = fopen(menuAdd, "w,ccs=UTF-8");

            for (DWORD i = 0; i < count; i++) {
                RETURN_IF_FAILED(selection->GetItemAt(i, &shellItem));
                LPWSTR pszFilePath = NULL;
                RETURN_IF_FAILED(shellItem->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING, &pszFilePath));
                fwprintf(f, (LPWSTR)pszFilePath);
                wchar_t linebreak[2] = { '\n', '\0' };
                fwprintf(f, linebreak);
            }

            fclose(f);
            WinExec(exePath, SW_HIDE);
        }
        else
        {
            title << L"(no selected items)";
        }

        return S_OK;
    }

    CATCH_RETURN();

    IFACEMETHODIMP GetFlags(_Out_ EXPCMDFLAGS* flags) { *flags = Flags(); return S_OK; }
    IFACEMETHODIMP EnumSubCommands(_COM_Outptr_ IEnumExplorerCommand** enumCommands) { *enumCommands = nullptr; return E_NOTIMPL; }

    // IObjectWithSite
    IFACEMETHODIMP SetSite(_In_ IUnknown* site) noexcept { m_site = site; return S_OK; }
    IFACEMETHODIMP GetSite(_In_ REFIID riid, _COM_Outptr_ void** site) noexcept { return m_site.CopyTo(riid, site); }

protected:
    ComPtr<IUnknown> m_site;
};

class __declspec(uuid("4CDEA877-ABBA-4618-89A0-892D09F83543")) ExplorerCompressFolderHandler final : 
    public ExplorerCommandBase
{
public:
    const wchar_t* Title() override { return L"Compress current folder"; }
};

class __declspec(uuid("4E68C2EC-B7B3-41A8-A11A-DFD30316FFD1")) ExplorerCompressDHandler final : 
    public ExplorerCommandBase
{
public:
    const wchar_t* Title() override { return L"Compress"; }
};

class __declspec(uuid("73EF65E4-A72D-4E56-B078-E5C4359FF981")) ExplorerCompressHandler final :
    public ExplorerCommandBase
{
public:
    const wchar_t* Title() override { return L"Compress"; }
};

CoCreatableClass(ExplorerCompressFolderHandler)
CoCreatableClass(ExplorerCompressHandler)
CoCreatableClass(ExplorerCompressDHandler)

CoCreatableClassWrlCreatorMapInclude(ExplorerCompressFolderHandler)
CoCreatableClassWrlCreatorMapInclude(ExplorerCompressHandler)
CoCreatableClassWrlCreatorMapInclude(ExplorerCompressDHandler)

STDAPI DllGetActivationFactory (_In_ HSTRING activatableClassId, 
    _COM_Outptr_ IActivationFactory** factory)
{
    return Module<ModuleType::InProc>::GetModule().GetActivationFactory(activatableClassId, factory);
}

STDAPI DllCanUnloadNow()
{
    return Module<InProc>::GetModule().GetObjectCount() == 0 ? S_OK : S_FALSE;
}

STDAPI DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _COM_Outptr_ void** instance)
{
    return Module<InProc>::GetModule().GetClassObject(rclsid, riid, instance);
}