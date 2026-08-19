namespace BattleCardGameFramework
{
    public static class GameClientStateExtensions
    {
        public static HearthstoneGameClientStateDTO AsHearthstoneState(this BaseGameClientStateDTO state)
        {
            return state as HearthstoneGameClientStateDTO;
        }

        public static snap.SnapGameClientStateDTO AsSnapState(this BaseGameClientStateDTO state)
        {
            return state as snap.SnapGameClientStateDTO;
        }

        public static mahjong.MahjongGameClientStateDTO AsMahjongState(this BaseGameClientStateDTO state)
        {
            return state as mahjong.MahjongGameClientStateDTO;
        }
    }
}
