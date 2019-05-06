#include "Sp2Utils.hpp"

/* A bit messy, maybe add preprocessor directives?  */

//sead
void(*sead::TextWriter::Constructor)(sead::TextWriter*, sead::DrawContext*);
void(*sead::TextWriter::setupGraphics)(sead::DrawContext *a1);
sead::Matrix44<float>*(*sead::Projection::getProjectionMatrix)(sead::Projection* proj);
void(*sead::TextWriter::printImpl_)(sead::TextWriter *, const char *text, int a3, bool a4, sead::BoundBox2<float> *box);
void(*sead::TextWriter::beginDraw)(sead::TextWriter*);
void(*sead::TextWriter::endDraw)(sead::TextWriter*);
void(*sead::TextWriter::setCursorFromTopLeft)(sead::TextWriter *, sead::Vector2<float> *pos);
void(*sead::TextWriter::printf)(sead::TextWriter *, const char *Format, ...);
void(*sead::TextWriter::calcFormatStringRect)(sead::TextWriter*, sead::BoundBox2<float>* box, const char* Format, ...);

//nn::err
void(*nn::err::ShowError)(u32);
void(*nn::err::ShowApplicationError)(nn::err::ApplicationErrorArg *);
void(*nn::err::ApplicationErrorArg_Constructor)(ApplicationErrorArg*);
void(*nn::err::ApplicationErrorArg_Constructor2)(ApplicationErrorArg*, Result number, const char *dialogViewMessage, const char *fullScreenViewMessage, u64* languageCode);

//nn::socket
Result(*nn::socket::Initialize)(void *memoryPool, u64 memoryPoolSize, u64 allocatorPoolSize, int concurrencyLimit);
void(*nn::socket::Finalize)();
Result(*nn::socket::Socket)(int domain, int type, int protocol);
u16(*nn::socket::InetHtons)(u16 hostValue);
int(*nn::socket::Connect)(int socket, const sockaddr* pAddress, socklen_t addressLength);
ssize_t(*nn::socket::Send)(int socket, const void* buffer, size_t bufferLength, int flags);
ssize_t(*nn::socket::Recv)(int socket, void* outBuffer, size_t outBufferLength, int flags);
void(*nn::socket::Close)(int);

//Minigame
void(*MiniGame::EtcDisp::draw)(void* etc, agl::lyr::RenderInfo* info);
void(*MiniGame::Gfx::drawLine)(agl::lyr::RenderInfo *a1, sead::Vector2<float> *a2, sead::Vector2<float> *a3, float a4, sead::Color4f *a5);
void(*MiniGame::TextBoxPage::setTextDirect)(void*, sead::SafeStringBase<char16_t>* text);

//Cmn::Def
u32(*Cmn::Def::getRomType)();

//agl
void(*agl::DrawContext::changeShaderMode)(agl::DrawContext*, u8 mode);
void(*agl::utl::DevTools::drawColorQuad)(agl::DrawContext *, sead::Color4f *color, sead::Matrix34<float> *a3, sead::Matrix44<float> *a4);
void(*agl::utl::DevTools::drawTriangleImm)(agl::DrawContext *, sead::Vector3<float> *pos1, sead::Vector3<float> *pos2, sead::Vector3<float> *pos3, sead::Color4f *color);
void(*agl::lyr::Renderer::draw)(agl::lyr::Renderer *, agl::DrawContext *, int);

//global
void* (*sp2Malloc)(u64);

void initSp2Funcs()
{
    //nn::socket
    *(u64*)&nn::socket::Initialize = ProcessMemory::MainAddr(0x1551F28);
    *(u64*)&nn::socket::Finalize = ProcessMemory::MainAddr(0x1551F88);
    *(u64*)&nn::socket::Socket = ProcessMemory::MainAddr(0x1551D98);
    *(u64*)&nn::socket::InetHtons = ProcessMemory::MainAddr(0x1551D88);
    *(u64*)&nn::socket::Connect = ProcessMemory::MainAddr(0x1551E18);
    *(u64*)&nn::socket::Send = ProcessMemory::MainAddr(0x1551E58);
    *(u64*)&nn::socket::Recv = ProcessMemory::MainAddr(0x1551E48);
    *(u64*)&nn::socket::Close = ProcessMemory::MainAddr(0x1551E68);

    //Cmn
    *(u64*)&Cmn::Def::getRomType = ProcessMemory::MainAddr(0x88404);
    
    *(u64*)&sp2Malloc = ProcessMemory::MainAddr(0xF003CC);

    //nn::err
    *(u64*)&nn::err::ShowError = ProcessMemory::SdkAddr(0x10EF1C);
    *(u64*)&nn::err::ShowApplicationError = ProcessMemory::SdkAddr(0x10F394);
    *(u64*)&nn::err::ApplicationErrorArg_Constructor = ProcessMemory::SdkAddr(0x10F8FC);
    *(u64*)&nn::err::ApplicationErrorArg_Constructor2 = ProcessMemory::SdkAddr(0x10F91C);

    //sead
    *(u64*)&sead::Projection::getProjectionMatrix = ProcessMemory::MainAddr(0xFCE018);
    *(u64*)&sead::TextWriter::Constructor = ProcessMemory::MainAddr(0xFD0380);
    *(u64*)&sead::TextWriter::setupGraphics = ProcessMemory::MainAddr(0xFD04A8);
    *(u64*)&sead::TextWriter::printImpl_ = ProcessMemory::MainAddr(0xFD1344);
    *(u64*)&sead::TextWriter::beginDraw = ProcessMemory::MainAddr(0xFD0668);
    *(u64*)&sead::TextWriter::endDraw = ProcessMemory::MainAddr(0xFD069C);
    *(u64*)&sead::TextWriter::setCursorFromTopLeft = ProcessMemory::MainAddr(0xFD0508);
    *(u64*)&sead::TextWriter::printf = ProcessMemory::MainAddr(0xFD09E8);
    *(u64*)&sead::TextWriter::calcFormatStringRect = ProcessMemory::MainAddr(0xFD0D5C);

    //MiniGame
    *(u64*)&MiniGame::EtcDisp::draw = ProcessMemory::MainAddr(0xC526B8);
    *(u64*)&MiniGame::Gfx::drawLine = ProcessMemory::MainAddr(0xC511D0);
    *(u64*)&MiniGame::TextBoxPage::setTextDirect = ProcessMemory::MainAddr(0xC5D220);

    //agl
    *(u64*)&agl::DrawContext::changeShaderMode = ProcessMemory::MainAddr(0xFED6B8);
    *(u64*)&agl::utl::DevTools::drawColorQuad = ProcessMemory::MainAddr(0x1033320);
    *(u64*)&agl::utl::DevTools::drawTriangleImm = ProcessMemory::MainAddr(0x10347CC);
    *(u64*)&agl::lyr::Renderer::draw = ProcessMemory::MainAddr(0x1062768);
}