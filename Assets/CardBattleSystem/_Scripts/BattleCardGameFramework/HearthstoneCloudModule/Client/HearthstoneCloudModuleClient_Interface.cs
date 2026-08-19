using System.Threading.Tasks;
using BattleCardGameFramework;
using Newtonsoft.Json;

public partial class HearthstoneCloudModuleClient : IBattleGameClient
{
    private static readonly System.Type s_DTOType = typeof(BaseGameClientStateDTO).Assembly.GetType("BattleCardGameFramework.HearthstoneGameClientStateDTO");

    async Task<BaseGameClientStateDTO> IBattleGameClient.GetPlayerState()
    {
        var result = await GetPlayerState(null);
        return (BaseGameClientStateDTO)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result), s_DTOType);
    }

    async Task<BaseGameClientStateDTO> IBattleGameClient.PlayCard(int cardId, string targetId)
    {
        var result = await PlayCard(null, cardId, targetId);
        return (BaseGameClientStateDTO)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result), s_DTOType);
    }

    async Task<BaseGameClientStateDTO> IBattleGameClient.Attack(int attackerCardId, string targetId)
    {
        var result = await Attack(null, attackerCardId, targetId);
        return (BaseGameClientStateDTO)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result), s_DTOType);
    }

    async Task<BaseGameClientStateDTO> IBattleGameClient.EndTurn()
    {
        var result = await EndTurn(null);
        return (BaseGameClientStateDTO)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result), s_DTOType);
    }
}






