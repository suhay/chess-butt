using System;
using System.Collections.Generic;
using ChessChallenge.API;

public partial class MyBot3_Base
{
  protected Move NegaMaxIterativeDeepening(Board board, int depth, int color, bool useMTD = false)
  {
    // Repetition history clears when a pawn moves or a capture is made. We can safely clear the transposition table
    if (board.GameRepetitionHistory.Length == 0)
      transpositionTable.Clear();

    for (int i = 1; i <= depth; i++)
    {
      NegaMax(i, board, Min, Max, color);
    }

    // need to pull from the pv_table
    return new Move();
  }

  protected Move[] NegaMaxRoot(Move[] moves, Board board, int depth, int color, bool useMTD = false)
  {
    Ply = 0;
    Nodes = 0;
    // Repetition history clears when a pawn moves or a capture is made. We can safely clear the transposition table
    if (board.GameRepetitionHistory.Length == 0)
      transpositionTable.Clear();

    if (moves.Length <= 1)
      return moves;

    KillerMoves.Clear();

    Move[] orderedMoves = SortMoves(moves, board, color);

    // if (EndGameDeepening)
    // {
    //   if (board.PlyCount > 60)
    //   {
    //     PieceList[] pieceList = board.GetAllPieceLists();
    //     int numPieces = 0;
    //     Array.ForEach(pieceList, pieces => numPieces += pieces.Count);
    //     if (numPieces <= 15)
    //     {
    //       depth += 2;
    //     }
    //   }
    // }

    List<Move> bestMoves = new List<Move>();
    int bestScore = Min;
    int alpha = Min;
    int beta = Max;

    foreach (Move move in orderedMoves) // root move
    {
      // Prevents kings from wiggling back and forth. But if we have no choice...
      if (orderedMoves.Length > 2 && Wiggling(board, move))
        continue;

      int score = MakeMove(board, move, depth, alpha, beta, color, isRoot: true, useMTD); // make root move
      Log_NegaMaxClosingReport(score, color, bestScore);

      // The move list always favors the king since it's calculated first, this way we can randomly select from all equal moves
      if (score == bestScore)
        bestMoves.Add(move);

      else if (score > bestScore)
      {
        bestScore = score;
        bestMoves.Clear();
        bestMoves.Add(move);

        if (score == CheckMate)
          break;
      }

      alpha = Math.Max(bestScore, alpha);
    }

    Log_Outcome(bestScore);
    BestGuess = bestScore;

    // If we found nothing, then we'll just randomly pick between what we do have.
    if (bestMoves.Count == 0)
      return orderedMoves;

    return bestMoves.ToArray();
  }

  int NegaMaxWithTransposition(int depth, Board board, int alpha, int beta, int color)
  {
    // I need to face them off
    // Depth - depth, depth: 3 == 19:76:4-1 == Cache Hits: ~80 at best, fewer hits, but bigger when there were
    // depth, depth: 3 == 17:76:7 == Cache Hits: ~20 at best

    // A = MyBot3 = Depth - depth
    // B = MyBot_Default = depth
    // depth 3: == 4+2:88:5-1

    int oldAlpha = alpha;
    ulong key = board.ZobristKey;

    int? entry = transpositionTable.Get(key, UseTT2 ? depth : Depth - depth, alpha, beta);
    if (entry.HasValue)
      return (int)entry;

    int bestScore = NegaMax(depth, board, alpha, beta, color);

    transpositionTable.Store(key, bestScore, oldAlpha, beta, UseTT2 ? depth : Depth - depth);
    return bestScore;
  }

  int NegaMax(
    int depth,
    Board board,
    int alpha,
    int beta,
    int color
  // TranspositionTable? table = null,
  // int? key = null,
  // int? oldAlpha = null
  )
  {
    Log_NegaMaxStartingReport(board, depth, color);

    if (depth == 0)
      return Evaluate(board, alpha, beta, color);

    if (board.IsInCheck()) // We want to keep 
      depth++;

    Move[] nextMoves = board.GetLegalMoves();
    Move[] orderedMoves = SortMoves(nextMoves, board, color);

    int bestScore = Min;
    foreach (Move move in orderedMoves)
    {
      if (orderedMoves.Length > 2 && ThreefoldRepetition(board))
        continue;

      int score = MakeMove(board, move, depth - 1, alpha, beta, color);

      if (score >= beta)
      {
        if (UseKillerMoves)
        {
          if (!move.IsCapture)
            KillerMoves.Store(move, board, Ply);
        }

        // if (FailHard)
        //   return beta;

        return score;
      }

      if (score > bestScore)
      {
        // if (key.HasValue && oldAlpha.HasValue)
        //   table?.Store(key, score, oldAlpha, beta, Ply);

        bestScore = score;
        alpha = Math.Max(alpha, score);
      }
    }

    return bestScore;
  }
}