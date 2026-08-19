using System;
using System.Collections.Generic;

namespace BattleCardGameFramework
{

    [Serializable]
    public class PlayerClientStateDTO
    {
        public string PlayerId;
        public int Mana;
        public int MaxMana;
        public int Life;
        public int MaxLife;
        public List<CardClientStateDTO> Hand;
        public int DeckSize;
        public List<CardClientStateDTO> Board;
        public List<CardClientStateDTO> Graveyard;
    }

    [Serializable]
    public class CardClientStateDTO
    {
        public int Id;
        public string CardType;
        public int ManaCost;
        public int Attack;
        public int Life;
        public bool IsReadyToAttack;
    }
}