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
      Console.WriteLine("Check wiggle in game history: {0}", move.ToString());
      return true;
    }

    return false;
  }

  protected bool ThreefoldRepetition(Board board)
  {
    int repCount = board.GameRepetitionHistory.Count((x => x == board.ZobristKey));

    // Prevent threefold repetition to keep the game going
    if (repCount >= 2)
    {
      Console.WriteLine("Check wiggle in game repetition history");
      return true;
    }

    return false;
  }
}