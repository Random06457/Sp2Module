#include "Draw.hpp"


void DrawRectLight(agl::DrawContext* drawCtx, float x, float y, float w, float h, sead::Color4f* color)
{
    sead::Vector2<float> pos1 = sead::Vector2<float>(x, y);
    sead::Vector2<float> pos2 = sead::Vector2<float>(x+w, y);
    sead::Vector2<float> pos3 = sead::Vector2<float>(x, y+h);
    sead::Vector2<float> pos4 = sead::Vector2<float>(x+w, y+h);

    DrawTriangle(drawCtx, pos1, pos2, pos3, *color);
    DrawTriangle(drawCtx, pos2, pos3, pos4, *color);
}
void DrawTriangle(agl::DrawContext* drawCtx, sead::Vector2<float> pos1, sead::Vector2<float> pos2, sead::Vector2<float> pos3, sead::Color4f color)
{
    sead::Vector3<float> vec1 = sead::Vector3<float>(pos1.X, pos1.Y, 0);
    sead::Vector3<float> vec2 = sead::Vector3<float>(pos2.X, pos2.Y, 0);
    sead::Vector3<float> vec3 = sead::Vector3<float>(pos3.X, pos3.Y, 0);
    agl::utl::DevTools::drawTriangleImm(drawCtx, &vec1, &vec2, &vec3, &color);
}

//todo : make the matrix mess better
void DrawRect(agl::lyr::RenderInfo* info, sead::Vector2<float>* pos, sead::Vector2<float>* size, sead::Color4f* color)
{
    auto camMatrix = info->mRenderCamera->matrix;
    auto projMatrix = sead::Projection::getProjectionMatrix(info->mProjection);

    sead::Matrix34<float> m;

    float x = (pos->X + size->X/2) - SCREEN_W/2;
    float y = (pos->Y + size->Y/2) - SCREEN_H/2;

    ASSIGN_FARR(m.mat[0], size->X, 0, 0, x);
    ASSIGN_FARR(m.mat[1], 0, size->Y, 0, y);
    ASSIGN_FARR(m.mat[2], 0, 0, 1, 0);
    
    for (size_t i = 0; i < 3; i++)
    {
        auto temp = vmulq_n_f32((float32x4_t*)&m.mat[0], camMatrix.mat[i][0]);
        temp = vmlaq_lane_f32(&temp, (float32x4_t*)m.mat[1], camMatrix.mat[i][1]);
        temp = vmlaq_lane_f32(&temp, (float32x4_t*)m.mat[2], camMatrix.mat[i][2]);
        float32x4_t f1(0, 0, 0, camMatrix.mat[i][3]);
        *(float32x4_t*)m.mat[i] = vaddq_f32(&f1, &temp);
    }
    
    agl::utl::DevTools::drawColorQuad(info->mDrawContext, color, &m, projMatrix);
}
void DrawRect(agl::lyr::RenderInfo* info, float x, float y, float w, float h, sead::Color4f* color)
{
    sead::Vector2<float> pos = sead::Vector2<float>(x, y);
    sead::Vector2<float> size = sead::Vector2<float>(w, h);
    DrawRect(info, &pos, &size, color);
}

void DrawText(sead::TextWriter* writer, float x, float y, float scale, sead::Color4f color, const char* Format, ...)
{
    va_list args;
    char buff[0x1000];
    memset(buff, 0, sizeof(buff));
    va_start(args, Format);

    writer->mScale1 = scale;
    writer->mScale2 = scale;
    sead::Vector2<float> pos = sead::Vector2<float>(x, y);
    sead::TextWriter::setCursorFromTopLeft(writer, &pos);
    
    vsnprintf(buff, sizeof(buff), Format, args);

    sead::TextWriter::printf(writer, buff);
    
    va_end (args);
}

float CalcTextWidth(sead::TextWriter* writer, const char* Format, ...)
{
    va_list args;
    char buff[0x1000];
    memset(buff, 0, sizeof(buff));
    va_start(args, Format);
    
    vsnprintf(buff, sizeof(buff), Format, args);

    sead::BoundBox2<float> box;
    sead::TextWriter::calcFormatStringRect(writer, &box, buff);
    
    va_end (args);

    //multiply by font scale isn't correct apparently
    return box.Right * writer->mScale1;
}