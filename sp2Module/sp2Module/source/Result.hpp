#pragma once
#include "Sp2Utils.hpp"
#include <stdio.h>
#include <cstdio>
#include <stdarg.h>
#include <cstring>

#include <switch.h>

enum class Sp2Error : u32
{
    Breakpoint = 0,
    setInitialize = 1,
    fsInitialize = 2,
    setMakeLanguageCode = 3,
    nnSocket = 4,
    nnSocketConnect = 5,
};

void showAppError(const char* format, ...);