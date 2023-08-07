using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot3 : MyBot3_Base
{
  public MyBot3()
  {
    experiments = new ExperimentType[] { };

    Depth = 4;
    UseTT = true;
    UseQuiescence = true;
    // QuiescenceHardPlyLimit = 5;
    MoveSort = "module";
    UseKillerMoves = true;
    // UsePV = true;
    UseNullMovePruning = true;
    // EndGameDeepening = true;
    FailHard = true;
    WiggleThreshold = 3;

    Logging = false;
  }

  public override Move Think(Board board, Timer timer)
  {
    Color = board.IsWhiteToMove ? 1 : -1;
    movesToScore.Clear();
    Log_GetLegalMoves(Color);

    Move[] bestMoves = NegaMaxRoot(board, Depth, -Inf, Inf, Color, UseMTD);
    Random rng = new();
    Move nextMove = bestMoves[rng.Next(bestMoves.Length)];

    Console.WriteLine("Nodes: {0} - {1}", Nodes, MoveToString(nextMove, movesToScore));

    Log_Move(nextMove.ToString());
    return nextMove;
  }

  string MoveToString(Move move, Dictionary<int, Move> moves)
  {
    List<int> moveOrder = new(moves.Keys);
    List<string> orderMoves = new();

    moveOrder.Sort();

    foreach (int entry in moveOrder)
    {
      orderMoves.Add(moves[entry].ToString().Replace("Move: ", "").Replace("'", ""));
    }
    return string.Join(" ", orderMoves);
  }
}

