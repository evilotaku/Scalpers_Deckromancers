using System;

namespace BattleCardGameFramework
{
    [Serializable]
    public class HearthstoneGameClientStateDTO : BaseGameClientStateDTO
    {
        public PlayerClientStateDTO YourState;
        public PlayerClientStateDTO OpponentState;
    }
}