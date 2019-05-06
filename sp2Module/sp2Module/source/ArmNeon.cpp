#include "ArmNeon.hpp"


/* maybe rewrite this part in asm for optimisation purpose */


float32x4_t vmlaq_lane_f32(float32x4_t* a, float32x4_t* b, float v)
{
    float32x4_t f;
    for (size_t i = 0; i < 4; i++)
        f.f[i] = a->f[i] + (b->f[i] * v);
    return f;
} 
float32x4_t vmulq_n_f32(float32x4_t* a, float b)
{
    float32x4_t f;
    for (size_t i = 0; i < 4; i++)
        f.f[i] = a->f[i] * b;
    return f;
}

float32x4_t vaddq_f32(float32x4_t* a, float32x4_t* b)
{
    float32x4_t f;
    for (size_t i = 0; i < 4; i++)
        f.f[i] = a->f[i] + b->f[i];
    return f;
}