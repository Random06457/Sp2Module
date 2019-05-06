#pragma once

#include <switch.h>
#include "Sp2Utils.hpp"
#include "ArmNeon.hpp"
#include <stdarg.h>
#include <cstring>
#include <stdio.h>
#include "FsLogger.hpp"

#define SCREEN_W    1280
#define SCREEN_H    720

void DrawRectLight(agl::DrawContext* drawCtx, float x, float y, float w, float h, sead::Color4f* color);
void DrawTriangle(agl::DrawContext* drawCtx, sead::Vector2<float> pos1, sead::Vector2<float> pos2, sead::Vector2<float> pos3, sead::Color4f color);
void DrawRect(agl::lyr::RenderInfo* info, sead::Vector2<float>* pos, sead::Vector2<float>* size, sead::Color4f* color);
void DrawRect(agl::lyr::RenderInfo* info, float x, float y, float w, float h, sead::Color4f* color);
void DrawText(sead::TextWriter* writer, float x, float y, float scale, sead::Color4f color, const char* Format, ...);
float CalcTextWidth(sead::TextWriter* writer, const char* Format, ...);