using System;
using ChessChallenge.API;

public class MyBot3_Default : MyBot3_Base
{
  public MyBot3_Default()
  {
    Depth = 2;
    experiments = new ExperimentType[] { };
  }

  public override Move Think(Board board, Timer timer)
  {
    Color = board.IsWhiteToMove ? 1 : -1;

    Move[] bestMoves = NegaMaxRoot(board, Depth, -Inf, Inf, Color, UseMTD);

    Random rng = new();
    Move nextMove = bestMoves[rng.Next(bestMoves.Length)];

    Log_Move(nextMove.ToString());
    return nextMove;
  }
}