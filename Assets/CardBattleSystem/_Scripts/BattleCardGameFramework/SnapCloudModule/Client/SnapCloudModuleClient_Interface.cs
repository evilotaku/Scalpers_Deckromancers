using System.Threading.Tasks;
using BattleCardGameFramework;
using Newtonsoft.Json;

public partial class SnapCloudModuleClient : IBattleGameClient
{
    private static readonly System.Type s_DTOType = typeof(BaseGameClientStateDTO).Assembly.GetType("snap.SnapGameClientStateDTO");

    async Task<BaseGameClientStateDTO> IBattleGameClient.GetPlayerState()
    {
        var result = await GetPlayerState(null);
        return (BaseGameClientStateDTO)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result), s_DTOType);
    }

    async Task<BaseGameClientStateDTO> IBattleGameClient.PlayCard(int cardId, string targetId)
    {
        int locationIndex = 0;
        int.TryParse(targetId, out locationIndex);
        var result = await PlayCard(null, cardId, locationIndex);
        return (BaseGameClientStateDTO)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result), s_DTOType);
    }

    Task<BaseGameClientStateDTO> IBattleGameClient.Attack(int attackerCardId, string targetId)
    {
        throw new System.NotSupportedException("Snap does not support direct Attack action");
    }

    async Task<BaseGameClientStateDTO> IBattleGameClient.EndTurn()
    {
        var result = await SubmitTurn(null);
        return (BaseGameClientStateDTO)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(result), s_DTOType);
    }
}




