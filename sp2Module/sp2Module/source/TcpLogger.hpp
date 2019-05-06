#pragma once
#include <stdio.h>
#include <sys/stat.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <list>
#include <vector>
#include <string>

#include <switch.h>

#include "Sp2Utils.hpp"
#include "Result.hpp"

class TcpLogger
{
    public:
    static void Initialize();
    static void Log(u8* data, size_t size);
    static void Log(std::string str);
    static void Log(std::vector<u8> vec);
    static void LogFormat(const char* format, ...);
};