#pragma once

#include <switch.h>
#include "../Sp2Utils.hpp"
#include "../Draw.hpp"


class PongBall
{
    public:
    PongBall();

    void Render(agl::lyr::RenderInfo* info);
    void Reset(sead::Vector2<float> startVec);
    sead::Vector2<float> mDirection;
    float mX, mY;
};

class PongPlayer
{
    private:
    float mX;
    public:
    PongPlayer(float x);
    void Render(agl::lyr::RenderInfo* info);
    void Reset();
    
    float mY;
    int mScore;
};

class PongGame
{
public:
    static void PongProcess(agl::lyr::RenderInfo* info);

    PongGame();
    ~PongGame();
    void Render(agl::lyr::RenderInfo* info);
    void Logic();
    void Reset();

    PongPlayer mPlayers[2];
    PongBall mBall;
};

