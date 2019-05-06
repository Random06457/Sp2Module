#pragma once
#include <vector>
#include <string>
#include <stdarg.h>

#include <switch.h>

#include "Sp2Utils.hpp"
#include "Result.hpp"

class FsLogger
{
    public:
    static void Initialize();
    static void Log(u8* data, size_t size);
    static void Log(std::string str);
    static void Log(std::vector<u8> vec);
    static void LogFormat(const char* format, ...);
};