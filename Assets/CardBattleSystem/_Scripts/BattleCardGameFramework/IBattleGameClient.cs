using System.Threading.Tasks;

namespace BattleCardGameFramework
{
    public interface IBattleGameClient
    {
        Task<BaseGameClientStateDTO> GetPlayerState();
        Task<BaseGameClientStateDTO> PlayCard(int cardId, string targetId = "");
        Task<BaseGameClientStateDTO> Attack(int attackerCardId, string targetId);
        Task<BaseGameClientStateDTO> EndTurn();
    }
}
