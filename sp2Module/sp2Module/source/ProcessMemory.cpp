#include "ProcessMemory.hpp"


u64 ProcessMemory::ModuleAddr(u64 addr, int moduleIndex)
{
    MemoryInfo mi;
    u32 pi;
    u64 curaddr = 0;
    int moduleCount = 0;

    while (true)
    {
        svcQueryMemory(&mi, &pi, curaddr);
        if (mi.perm == Perm_Rx && moduleCount++ == moduleIndex)
            break;
            
        curaddr = mi.addr + mi.size;
    }
    
    return mi.addr + addr;
}

u64 ProcessMemory::ImageAddr(u64 addr)
{
    return ProcessMemory::RtldAddr(addr);
}
u64 ProcessMemory::RtldAddr(u64 addr)
{
    static u64 rtldAddr = 0;
    if (!rtldAddr)
    {
        rtldAddr = ProcessMemory::ModuleAddr(0, 0);
    }
    return rtldAddr + addr;
}
u64 ProcessMemory::MainAddr(u64 addr)
{
    static u64 mainAddr = 0;
    if (!mainAddr)
    {
        mainAddr = ProcessMemory::ModuleAddr(0, 1);
    }
    return mainAddr + addr;
}
u64 ProcessMemory::SdkAddr(u64 addr)
{
    static u64 sdkAddr = 0;
    if (!sdkAddr)
    {
        sdkAddr = ProcessMemory::ModuleAddr(0, 2);
    }
    return sdkAddr + addr;
}

u64 ProcessMemory::ThisAddr(u64 addr)
{
    static u64 sdkAddr = 0;
    if (!sdkAddr)
    {
        sdkAddr = ProcessMemory::ModuleAddr(0, 3);
    }
    return sdkAddr + addr;
}

HidSharedMemory* ProcessMemory::GetHidSharedMemory()
{
    MemoryInfo mi;
    u32 pi;
    u64 curaddr = 0;

    while (true)
    {
        svcQueryMemory(&mi, &pi, curaddr);
        if (mi.perm == Perm_R && mi.type == MemType_SharedMem && mi.size == 0x40000)
            break;
            
        curaddr = mi.addr + mi.size;
    }
    
    return (HidSharedMemory*)mi.addr;
}