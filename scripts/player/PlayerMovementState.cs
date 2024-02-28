using System;

namespace Brave.scripts.player;

[Flags]
public enum PlayerMovementState
{
    /// <summary>
    /// 静止
    /// </summary>
    Idle = 1 << 0,
    /// <summary>
    /// 跑动
    /// </summary>
    Running = 1 << 1,
    /// <summary>
    /// 跳跃
    /// </summary>
    Jump = 1 << 2,
    /// <summary>
    /// 下落
    /// </summary>
    Fall = 1 << 3,
    /// <summary>
    /// 下蹲
    /// </summary>
    Landing = 1 << 4,
    /// <summary>
    /// 滑墙
    /// </summary>
    WallSliding = 1 << 5,
}