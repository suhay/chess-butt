using System.Collections.Generic;
using System.Linq;
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

  protected Move[] NegaMaxRoot(Board board, int depth, int alpha, int beta, int color, bool useMTD = false)
  {
    Nodes = 0;
    IsInEndGame = !IsInEndGame
      && board.PlyCount > 60
      && board.GetAllPieceLists().Sum((pieceList) => pieceList.Count()) <= 8;

    // Repetition history clears when a pawn moves or a capture is made. We can safely clear the transposition table
    if (board.GameRepetitionHistory.Length == 0)
      transpositionTable.Clear();

    Move[] moves = board.GetLegalMoves();

    if (moves.Length <= 1)
      return moves;

    KillerMoves.Clear();

    // If the opponent did not make the move that our PV Table has next, the table is not viable so clear it
    if (PVTable.ContainsKey(board.PlyCount - 1))
    {
      if (board.GameMoveHistory.Last() != PVTable[board.PlyCount - 1])
        PVTable.Clear();
    }

    Move[] orderedMoves = SortMoves(moves, board, isRoot: true);

    if (IsInEndGame && EndGameDeepening)
      depth += 2;

    List<Move> bestMoves = new();

    int score;
    int bestScore = alpha;
    foreach (Move move in orderedMoves) // root move
    {
      // Prevents kings from wiggling back and forth. But if we have no choice...
      if (orderedMoves.Length > 2 && Wiggling(board, move))
        continue;

      score = MakeMove(board, move, depth, alpha, beta, color, useMTD); // make root move
      Log_NegaMaxClosingReport(score, color, bestScore);

      if (score == bestScore)
        bestMoves.Add(move);

      else if (score > bestScore)
      {
        bestScore = score;
        movesToScore[board.PlyCount] = move;
        bestMoves.Clear();
        bestMoves.Add(move);

        if (score == CheckMate)
          break;
      }
    }

    Log_Outcome(bestScore);
    BestGuess = bestScore;

    // If we found nothing, then we'll just randomly pick between what we do have.
    if (bestMoves.Count == 0)
      return orderedMoves;

    return bestMoves.ToArray();
  }
}