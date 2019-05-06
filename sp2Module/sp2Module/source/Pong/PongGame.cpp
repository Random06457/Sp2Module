#include "PongGame.hpp"

PongGame g_pongGame;


#define PLAYER_SPEED    10
#define BALL_SPEED      8
#define BALL_SIZE       6

#define PLAYER_H        100
#define PLAYER_W        8
#define PLAYER_OFF      50

#define GAME_X          200
#define GAME_Y          100

#define GAME_W          (SCREEN_W - GAME_X * 2)
#define GAME_H          (SCREEN_H - GAME_Y * 2)

#define BORDER_SIZE     4

PongBall::PongBall()
{
    Reset(sead::Vector2<float>(1.0, 0));
}
void PongBall::Reset(sead::Vector2<float> startVec)
{
    mX = SCREEN_W / 2;
    mY = SCREEN_H / 2;
    mDirection = startVec;
}
void PongBall::Render(agl::lyr::RenderInfo* info)
{
    sead::Color4f foreColor = {1, 1, 1, 1};
    DrawRect(info, mX - BALL_SIZE/2, mY - BALL_SIZE/2, BALL_SIZE, BALL_SIZE, &foreColor);
}


PongGame::PongGame() :
mPlayers( {
    PongPlayer(GAME_X + PLAYER_OFF),
    PongPlayer(SCREEN_W - GAME_X - PLAYER_OFF) 
    })
{
    Reset();
}

PongGame::~PongGame()
{
}

void PongGame::Reset()
{
    mPlayers[0].Reset();
    mPlayers[1].Reset();
    mBall.Reset(sead::Vector2<float>(1, 0));
}

void PongGame::Logic()
{   
    hidScanInput();

    u64 kDown = hidKeysDown(CONTROLLER_P1_AUTO);
    u64 kHeld = hidKeysHeld(CONTROLLER_P1_AUTO);

    if (kDown & KEY_B)
        Reset();
    
    if (kHeld & KEY_UP && !(kHeld & KEY_DOWN))
    {
        float y = mPlayers[0].mY + PLAYER_SPEED;
        if (y >= (GAME_Y + GAME_H) - PLAYER_H)
            y = (GAME_Y + GAME_H) - PLAYER_H;

        mPlayers[0].mY = y;
        mPlayers[1].mY = y;
    }
    if (kHeld & KEY_DOWN && !(kHeld & KEY_UP))
    {
        float y = mPlayers[0].mY - PLAYER_SPEED;
        if (y <= GAME_Y)
            y = GAME_Y;

        mPlayers[0].mY = y;
        mPlayers[1].mY = y;
    }

    //ball
    mBall.mX += mBall.mDirection.X * BALL_SPEED;
    mBall.mY += mBall.mDirection.Y * BALL_SPEED;
    
    if (mBall.mY >= (GAME_Y + GAME_H) - BALL_SIZE)
    {
        mBall.mY = (GAME_Y + GAME_H) - BALL_SIZE;
        mBall.mDirection.Y = -mBall.mDirection.Y;
    }
    if (mBall.mY <= GAME_Y)
    {
        mBall.mY = GAME_Y;
        mBall.mDirection.Y = -mBall.mDirection.Y;
    }

    if (mBall.mX >= (GAME_X + GAME_W) - BALL_SIZE)
    {
        mBall.Reset(sead::Vector2<float>(-1, 0));
        mPlayers[0].mScore++;
    }
    if (mBall.mX <= GAME_X)
    {
        mBall.Reset(sead::Vector2<float>(1, 0));
        mPlayers[1].mScore++;
    }

    //player 1 coll
    if (mBall.mX > (float)(GAME_X + PLAYER_OFF) &&
        mBall.mX <= (float)(GAME_X + PLAYER_OFF + PLAYER_W) &&
        mBall.mY >= mPlayers[0].mY && mBall.mY < mPlayers[0].mY + PLAYER_H)
    {
        float midDistance = PLAYER_H/2 - (mBall.mY - mPlayers[0].mY);

        mBall.mDirection.X = -mBall.mDirection.X;
        mBall.mDirection.Y = -(midDistance/70);
    }

    //player 2 coll
    if (mBall.mX >= GAME_X + GAME_W - PLAYER_OFF &&
        mBall.mX < GAME_X + GAME_W - PLAYER_OFF + PLAYER_W &&
        mBall.mY >= mPlayers[1].mY && mBall.mY < mPlayers[1].mY + PLAYER_H)
    {
        float midDistance = PLAYER_H/2 - (mBall.mY - mPlayers[1].mY);

        mBall.mDirection.X = -mBall.mDirection.X;
        mBall.mDirection.Y = -(midDistance/30);
    }

    return;
}

void PongGame::Render(agl::lyr::RenderInfo* info)
{   
    sead::TextWriter writer;
    sead::TextWriter::Constructor(&writer, (sead::DrawContext*)info->mDrawContext);
    writer.mViewport = info->mViewport;

    sead::Color4f bgColor = {0, 0, 0, 1};
    sead::Color4f foreColor = {1, 1, 1, 1};


    //draw background
    DrawRect(info, 0, 0, SCREEN_W, SCREEN_H, &bgColor);
    //top
    DrawRect(info, GAME_X, GAME_Y - BORDER_SIZE, GAME_W, BORDER_SIZE, &foreColor);
    //bottom
    DrawRect(info, GAME_X, GAME_Y + GAME_H, GAME_W, BORDER_SIZE, &foreColor);
    //left
    DrawRect(info, GAME_X - BORDER_SIZE, GAME_Y, BORDER_SIZE, GAME_H, &foreColor);
    //right
    DrawRect(info, GAME_X + GAME_W, GAME_Y, BORDER_SIZE, GAME_H, &foreColor);

    mPlayers[0].Render(info);
    mPlayers[1].Render(info);
    mBall.Render(info);

    agl::DrawContext::changeShaderMode(info->mDrawContext, 0);
    sead::TextWriter::beginDraw(&writer);


    DrawText(&writer, SCREEN_W/2 - 100, 10, 2.0, foreColor, "--Pong Game--");

    DrawText(&writer, 100, 10, 2.0, foreColor, "Player 1 : %d", mPlayers[0].mScore);
    DrawText(&writer, SCREEN_W - 300, 10, 2.0, foreColor, "Player 2 : %d", mPlayers[1].mScore);

    DrawText(&writer, 50, SCREEN_H - 35, 1.5, foreColor, "B : Quit");

    sead::TextWriter::endDraw(&writer);
}


void PongGame::PongProcess(agl::lyr::RenderInfo* info)
{
    static bool pongInit = false;
    if(!pongInit)
    {
        g_pongGame = PongGame();
        pongInit = true;
    }
        
    g_pongGame.Logic();
    g_pongGame.Render(info);
}

PongPlayer::PongPlayer(float x)
{
    Reset();
    mX = x;
}

void PongPlayer::Reset()
{
    mY = GAME_Y;
    mScore = 0;
}

void PongPlayer::Render(agl::lyr::RenderInfo* info)
{
    sead::Color4f c = {1, 1, 1, 1};
    DrawRect(info, mX, mY, PLAYER_W, PLAYER_H, &c);
}