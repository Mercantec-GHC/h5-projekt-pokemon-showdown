using Microsoft.AspNetCore.Mvc;
using PokemonShowdown.Api.Models;
using PokemonShowdown.Api.Services;

namespace PokemonShowdown.Api.Controllers;

[ApiController]
[Route("game")]
public sealed class GameController : ControllerBase
{
    private readonly PokemonDataService _data;
    private readonly BattleStore _store;
    private readonly BattleEngine _engine;

    public GameController(PokemonDataService data, BattleStore store, BattleEngine engine)
    {
        _data = data;
        _store = store;
        _engine = engine;
    }

    [HttpPost("start")]
    public async Task<ActionResult<StartBattleResponse>> Start([FromBody] StartBattleRequest? request, CancellationToken cancellationToken)
    {
        var body = request ?? new StartBattleRequest();
        var details = await _data.GetDetailsAsync(cancellationToken);

        var state = _engine.CreateBattle(details, body.TeamSize, body.MovesPerPokemon);
        _store.Create(state);

        return Ok(new StartBattleResponse
        {
            BattleId = state.BattleId,
            State = state
        });
    }

    [HttpPost("turn")]
    public ActionResult<ResolveTurnResponse> Turn([FromBody] TurnRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BattleId))
        {
            return BadRequest(new { error = "battleId is required" });
        }

        if (request.Type is not ("move" or "switch"))
        {
            return BadRequest(new { error = "type must be 'move' or 'switch'" });
        }

        if (request.Type == "move" && string.IsNullOrWhiteSpace(request.MoveName))
        {
            return BadRequest(new { error = "moveName is required for move action" });
        }

        if (request.Type == "switch" && request.SwitchIndex is null)
        {
            return BadRequest(new { error = "switchIndex is required for switch action" });
        }

        var state = _store.Get(request.BattleId);
        if (state is null)
        {
            return NotFound(new { error = "Battle not found" });
        }

        var result = _engine.ResolveTurn(state, request);
        _store.Update(result.State);

        return Ok(result);
    }

    [HttpGet("state")]
    public ActionResult<StateResponse> State([FromQuery] string battleId)
    {
        if (string.IsNullOrWhiteSpace(battleId))
        {
            return BadRequest(new { error = "battleId is required" });
        }

        var state = _store.Get(battleId);
        if (state is null)
        {
            return NotFound(new { error = "Battle not found" });
        }

        return Ok(new StateResponse { State = state });
    }


}
