using ChessChallenge.API;

public partial class MyBot3_Base
{
  int MakeMove(Board board, Move move, int depth, int alpha, int beta, int color, bool isRoot = false, bool useMTD = false)
  {
    Nodes++;
    Ply++;
    board.MakeMove(move);
    Log_MakeMove(depth, move);

    if (board.IsInCheckmate())
    {
      Ply--;
      board.UndoMove(move);
      Log_UndoMove(color, move, CheckMate);
      return CheckMate;
    }

    int score;

    // So the root node and child nodes can share code
    if (isRoot)
    {
      if (useMTD)
        score = -MTD(depth, board, BestGuess, color);
      else
      {
        if (UseTT)
          score = -NegaMaxWithTransposition(depth, board, alpha, beta, -color);
        else
          score = -NegaMax(depth, board, alpha, beta, -color);
      }
    }
    else
      score = -NegaMax(depth, board, -beta, -alpha, -color);

    Ply--;
    board.UndoMove(move);
    Log_UndoMove(color, move, score);

    return score + (CapturePriority * MyBot.PieceVal[move.CapturePieceType]);
  }
}