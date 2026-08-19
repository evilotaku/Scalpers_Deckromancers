using System;
using System.Collections.Generic;
using BattleCardGameFramework;

namespace snap
{
    [Serializable]
    public class SnapGameClientStateDTO : BaseGameClientStateDTO
    {
        public int CurrentTurn;
        public List<SnapLocationClientStateDTO> Locations;
        public SnapPlayerClientStateDTO YourState;
        public SnapPlayerClientStateDTO OpponentState;
    }

    [Serializable]
    public class SnapLocationClientStateDTO
    {
        public int Index;
        public string Name;
        public int YourPower;
        public int OpponentPower;
    }

    [Serializable]
    public class SnapPlayerClientStateDTO
    {
        public string PlayerId;
        public int Energy;
        public int MaxEnergy;
        public List<SnapCardClientStateDTO> Hand;
        public int DeckSize;
        public List<List<SnapCardClientStateDTO>> Boards; // 3 boards, one for each location
        public List<List<SnapCardClientStateDTO>> Pendings; // 3 pending collections
    }

    [Serializable]
    public class SnapCardClientStateDTO
    {
        public int Id;
        public string CardType;
        public int Cost;
        public int Power;
        public bool IsRevealed;
    }
}