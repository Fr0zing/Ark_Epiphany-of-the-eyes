#include <Windows.h>
#include <dia2.h>
#include <algorithm>
#include <cwctype>
#include <iostream>
#include <string>
#include <vector>

static std::wstring NameOf(IDiaSymbol* symbol)
{
    BSTR value{};
    if (!symbol || FAILED(symbol->get_name(&value)) || !value) return L"";
    std::wstring result(value, SysStringLen(value));
    SysFreeString(value);
    return result;
}

static bool Interesting(const std::wstring& value)
{
    std::wstring lower = value;
    std::transform(lower.begin(), lower.end(), lower.begin(), towlower);
	return lower.find(L"name") != std::wstring::npos ||
		lower.find(L"tribe") != std::wstring::npos ||
		lower.find(L"team") != std::wstring::npos ||
		lower.find(L"playerstate") != std::wstring::npos ||
		lower.find(L"owner") != std::wstring::npos ||
		lower.find(L"dead") != std::wstring::npos ||
		lower.find(L"death") != std::wstring::npos ||
		lower.find(L"alive") != std::wstring::npos ||
		lower.find(L"sleep") != std::wstring::npos ||
		lower.find(L"ragdoll") != std::wstring::npos ||
		lower.find(L"health") != std::wstring::npos ||
		lower.find(L"weapon") != std::wstring::npos ||
		lower.find(L"equip") != std::wstring::npos ||
		lower.find(L"armor") != std::wstring::npos ||
		lower.find(L"inventory") != std::wstring::npos ||
		lower.find(L"mesh") != std::wstring::npos ||
		lower.find(L"bone") != std::wstring::npos ||
		lower.find(L"ref") != std::wstring::npos ||
		lower.find(L"item") != std::wstring::npos ||
		lower.find(L"transform") != std::wstring::npos ||
		lower.find(L"space") != std::wstring::npos ||
		lower.find(L"cached") != std::wstring::npos;
}

static void DumpType(IDiaSymbol* type, int depth = 0)
{
    if (!type || depth > 8) return;
    ULONGLONG length{};
    type->get_length(&length);
    std::wcout << std::wstring(depth * 2, L' ') << L"TYPE " << NameOf(type)
        << L" size=0x" << std::hex << length << std::dec << L"\n";

    IDiaEnumSymbols* fields{};
    if (SUCCEEDED(type->findChildren(SymTagData, nullptr, nsNone, &fields)) && fields)
    {
        IDiaSymbol* field{};
        ULONG fetched{};
        while (fields->Next(1, &field, &fetched) == S_OK)
        {
            const std::wstring name = NameOf(field);
			DWORD kind{}, location{};
			LONG offset{};
			DWORD bitPosition{};
            field->get_dataKind(&kind);
            field->get_locationType(&location);
            if (Interesting(name) && SUCCEEDED(field->get_offset(&offset)))
            {
                IDiaSymbol* fieldType{};
                ULONGLONG fieldLength{};
                if (SUCCEEDED(field->get_type(&fieldType)) && fieldType)
                {
                    fieldType->get_length(&fieldLength);
                    fieldType->Release();
                }
				std::wcout << std::wstring(depth * 2 + 2, L' ') << name
					<< L" offset=0x" << std::hex << offset
					<< L" size=0x" << fieldLength << std::dec
					<< L" kind=" << kind << L" loc=" << location;
				if (SUCCEEDED(field->get_bitPosition(&bitPosition)))
					std::wcout << L" bit=" << bitPosition;
				std::wcout << L"\n";
            }
            field->Release();
        }
        fields->Release();
    }

    IDiaEnumSymbols* bases{};
    if (SUCCEEDED(type->findChildren(SymTagBaseClass, nullptr, nsNone, &bases)) && bases)
    {
        IDiaSymbol* base{};
        ULONG fetched{};
        while (bases->Next(1, &base, &fetched) == S_OK)
        {
            IDiaSymbol* baseType{};
            if (SUCCEEDED(base->get_type(&baseType)) && baseType)
            {
                DumpType(baseType, depth + 1);
                baseType->Release();
            }
            base->Release();
        }
        bases->Release();
    }
}

