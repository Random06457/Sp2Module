#include "FsLogger.hpp"

FsFileSystem g_fsHandle;
FsFile g_fileHandle;

int g_fsInit = 0;


void LogRaw(void* data, size_t size)
{
    if (!g_fsInit)
        return;
    
    u64 filesize = 0;
    
    Result rc = fsFileGetSize(&g_fileHandle, &filesize);
    if (R_SUCCEEDED(rc))
    {
        fsFileWrite(&g_fileHandle, filesize, data, size);
        fsFileFlush(&g_fileHandle);
    }
}

void FsLogger::Initialize()
{
    if (g_fsInit)
        return;

    const char* path = "/sp2Module/logs.txt";
    Result rc;
    
    rc = fsInitialize();
    if (R_FAILED(rc))
        goto error_fs;

    if (R_FAILED(rc = fsMountSdcard(&g_fsHandle)))
        goto error_fs;
        
    if (R_FAILED(rc = fsFsCreateDirectory(&g_fsHandle, "/sp2Module")) && rc != 0x402)
        goto error_fs;

    if (R_FAILED(rc = fsFsCreateFile(&g_fsHandle, path, 0, 0)))
    {
        if (rc == 0x402) //path already exist
        {
            if (R_FAILED(rc = fsFsDeleteFile(&g_fsHandle, path)))
                goto error_fs;
            rc = fsFsCreateFile(&g_fsHandle, path, 0, 0);
        }
        if (R_FAILED(rc))
            goto error_fs;
    }

    if (R_FAILED(rc = fsFsOpenFile(&g_fsHandle, path, FS_OPEN_READ | FS_OPEN_APPEND | FS_OPEN_WRITE, &g_fileHandle)))
        goto error_fs;

    if (R_FAILED(rc = fsFileSetSize(&g_fileHandle, 0)))
        goto error_fs;
    
    g_fsInit = 1;
    
    FsLogger::LogFormat("fs initialized!");
    return;

error_fs:
    fatalSimple((u32)Sp2Error::fsInitialize);
}

void FsLogger::Log(u8* data, size_t size)
{
    u8* ptr = new u8[size+2];
    memset(ptr, 0, size+2);
    ptr[size] = (u8)'\n';
    memcpy(ptr, data, size);

    LogRaw(ptr, size+1);
}

void FsLogger::Log(std::string str)
{
    FsLogger::Log((u8*)str.data(), str.size());
}

void FsLogger::Log(std::vector<u8> vec)
{
    FsLogger::Log(vec.data(), vec.size());
}

void FsLogger::LogFormat(const char* format, ...)
{
    va_list args;
    char buff[0x1000];
    memset(buff, 0, sizeof(buff));
    va_start(args, format);

    int len = vsnprintf(buff, sizeof(buff), format, args);
    
    FsLogger::Log((u8*)buff, len);
    
    va_end (args);
}