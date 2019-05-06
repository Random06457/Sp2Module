#pragma once

#include <switch.h>

class ProcessMemory
{
    public:
        static u64 ModuleAddr(u64 addr, int moduleIndex);
        static u64 ImageAddr(u64 addr);
        static u64 RtldAddr(u64 addr);
        static u64 MainAddr(u64 addr);
        static u64 SdkAddr(u64 addr);
        static u64 ThisAddr(u64 addr);
        static HidSharedMemory* GetHidSharedMemory();
};