static void DumpEquipmentEnums(IDiaSymbol* global)
{
    IDiaEnumSymbols* enums{};
    if (FAILED(global->findChildren(SymTagEnum, nullptr, nsNone, &enums)) || !enums) return;
    IDiaSymbol* type{};
    ULONG fetched{};
    while (enums->Next(1, &type, &fetched) == S_OK)
    {
        const std::wstring typeName = NameOf(type);
        std::wstring lower = typeName;
        std::transform(lower.begin(), lower.end(), lower.begin(), towlower);
        if (lower.find(L"equipmenttype") != std::wstring::npos)
        {
            std::wcout << L"ENUM " << typeName << L"\n";
            IDiaEnumSymbols* values{};
            if (SUCCEEDED(type->findChildren(SymTagData, nullptr, nsNone, &values)) && values)
            {
                IDiaSymbol* value{};
                ULONG valueFetched{};
                while (values->Next(1, &value, &valueFetched) == S_OK)
                {
                    VARIANT constant;
                    VariantInit(&constant);
                    if (SUCCEEDED(value->get_value(&constant)))
                    {
                        if (SUCCEEDED(VariantChangeType(&constant, &constant, 0, VT_I8)))
                            std::wcout << L"  " << NameOf(value) << L"=" << constant.llVal << L"\n";
                    }
                    VariantClear(&constant);
                    value->Release();
                }
                values->Release();
            }
        }
        type->Release();
    }
    enums->Release();
}

int wmain(int argc, wchar_t** argv)
{
	if (argc < 3) return 2;
	if (FAILED(CoInitializeEx(nullptr, COINIT_MULTITHREADED))) return 3;
	IDiaDataSource* source{};
	HMODULE diaModule = LoadLibraryW(argv[2]);
	using DllGetClassObjectFn = HRESULT(STDAPICALLTYPE*)(REFCLSID, REFIID, void**);
	auto getClassObject = diaModule ? reinterpret_cast<DllGetClassObjectFn>(GetProcAddress(diaModule, "DllGetClassObject")) : nullptr;
	IClassFactory* factory{};
	HRESULT hr = getClassObject ? getClassObject(__uuidof(DiaSource), IID_IClassFactory,
		reinterpret_cast<void**>(&factory)) : HRESULT_FROM_WIN32(GetLastError());
	if (SUCCEEDED(hr))
	{
		hr = factory->CreateInstance(nullptr, __uuidof(IDiaDataSource), reinterpret_cast<void**>(&source));
		factory->Release();
	}
	if (FAILED(hr))
	{
		std::wcerr << L"DIA creation failed: 0x" << std::hex << hr << L"\n";
		if (diaModule) FreeLibrary(diaModule);
		CoUninitialize();
		return 4;
    }
    hr = source->loadDataFromPdb(argv[1]);
    IDiaSession* session{};
    if (SUCCEEDED(hr)) hr = source->openSession(&session);
    IDiaSymbol* global{};
    if (SUCCEEDED(hr)) hr = session->get_globalScope(&global);
    if (FAILED(hr))
    {
        std::wcerr << L"PDB load failed: 0x" << std::hex << hr << L"\n";
        if (session) session->Release();
	source->Release();
	FreeLibrary(diaModule);
	CoUninitialize();
        return 5;
    }

    const wchar_t* requested[] = {
		L"AShooterCharacter", L"AShooterPlayerState", L"APrimalCharacter",
		L"APrimalDinoCharacter", L"APrimalStructure", L"APlayerState",
		L"UPrimalCharacterStatusComponent", L"UPrimalInventoryComponent",
		L"AShooterWeapon", L"UPrimalItem", L"USkeletalMeshComponent",
		L"USkeletalMesh", L"FReferenceSkeleton", L"FMeshBoneInfo"
    };
    for (const auto name : requested)
    {
        IDiaEnumSymbols* matches{};
        if (SUCCEEDED(global->findChildren(SymTagUDT, name, nsCaseInsensitive, &matches)) && matches)
        {
            IDiaSymbol* type{};
            ULONG fetched{};
            while (matches->Next(1, &type, &fetched) == S_OK)
            {
                DumpType(type);
                type->Release();
            }
            matches->Release();
        }
    }
    DumpEquipmentEnums(global);
    global->Release();
    session->Release();
    source->Release();
    CoUninitialize();
    return 0;
}
