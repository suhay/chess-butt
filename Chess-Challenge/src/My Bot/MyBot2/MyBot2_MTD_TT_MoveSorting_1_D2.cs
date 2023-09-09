using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot2_MTD_TT_MoveSorting_1_D2 : IChessBot
{
  ExperimentType[] Experiments = new ExperimentType[]
  {
  };
  int Depth = 2;
  public static int CapturePriority = 2;
  int WiggleThreshold = 15;
  int IterativeDepth = 0;

  int CheckMate = 10000;
  int Min = -9999;
  int Max = 9999;

  public static bool Color;

  int BestGuess = 0;
  bool UseMTD = false;
  bool UseTT = false;
  TT.TranspositionTable TranspositionTable = new TT.TranspositionTable();

  bool UseMoveOrder1 = false;
  int MoveOrder1Depth = 2;
  bool UseMoveOrder2 = false;
  int MoveOrder2Depth = 2;

  bool UseQuiescence = false;

  public MyBot2_MTD_TT_MoveSorting_1_D2()
  {
    TranspositionTable.Clear();
    BestGuess = 0;
  }

  public Move Think(Board board, Timer timer)
  {
    // Repetition history clears when a pawn moves or capture is made. We can safely clear the transposition table
    if (board.GameRepetitionHistory.Length == 0)
      TranspositionTable.Clear();

    int color = board.IsWhiteToMove ? 1 : -1;
    Color = board.IsWhiteToMove;

    Move[] moves = board.GetLegalMoves();
    Move[] orderedMoves = UseMoveOrder1
      ? NegaMaxHandler(moves, board, MoveOrder1Depth, color)
      : moves; // Order the moves
    Move[] bestMoves = orderedMoves;

    // Internal Iterative Deepening
    if (orderedMoves.Length > 1)
    {
      bestMoves = NegaMaxHandler(orderedMoves, board, Depth, color, UseMTD);
      bestMoves = IterativeDepth > 0
        ? NegaMaxHandler(bestMoves.AsQueryable().Take(5).ToArray(), board, IterativeDepth, color, UseMTD)
        : bestMoves;
    }

    Random rng = new();
    Move nextMove = bestMoves.Length > 0
      ? bestMoves[rng.Next(bestMoves.Length)]
      : orderedMoves.Length > 0
        ? orderedMoves[rng.Next(orderedMoves.Length)]
        : moves[rng.Next(moves.Length)];

    return nextMove;
  }

  // Generates root node
  Move[] NegaMaxHandler(Move[] moves, Board board, int depth, int color, bool useMTD = false)
  {
    List<Move> bestMoves = new List<Move>();
    int bestMove = Min;
    int alpha = Min;
    int beta = Max;

    foreach (Move move in moves) // root move
    {
      // Prevents kings from wiggling back and forth
      if (!board.GameMoveHistory
        .Take(WiggleThreshold)
        .FirstOrDefault((prevMove) => prevMove.Equals(move), new Move()).IsNull)
      {
        continue;
      }

      var score = MakeMove(board, move, depth, alpha, beta, color, isRoot: true, useMTD); // make root move

      // The move list always favors the king since it's calculated first, this way we can randomly select from all equal moves
      if (score == bestMove)
        bestMoves.Add(move);

      else if (score > bestMove)
      {
        bestMove = score;
        bestMoves.Clear();
        bestMoves.Add(move);
        if (score == CheckMate) break;
      }

      alpha = Math.Max(bestMove, alpha);
    }

    BestGuess = bestMove;
    return bestMoves.ToArray();
  }

  int MakeMove(Board board, Move move, int depth, int alpha, int beta, int color, bool isRoot = false, bool useMTD = false)
  {
    board.MakeMove(move);
    if (board.IsInCheckmate())
    {
      board.UndoMove(move);
      return CheckMate;
    }

    // So the root node and child nodes can share code
    int score = isRoot
      ? useMTD // root search
        ? -MTD(depth, board, BestGuess, color)
        : -NegaMax(depth, board, alpha, beta, -color)
      : -NegaMax(depth, board, -beta, -alpha, -color);

    board.UndoMove(move);
    return score + (CapturePriority * MyBot2.PieceVal[move.CapturePieceType]);
  }

  int MTD(int depth, Board board, int guess, int color)
  {
    int upperbound = Max;
    int lowerbound = Min;

    while (lowerbound < upperbound)
    {
      int beta = Math.Max(guess, lowerbound + 1);
      guess = NegaMax(depth, board, beta - 1, beta, color);

      if (guess < beta)
        upperbound = guess;
      else
        lowerbound = guess;
    }

    return guess;
  }

  int NegaMax(int depth, Board board, int alpha, int beta, int color)
  {
    int oldAlpha = alpha;
    ulong key = board.ZobristKey;

    if (UseTT)
    {
      int? entry = TranspositionTable.Get(key, depth, alpha, beta, 0);
      if (entry.HasValue)
        return (int)entry;
    }

    _board.NegaMaxStartingReport(board, Depth, depth, color);
    if (depth == 0)
    {
      if (UseQuiescence)
      {
        return color * Quiescence(board, alpha, beta);
      }
      return color * EvaluateBoard(board);
    }

    Move[] nextMoves = board.GetLegalMoves();
    Move[] orderedMoves = UseMoveOrder2
      ? NegaMaxHandler(nextMoves, board, MoveOrder2Depth, color)
      : nextMoves; // Order the moves

    int bestScore = Min;

    foreach (Move move in orderedMoves)
    {
      int score = MakeMove(board, move, depth - 1, alpha, beta, color);

      if (score >= beta)
      {
        TranspositionTable.Store(key, score, depth, 2, 0);
        return score;
      }

      if (score > bestScore)
      {
        TranspositionTable.Store(key, score, depth, 1, 0);
        bestScore = score;
        if (score > alpha)
          alpha = score;
      }
    }

    TranspositionTable.Store(key, bestScore, depth, 0, 0);
    return bestScore;
  }

  int Quiescence(Board board, int alpha, int beta)
  {
    int eval = EvaluateBoard(board);

    if (eval >= beta)
      return beta;

    if (eval > alpha)
      alpha = eval;

    Move[] captureMoves = board.GetLegalMoves(true);
    foreach (Move move in captureMoves)
    {
      board.MakeMove(move);
      int score = -Quiescence(board, -beta, -alpha);
      board.UndoMove(move);

      if (score >= beta)
        return beta;

      if (score > alpha)
        alpha = score;
    }

    return alpha;
  }

  int EvaluateBoard(Board board)
  {
    PieceList[] pieceList = board.GetAllPieceLists();
    int materialVal = 0;

    foreach (PieceList pieces in pieceList)
    {
      foreach (Piece piece in pieces)
      {
        if (piece.IsWhite)
          materialVal += EvaluatePiece(piece);
        else
          materialVal -= EvaluatePiece(piece);
      }
    }

    int boardVal = Juice.GetJuice(Experiments, board);

    return materialVal + boardVal;
  }

  int EvaluatePiece(Piece piece)
  {
    return Juice.GetJuice(Experiments, piece);
  }
}