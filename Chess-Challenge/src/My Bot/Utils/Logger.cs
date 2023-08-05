using System;
using ChessChallenge.API;

public partial class MyBot3_Base
{
  protected void Log_GetLegalMoves(int color)
  {
    Nodes = 0;
    if (!Logging) return;

    Console.WriteLine();
    Console.WriteLine("{0} Moves", color == 1 ? "White" : "Black");
    Console.WriteLine();
    Console.WriteLine("-------------------------------");
  }

  void Log_MakeMove(int depth, Move move)
  {
    if (!Logging) return;

    Console.WriteLine();
    Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, '+') + " ????? {2} => {0} {1} => {3} ?????", color == 1 ? "White" : "Black", move.MovePieceType, move.StartSquare.Name, move.TargetSquare.Name);
  }

  void Log_UndoMove(int color, Move move, int score)
  {
    if (!Logging) return;

    if (move.IsCapture)
    {
      Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, Color == 1 ? '+' : '-') + " MOVE HAS CAPTURE: -------- {0}{1} takes {2}", color == 1 ? "w" : "b", move.MovePieceType, move.CapturePieceType);
      Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, Color == 1 ? '+' : '-') + " Current: {0}", score);
    }
  }

  void Log_NegaMaxStartingReport(Board board, int depth, int color)
  {
    if (!Logging) return;

    Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, Color == 1 ? '+' : '-') + " Thinking about {0} ", color == 1 ? "White" : "Black");
    if (depth == 0)
    {
      Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, Color == 1 ? '+' : '-') + " No more moves....");
    }

    if (board.IsInCheckmate())
    {
      Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, Color == 1 ? '+' : '-') + " Board is checkmate, prioritizing... {0}", -color == 1 ? "White" : "Black");
    }

    if (board.IsInCheck())
    {
      Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, Color == 1 ? '+' : '-') + " Board is check, prioritizing...");
    }
  }

  void Log_NegaMaxClosingReport(int score, int color, int bestScore)
  {
    if (!Logging) return;

    Console.WriteLine("Best board outcome: {0} - {1}", score, color == 1 ? 'w' : 'b');
    if (score == bestScore)
    {
      Console.WriteLine("Adding to collection");
    }

    else if (score > bestScore)
    {
      Console.WriteLine("Clearing worser moves");
    }

    Console.WriteLine("----------");
    Console.WriteLine();
  }

  void Log_Outcome(int bestMove)
  {
    if (!Logging) return;

    Console.Write("Best turn outcome - {0} - Nodes: {1} - ", bestMove, Nodes);
  }

  protected void Log_Move(string move)
  {
    if (!Logging) return;

    Console.Write(move);
    Console.WriteLine();
  }
}