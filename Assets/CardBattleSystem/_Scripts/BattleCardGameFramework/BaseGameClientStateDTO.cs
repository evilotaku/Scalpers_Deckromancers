using System;

namespace BattleCardGameFramework
{
    [Serializable]
    public abstract class BaseGameClientStateDTO
    {
        public bool IsGameOver;
        public string ActivePlayerId;
    }
}
