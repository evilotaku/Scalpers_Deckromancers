using csbcgf;
using System.Collections.Generic;
using System.Linq;

namespace mahjong
{
    public class MahjongGame : Game<MahjongGameState>
    {
        public MahjongGame() : base(new MahjongGameState(true))
        {
            Init();
        }

        private void Init()
        {
            // Create 4 MahjongPlayers
            for (int i = 0; i < 4; i++)
            {
                State.AddPlayer(new MahjongPlayer(true));
            }

            // Create 136 MahjongTiles
            ICardCollection wall = State.GetCardCollection(CollectionKeys.Wall);
            
            // 3 Suits: Dots, Bamboos, Characters (1-9)
            foreach (MahjongSuit suit in new[] { MahjongSuit.Dots, MahjongSuit.Bamboos, MahjongSuit.Characters })
            {
                for (int val = 1; val <= 9; val++)
                {
                    for (int count = 0; count < 4; count++)
                    {
                        wall.Add(new MahjongTile(suit, val));
                    }
                }
            }

            // Winds: East, South, West, North (1-4)
            for (int val = 1; val <= 4; val++)
            {
                for (int count = 0; count < 4; count++)
                {
                    wall.Add(new MahjongTile(MahjongSuit.Winds, val));
                }
            }

            // Dragons: Red, Green, White (1-3)
            for (int val = 1; val <= 3; val++)
            {
                for (int count = 0; count < 4; count++)
                {
                    wall.Add(new MahjongTile(MahjongSuit.Dragons, val));
                }
            }

            // Shuffle wall
            wall.Shuffle();

            // Deal 13 tiles to each player (standard Mahjong hand size)
            foreach (MahjongPlayer player in State.Players.Cast<MahjongPlayer>())
            {
                ICardCollection hand = player.GetCardCollection(CollectionKeys.Hand);
                for (int i = 0; i < 13; i++)
                {
                    if (!wall.IsEmpty)
                    {
                        ICard tile = wall.First;
                        wall.Remove(tile);
                        hand.Add(tile);
                    }
                }
            }

            // Start with Player 0
            State.ActivePlayerIndex = 0;
        }
    }
}
