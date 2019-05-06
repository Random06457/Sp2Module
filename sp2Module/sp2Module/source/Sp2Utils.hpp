#pragma once
#include "ProcessMemory.hpp"
#include <switch.h>
#include <sys/socket.h>
#include <arpa/inet.h>

void initSp2Funcs();

/* This only contains the minimal required structs, I don't plan to include everything I know as this is just a template*/


extern void* (*sp2Malloc)(u64);

namespace sead
{
    template<typename T>
    class Vector2
    {
        public:
        Vector2(){}
        Vector2(T x, T y) : X(x), Y(y)
        {

        }
        T X, Y;
    };
    template<typename T>
    class Vector3
    {
        public:
        Vector3(){}
        Vector3(T x, T y, T z) : X(x), Y(y), Z(z)
        {

        }
        T X, Y, Z;
    };

    template<typename T>
    class Matrix44
    {
        public:
        T mat[4][4];
    };

    template<typename T>
    class Matrix34
    {
        public:
        T mat[3][4];
    };

    template<typename T>
    class BoundBox2
    {
        public:
        BoundBox2() {}
        BoundBox2(T top, T left, T right, T bottom) : Top(top), Left(left), Right(right), Bottom(bottom)
        {

        }
        T Top, Left, Right, Bottom;
    };

    struct Color4f
    {
        float R;
        float G;
        float B;
        float A;
    };

    template<typename T>
    class SafeStringBase
    {
        public:
        SafeStringBase(const T* data) : __vftable((void*)ProcessMemory::MainAddr(0x2FCC3D0)), mData(data)
        {
        }
        void* __vftable;
        const T* mData;
    };
    static_assert(sizeof(SafeStringBase<char>) == 0x10, "Invalid SafeStringBase size");
    
    template<typename T>
    class BufferedSafeStringBase : SafeStringBase<T>
    {
        public:
        BufferedSafeStringBase(const T* data, int size)
        {
            this->__vftable = (void*)ProcessMemory::MainAddr(0x2FE69A0);
            this->mData = data;
            this->mSize = size;
        }
        u64 mSize;
    };
    static_assert(sizeof(BufferedSafeStringBase<char>) == 0x18, "Invalid BufferedSafeStringBase size");
    

    struct Camera
    {
        void* __vftable;
        Matrix34<float> matrix;
        u32 pos;
        u32 camera_x3C;
        u32 camera_x40;
        u32 at;
        u32 camera_x48;
    } PACKED;
    static_assert(sizeof(Camera) == 0x4C, "Invalid Camera size");

    

    class Viewport;
    class DrawContext;
    class Projection
    {
        public:
        static Matrix44<float>*(*getProjectionMatrix)(Projection* proj);
    };

    class TextWriter
    {
        public:
        static void(*setupGraphics)(DrawContext *a1);
        static void(*Constructor)(TextWriter*, DrawContext*);
        static void(*printImpl_)(TextWriter*, const char *text, int a3, bool a4, BoundBox2<float> *box);
        static void(*beginDraw)(TextWriter*);
        static void(*endDraw)(TextWriter*);
        static void(*setCursorFromTopLeft)(TextWriter *, Vector2<float> *pos);
        static void(*printf)(sead::TextWriter *, const char *Format, ...);
        static void(*calcFormatStringRect)(sead::TextWriter*, sead::BoundBox2<float>* box, const char* Format, ...);

        void *__vftable;
        void *mViewport;
        void *mProjection;
        void *mCamera;
        int TextWriter_x20;
        int TextWriter_x24;
        int TextWriter_x28;
        int TextWriter_x2C;
        float mScale1;
        float mScale2;
        Color4f mColor;
        int TextWriter_x48;
        float mLineSpace;
        int TextWriter_x50;
        int TextWriter_x54;
        int TextWriter_x58;
        int TextWriter_x5C;
        char16_t *mFormatBuffer;
        int mFormatBufferSize;
        int TextWriter_x6C;
        DrawContext *mDrawContext;
    };
    static_assert(sizeof(TextWriter) == 0x78, "Invalid TextWriter size");

}

namespace agl
{
    class DrawContext
    {
        public:
        static void(*changeShaderMode)(DrawContext*, u8 mode); 
    };

