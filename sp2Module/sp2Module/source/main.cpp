#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <string>
#include <vector>
#include <sstream>
#include <sys/stat.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <malloc.h>

#include <switch.h>

#include "ProcessMemory.hpp"
#include "Sp2Utils.hpp"
#include "Draw.hpp"
#include "FsLogger.hpp"
#include "TcpLogger.hpp"
#include "Pong/PongGame.hpp"

//todo : choose a better moment to hook this up
void serviceInit()
{
    Result rc;

    //for setGetLanguageCode
    if (R_FAILED(rc = setInitialize()))
        fatalSimple((u32)Sp2Error::setInitialize);
}


Result svcConnectToNamedPortHook(Handle* session, char* name)
{
    initSp2Funcs();
    Result rc = svcConnectToNamedPort(session, name);

    if (strcmp(name, "sm:") == 0)
    {   
        //we steal the handle and pass it to libnx
        smSetHandle(*session);
    }
    
    return rc;
}

//todo
void DrawTvDrcHook(agl::lyr::Renderer *renderer, agl::DrawContext* ctx, int tv)
{
    agl::lyr::Renderer::draw(renderer, ctx, tv);
}


//nn::diag::detail::PutImpl
void diagPutImplHook(nn::diag::LogMetaData* meta, const char* str, u64 len)
{
    //FsLogger::Initialize();
    //FsLogger::LogFormat(str);
}

//nn::diag::detail::LogImpl
void diagLogImplHook(nn::diag::LogMetaData* meta, const char* format, ...)
{
    char buff[0x1000];
    va_list args;

    memset(buff, 0, sizeof(buff));
    va_start(args, format);
    int len = vsnprintf(buff, sizeof(buff), format, args);


    va_end (args);
    
    //FsLogger::Initialize();
    //FsLogger::Log((u8*)buff, len);
    //TcpLogger::LogFormat(buff);
}

const char* ret_ShootinRangeName()
{
    static int callCount = 0;
    //FsLogger::LogFormat("hello world from ret_ShootinRangeName() : %d", callCount++);
    //TcpLogger::LogFormat("hello world from ret_ShootinRangeName() : %d", callCount);

    auto ret = "ShootingRange";
    
    //x8 is then pushed on the stack
    asm("mov x8, x0");
    return ret;
}

void socketInitHook()
{
    serviceInit();
    //TcpLogger::Initialize();
}
void MiniGameSetTextBoxHook(void* textBox)
{
    sead::SafeStringBase<char16_t> str(u"\0");

    MiniGame::TextBoxPage::setTextDirect(textBox, &str);
}
void MiniGameEtcDrawHook(void* etc, agl::lyr::RenderInfo* info)
{
    static bool hidInit = false;
    if (!hidInit)
    {
        hidSetSharedMemPtr(ProcessMemory::GetHidSharedMemory());
        hidReset();
        hidInit = true;
    }

    PongGame::PongProcess(info);
    return;
}


int main(int argc, char* argv[])
{
    ret_ShootinRangeName();
    svcConnectToNamedPortHook(NULL, NULL);
    MiniGameEtcDrawHook(NULL, NULL);
    MiniGameSetTextBoxHook(NULL);
    socketInitHook();
    diagPutImplHook(NULL, NULL, 0);
    diagLogImplHook(NULL, NULL);
    DrawTvDrcHook(NULL, NULL, 0);
    return 0;
}


alignas(16) u8 __nx_exception_stack[0x1000];
u64 __nx_exception_stack_size = sizeof(__nx_exception_stack);

//todo
void __libnx_exception_handler(ThreadExceptionDump *ctx)
{
}