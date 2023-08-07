using ChessChallenge.API;

public partial class MyBot3_Base
{
  int NegaMax(int depth, Board board, int alpha, int beta, int color)
  {
    Log_NegaMaxStartingReport(board, depth, color);
    ulong key = board.ZobristKey;
    int flag = 1;
    bool foundPV = false;

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

    if (UseNullMovePruning && !IsInEndGame && depth >= 3 && !board.IsInCheck())
    {
      board.TrySkipTurn();
      int nullScore = -NegaMax(depth - 1 - 2, board, -beta, -beta + 1, -color);
      board.UndoSkipTurn();
      if (nullScore >= beta)
        return beta;
    }

    // if (board.IsInCheck()) // We want to keep searching
    //   depth++;

    Move[] nextMoves = board.GetLegalMoves();
    Move[] orderedMoves = SortMoves(nextMoves, board);

    int score;
    foreach (Move move in orderedMoves)
    {
      if (orderedMoves.Length > 2 && ThreefoldRepetition(board))
        continue;

      if (foundPV && UsePV) // Found PV, try and beat it
      {
        score = MakeMove(board, move, depth - 1, alpha, alpha + 1, color);
        if (score > alpha && score < beta) // OK, I was wrong, keep searching...
          score = MakeMove(board, move, depth - 1, alpha, beta, color);
      }

      else
        score = MakeMove(board, move, depth - 1, alpha, beta, color);

      if (score >= beta)
      {
        if (!move.IsCapture)
          KillerMoves.Store(move, board.PlyCount);

        transpositionTable.Store(key, FailHard ? beta : score, UseTT2 ? depth : Depth - depth, flag: 2, board.PlyCount);

        return FailHard ? beta : score;
      }

      if (score > alpha)
      {
        flag = 0;
        alpha = score;
        foundPV = true;
        PVTable[board.PlyCount] = move;
        movesToScore[board.PlyCount] = move;
      }
    }

    transpositionTable.Store(key, alpha, UseTT2 ? depth : Depth - depth, flag, board.PlyCount);
    return alpha;
  }
}