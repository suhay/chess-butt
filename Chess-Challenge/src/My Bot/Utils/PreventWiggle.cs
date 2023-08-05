using System;
using System.Linq;
using ChessChallenge.API;

public partial class MyBot3_Base
{
  protected bool Wiggling(Board board, Move move)
  {
    // Prevent kings wiggling around
    if (board.GameMoveHistory
        .TakeLast(WiggleThreshold)
        .FirstOrDefault((prevMove) => prevMove.Equals(move), new Move()) != Move.NullMove)
    {
      return true;
    }

    return false;
  }

  protected bool ThreefoldRepetition(Board board)
  {
    int repCount = board.GameRepetitionHistory.Count(x => x == board.ZobristKey);

    // Prevent threefold repetition to keep the game going
    if (repCount >= 2)
    {
      return true;
    }

    return false;
  }
}