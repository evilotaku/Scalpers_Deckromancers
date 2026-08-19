using System.Threading.Tasks;
using BattleCardGameFramework;
using Newtonsoft.Json;

public partial class MahjongCloudModuleClient : IBattleGameClient
{
    private static readonly System.Type s_DTOType = typeof(BaseGameClientStateDTO).Assembly.GetType("mahjong.MahjongGameClientStateDTO");

    async Task<BaseGameClientStateDTO> IBattleGameClient.GetPlayerState()
    {
        var result = await GetPlayerState(null);
        return (BaseGameClientStateDTO)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result), s_DTOType);
    }

    async Task<BaseGameClientStateDTO> IBattleGameClient.PlayCard(int cardId, string targetId)
    {
        var result = await DiscardTile(null, cardId);
        return (BaseGameClientStateDTO)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result), s_DTOType);
    }

    Task<BaseGameClientStateDTO> IBattleGameClient.Attack(int attackerCardId, string targetId)
    {
        throw new System.NotSupportedException("Mahjong does not support Attack action");
    }

    Task<BaseGameClientStateDTO> IBattleGameClient.EndTurn()
    {
        throw new System.NotSupportedException("Mahjong does not have a separate EndTurn action; it happens automatically after discard");
    }
}


