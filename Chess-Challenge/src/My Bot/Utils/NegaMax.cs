using ChessChallenge.API;

public partial class MyBot3_Base
{
  int NegaMax(int depth, Board board, int alpha, int beta, int color)
  {
    Log_NegaMaxStartingReport(board, depth, color);
    ulong key = board.ZobristKey;
    int flag = 1;

    if (UseTT)
    {
      int? entry = transpositionTable.Get(key, UseTT2 ? depth : Depth - depth, alpha, beta, board.PlyCount);
      if (entry != null)
        return (int)entry;
    }

    if (depth == 0)
    {
      int val = Evaluate(board, alpha, beta, color);
      transpositionTable.Store(key, val, UseTT2 ? depth : Depth - depth, flag: 0, board.PlyCount);
      return val;
    }

    if (board.IsInCheck()) // We want to keep searching
      depth++;

    Move[] nextMoves = board.GetLegalMoves();
    Move[] orderedMoves = SortMoves(nextMoves, board);

    foreach (Move move in orderedMoves)
    {
      if (orderedMoves.Length > 2 && ThreefoldRepetition(board))
        continue;

      int score = MakeMove(board, move, depth - 1, alpha, beta, color);

      if (score >= beta)
      {
        if (!move.IsCapture)
          KillerMoves.Store(move, board.PlyCount);

        transpositionTable.Store(key, FailHard ? beta : score, UseTT2 ? depth : Depth - depth, flag: 2, board.PlyCount);

        if (FailHard)
          return beta;

        return score;
      }

      if (score > alpha)
      {
        flag = 0;
        alpha = score;
      }
    }

    transpositionTable.Store(key, alpha, UseTT2 ? depth : Depth - depth, flag, board.PlyCount);
    return alpha;
  }
}