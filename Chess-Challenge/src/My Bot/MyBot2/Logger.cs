using System;
using ChessChallenge.API;

public static class _board
{
  public static Move[] GetLegalMoves(Board board, int color)
  {
    // Console.WriteLine();
    // Console.WriteLine("{0} Moves", color == 1 ? "White" : "Black");
    // Console.WriteLine();
    // Console.WriteLine("-------------------------------");

    return board.GetLegalMoves();
  }

  public static void MakeMove(Board board, int color, Move move, int Depth, int depth)
  {
    // Console.WriteLine();
    // Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, '+ ') + "????? {2} => {0} {1} => {3} ?????", color == 1 ? "White" : "Black", move.MovePieceType, move.StartSquare.Name, move.TargetSquare.Name);

    board.MakeMove(move);
  }

  public static void UndoMove(Board board, Move move, int Depth, int depth, int color, int score)
  {
    // if (mod > 0)
    // {
    //   Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, MyBot.Color ? '+' : '-') + " MOVE HAS CAPTURE: -------- {0}{1} takes {2}", color == 1 ? "w" : "b", move.MovePieceType, move.CapturePieceType);
    //   Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, MyBot.Color ? '+' : '-') + " Current: {0}, Modded: {1}", score, score + mod);
    // }

    board.UndoMove(move);
  }

  public static void NegaMaxStartingReport(Board board, int Depth, int depth, int color)
  {
    // Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, MyBot.Color? '+' : '-') + " Thinking about {0} ", color == 1 ? "White" : "Black");
    // if (depth == 0)
    // {
    //   Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, MyBot.Color ? '+' : '-') + " No more moves....");
    // }

    // if (board.IsInCheckmate())
    // {
    //   Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, MyBot.Color ? '+' : '-') + " Board is checkmate, prioritizing... {0}", -color == 1 ? "White" : "Black");
    // }

    // if (board.IsInCheck())
    // {
    //   Console.WriteLine("".PadLeft((1 + Depth - depth) * 2, MyBot.Color ? '+' : '-') + " Board is check, prioritizing...");
    // }
  }

  public static void NegaMaxClosingReport(int score, int color, int bestMove)
  {
    // Console.WriteLine("Best board outcome: {0} - {1}", score, color == 1 ? 'w' : 'b');
    // if (score == bestMove)
    // {
    //   Console.WriteLine("Adding to collection");
    // }

    // else if (score > bestMove)
    // {
    //   Console.WriteLine("Clearing worser moves");
    // }

    // Console.WriteLine("----------");
    // Console.WriteLine();
  }
}