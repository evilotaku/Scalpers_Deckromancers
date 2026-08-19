using System;
using System.Collections.Generic;
using BattleCardGameFramework;

namespace mahjong
{
    [Serializable]
    public class MahjongGameClientStateDTO : BaseGameClientStateDTO
    {
        public int WallSize;
        public MahjongPlayerClientStateDTO YourState;
        public List<MahjongPlayerClientStateDTO> OpponentStates;
    }

    [Serializable]
    public class MahjongPlayerClientStateDTO
    {
        public string PlayerId;
        public int HandSize;
        public List<MahjongTileClientStateDTO> Hand; // Only for caller
        public List<MahjongTileClientStateDTO> River;
    }

    [Serializable]
    public class MahjongTileClientStateDTO
    {
        public int Id;
        public int Suit;
        public int Value;
    }
}
