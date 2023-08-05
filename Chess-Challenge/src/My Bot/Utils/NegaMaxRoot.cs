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
      NegaMax(i, board, -Inf, Inf, color);
    }

    // need to pull from the pv_table
    return new Move();
  }

  protected Move[] NegaMaxRoot(Move[] moves, Board board, int depth, int color, bool useMTD = false)
  {
    Nodes = 0;
    // Repetition history clears when a pawn moves or a capture is made. We can safely clear the transposition table
    if (board.GameRepetitionHistory.Length == 0)
      transpositionTable.Clear();

    if (moves.Length <= 1)
      return moves;

    KillerMoves.Clear();

    Move[] orderedMoves = SortMoves(moves, board);

    if (EndGameDeepening)
    {
      if (board.PlyCount > 60)
      {
        PieceList[] pieceList = board.GetAllPieceLists();
        int numPieces = 0;
        Array.ForEach(pieceList, pieces => numPieces += pieces.Count);
        if (numPieces <= 15)
          depth += 2;
      }
    }

    List<Move> bestMoves = new();
    int bestScore = -Inf;
    int alpha = -Inf;
    int beta = Inf;

    foreach (Move move in orderedMoves) // root move
    {
      // Prevents kings from wiggling back and forth. But if we have no choice...
      if (orderedMoves.Length > 2 && Wiggling(board, move))
        continue;

      if (orderedMoves.Length <= 20 && board.PlyCount > 50)
      {
        Console.WriteLine();
      }

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

        // Checkmate in one, we're done
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
}