    namespace lyr
    {
        class Renderer
        {
            public:
            static void (*draw)(Renderer *, DrawContext *, int);

        };
        struct RenderInfo
        {
            int RenderInfo_x0;
            int RenderInfo_x4;
            int RenderInfo_x8;
            int RenderInfo_xC;
            void* mRenderBuffer;
            int RenderInfo_x18;
            int RenderInfo_x1C;
            int RenderInfo_x20;
            int RenderInfo_x24;
            sead::Camera* mRenderCamera;
            sead::Projection* mProjection;
            sead::Viewport* mViewport;
            int RenderInfo_x40;
            int RenderInfo_x44;
            DrawContext* mDrawContext;
            int RenderInfo_x50;
            int RenderInfo_x54;
            int RenderInfo_x58;
            int RenderInfo_x5C;
        };
        static_assert(sizeof(RenderInfo) == 0x60, "Invalid RenderInfo size");

    }

    namespace utl
    {
        namespace DevTools
        {
            extern void(*drawColorQuad)(agl::DrawContext *a1, sead::Color4f *color, sead::Matrix34<float> *a3, sead::Matrix44<float> *a4);
            extern void(*drawTriangleImm)(agl::DrawContext *a1, sead::Vector3<float> *pos1, sead::Vector3<float> *pos2, sead::Vector3<float> *pos3, sead::Color4f *color);
        }
    }
}

namespace nn
{
    namespace err
    {
        struct ApplicationErrorArg
        {
            int ApplicationErrorArg_x0; //unknown, sdk sets it always to 0x01020000
            int ApplicationErrorArg_x4; //never used
            int ErrorCodeNumber;
            u64 LanguageCode;
            char DialogMessage[2048];
            char FullScreenMessage[2048];
        } PACKED;

        static_assert(sizeof(ApplicationErrorArg) == 0x1014, "Invalid ApplicationErrorArg size");

        extern void(*ShowError)(u32);
        extern void(*ShowApplicationError)(ApplicationErrorArg *);
        
        extern void(*ApplicationErrorArg_Constructor)(ApplicationErrorArg*);
        extern void(*ApplicationErrorArg_Constructor2)(ApplicationErrorArg*, Result number, const char *dialogViewMessage, const char *fullScreenViewMessage, u64* languageCode);
    }

    namespace diag
    {
        enum LogSeverity {
            LogSeverity_Trace,
            LogSeverity_Info,
            LogSeverity_Warn,
            LogSeverity_Error,
            LogSeverity_Fatal
        };

        struct SourceInfo
        {
            int lineNumber;
            const char* fileName;
            const char* functionName;
        };

        struct LogMetaData
        {
            SourceInfo sourceInfo;
            const char* moduleName;
            LogSeverity severity;
            int verbosity;
            bool useDefaultLocaleCharset;
            void* pAdditionalData;
            size_t additionalDataBytes;
        };
    }

    namespace socket
    {
        const size_t MemoryPoolAlignment      = 4096;
        const int DefaultSocketMemoryPoolSize = (6  * 1024 * 1024);
        
        
        extern Result(*Initialize)(void *memoryPool, u64 memoryPoolSize, u64 allocatorPoolSize, int concurrencyLimit);
        extern void(*Finalize)();
        extern Result(*Socket)(int domain, int type, int protocol);
        extern u16(*InetHtons)(u16 hostValue);
        extern int(*Connect)(int socket, const sockaddr* pAddress, socklen_t addressLength);
        extern ssize_t(*Send)(int socket, const void* buffer, size_t bufferLength, int flags);
        extern ssize_t(*Recv)(int socket, void* outBuffer, size_t outBufferLength, int flags);
        extern void(*Close)(int);
    }
}

namespace Cmn
{
    namespace Def
    {
        extern u32(*getRomType)();
    }
}
namespace MiniGame
{
    namespace Gfx
    {
        extern void(*drawLine)(agl::lyr::RenderInfo *a1, sead::Vector2<float> *a2, sead::Vector2<float> *a3, float a4, sead::Color4f *a5);
    }
    namespace EtcDisp
    {
        extern void(*draw)(void*, agl::lyr::RenderInfo*);
    }
    namespace TextBoxPage
    {
        extern void(*setTextDirect)(void*, sead::SafeStringBase<char16_t>* text);
    }
}
