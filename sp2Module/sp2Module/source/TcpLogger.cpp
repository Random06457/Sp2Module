#include "TcpLogger.hpp"


#define IP      "192.168.0.10"
#define PORT    6969

int g_tcpSocket;
int g_loggerInit = 0;


std::list<u8*>* g_msgQueue = nullptr;

void SendRaw(void* data, size_t size)
{
    nn::socket::Send(g_tcpSocket, data, size, 0);
}


void AddToQueue(u8* data)
{
    if(!g_msgQueue)
        g_msgQueue = new std::list<u8*>();

    g_msgQueue->push_back(data);
}

void ClearQueue()
{
    if(!g_msgQueue)
        return;

    while (!g_msgQueue->empty())
    {
        auto data = g_msgQueue->front();
        SendRaw(data, strlen((char*)data));
        delete[] data;
        g_msgQueue->pop_front();
    }
    
}


void TcpLogger::Initialize()
{
    struct sockaddr_in serverAddr;
    g_tcpSocket = nn::socket::Socket(AF_INET, SOCK_STREAM, 0);
    if(g_tcpSocket & 0x80000000)
        nn::err::ShowError((u32)Sp2Error::nnSocket);

    serverAddr.sin_addr.s_addr = inet_addr(IP);
    serverAddr.sin_family      = AF_INET;
    serverAddr.sin_port        = nn::socket::InetHtons(PORT);

    int rval = nn::socket::Connect(g_tcpSocket, (struct sockaddr*)&serverAddr, sizeof(serverAddr));
    if (rval < 0)
        nn::err::ShowError((u32)Sp2Error::nnSocketConnect);


    g_loggerInit = 1;
    
    TcpLogger::LogFormat("socket initialized!");
    ClearQueue();
}

void TcpLogger::Log(u8* data, size_t size)
{
    u8* ptr = new u8[size+2];
    memset(ptr, 0, size+2);
    ptr[size] = (u8)'\n';
    memcpy(ptr, data, size);

    if (!g_loggerInit)
    {
        AddToQueue(ptr);
        return;
    }

    SendRaw(ptr, size+1);
}

void TcpLogger::Log(std::string str)
{
    TcpLogger::Log((u8*)str.data(), str.size());
}

void TcpLogger::Log(std::vector<u8> vec)
{
    TcpLogger::Log(vec.data(), vec.size());
}

void TcpLogger::LogFormat(const char* format, ...)
{
    va_list args;
    char buff[0x1000];
    memset(buff, 0, sizeof(buff));
    va_start(args, format);

    int len = vsnprintf(buff, sizeof(buff), format, args);
    
    TcpLogger::Log((u8*)buff, len);
    
    va_end (args);
}