#include "Result.hpp"

//shouldn't be called too early

void showAppError(const char* format, ...)
{
    char buff[0x1000];
    va_list args;
    Result rc;
    static u64 LanguageCode = 0;
    nn::err::ApplicationErrorArg arg;
    
    memset(&arg, 0, sizeof(arg));
    memset(buff, 0, sizeof(buff));
    
    if (!LanguageCode)
    {       
        rc = setMakeLanguageCode(SetLanguage_FR, &LanguageCode);
        if (R_FAILED(rc))
            nn::err::ShowError((u32)Sp2Error::setMakeLanguageCode);
    }

    va_start(args, format);
    vsnprintf(buff, sizeof(buff), format, args);

    nn::err::ApplicationErrorArg_Constructor2(&arg, 0, buff, buff, &LanguageCode);
    nn::err::ShowApplicationError(&arg);

    va_end (args);
}