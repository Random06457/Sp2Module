#pragma once

#include <switch.h>

struct float32x4_t; 
struct float32x2_t; 

#define ASSIGN_FARR(arr, f1, f2, f3, f4) arr[0] = f1; arr[1] = f2;  arr[2] = f3;  arr[3] = f4;  

struct float32x4_t
{
    float32x4_t(float f1, float f2, float f3, float f4)
    {
        ASSIGN_FARR(f, f1, f2, f3, f4);
    }
    float32x4_t()
    {
        ASSIGN_FARR(f, 0, 0, 0, 0);
    }
    float f[4];
};
struct float32x2_t
{
    float f[2];
};

float32x4_t vaddq_f32(float32x4_t* a, float32x4_t* b);
float32x4_t vmlaq_lane_f32(float32x4_t* a, float32x4_t* b, float v);
float32x4_t vmulq_n_f32(float32x4_t* a, float b);