using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot2_MTD_MoveSorting_1_D2 : IChessBot
{
  ExperimentType[] Experiments = new ExperimentType[]
  {
  };
  int Depth = 3;
  public static int CapturePriority = 2;
  int WiggleThreshold = 15;
  int IterativeDepth = 0;

  int CheckMate = 10000;
  int Min = -9999;
  int Max = 9999;

  public static bool Color;

  int BestGuess = 0;
  bool UseMTD = true;

  public MyBot2_MTD_MoveSorting_1_D2()
  {
  }

  public Move Think(Board board, Timer timer)
  {
    int color = board.IsWhiteToMove ? 1 : -1;
    Color = board.IsWhiteToMove;

    // Move[] moves = board.GetLegalMoves();
    Move[] moves = _board.GetLegalMoves(board, color);
    Move[] orderedMoves = NegaMaxHandler(moves, board, depth: 2, color); // Order the moves
    Move[] bestMoves = NegaMaxHandler(orderedMoves, board, Depth, color, UseMTD);

    // Deep thinking
    bestMoves = IterativeDepth > 0
      ? NegaMaxHandler(bestMoves.AsQueryable().Take(5).ToArray(), board, IterativeDepth, color)
      : bestMoves;

    Random rng = new();
    Move nextMove = bestMoves.Length > 0
      ? bestMoves[rng.Next(bestMoves.Length)]
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
      _board.NegaMaxClosingReport(score, color, bestMove);

      // The move list always favors the king since it's calculated first, this way we can randomly select from all equal moves
      if (score == bestMove)
      {
        bestMoves.Add(move);
      }

      else if (score > bestMove)
      {
        bestMove = score;
        bestMoves.Clear();
        bestMoves.Add(move);
        if (score == CheckMate) break;
      }

      alpha = Math.Max(bestMove, alpha);
    }

    Console.WriteLine("Best turn outcome - {0}", bestMove);
    BestGuess = bestMove;
    return bestMoves.ToArray();
  }

  int MakeMove(Board board, Move move, int depth, int alpha, int beta, int color, bool isRoot = false, bool useMTD = false)
  {
    // board.MakeMove(move);
    _board.MakeMove(board, color, move, Depth, depth);

    if (board.IsInCheckmate())
    {
      _board.UndoMove(board, move, Depth, depth, color, CheckMate);
      return CheckMate;
    }

    // So the root node and child nodes can share code
    int score = isRoot
      ? useMTD // root search
        ? -MTD(depth, board, BestGuess, color)
        : -NegaMax(depth, board, alpha, beta, -color)
      : -NegaMax(depth, board, -beta, -alpha, -color);

    // board.UndoMove(move);
    _board.UndoMove(board, move, Depth, depth, color, score);

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
    _board.NegaMaxStartingReport(board, Depth, depth, color);
    if (depth == 0)
    {
      return color * EvaluateBoard(board);
    }

    Move[] nextMoves = board.GetLegalMoves();
    int score = Min;

    foreach (Move move in nextMoves)
    {
      score = Math.Max(score, MakeMove(board, move, depth - 1, alpha, beta, color));
      alpha = Math.Max(alpha, score);

      if (alpha >= beta) break;
    }

    return score;
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
        {
          materialVal += EvaluatePiece(piece);
        }
        else
        {
          materialVal -= EvaluatePiece(piece);
        }
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