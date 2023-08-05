using System;
using ChessChallenge.API;

public class MyBot3 : MyBot3_Base
{
  public MyBot3()
  {
    experiments = new ExperimentType[] { };

    Depth = 3;
    UseTT = true;
    UseQuiescence = true;
    QuiescenceHardPlyLimit = 5;
    MoveSort = "module";
    // UseKillerMoves = true;
    // EndGameDeepening = false;
    // WiggleThreshold = 3;

    Logging = false;
  }

  public override Move Think(Board board, Timer timer)
  {
    Color = board.IsWhiteToMove ? 1 : -1;

    Move[] moves = board.GetLegalMoves();
    Log_GetLegalMoves(Color);

    Move[] bestMoves = NegaMaxRoot(moves, board, Depth, Color, UseMTD);
    // bestMoves = InternalIterativeDeepening(bestMoves, board, Color);
    Console.WriteLine("Nodes searched: {0}", Nodes);
    Random rng = new();
    Move nextMove = bestMoves[rng.Next(bestMoves.Length)];

    Log_Move(nextMove.ToString());
    return nextMove;
  }
}