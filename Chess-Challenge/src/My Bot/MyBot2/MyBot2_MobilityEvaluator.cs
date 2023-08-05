using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot2_MobilityEvaluator : IChessBot
{
  ExperimentType[] Experiments = new ExperimentType[]
  {
    ExperimentType.MobilityEvaluator,
  };
  int Depth = 2;
  int CapturePriority = 2;
  int WiggleThreshold = 5;
  int IterativeDepth = 0;

  List<Move> PrevMoves = new List<Move>();
  Dictionary<PieceType, int> PieceVal = new Dictionary<PieceType, int>
  {
    {PieceType.None, 0},
    {PieceType.Pawn, 10},
    {PieceType.Rook, 50},
    {PieceType.Knight, 30},
    {PieceType.Bishop, 30},
    {PieceType.Queen, 90},
    {PieceType.King, 1000}
  };
  int CheckMate = 10000;
  int Min = -9999;
  int Max = 9999;

  public static bool Color;

  public Move Think(Board board, Timer timer)
  {
    int color = board.IsWhiteToMove ? 1 : -1;
    Color = board.IsWhiteToMove;

    // Move[] moves = board.GetLegalMoves();
    Move[] moves = _board.GetLegalMoves(board, color);

    Move[] bestMoves = NegaMaxHandler(moves, board, Depth, color);
    bestMoves = Rethink(board, bestMoves, color);

    Random rng = new();
    Move nextMove = bestMoves.Length > 0 ? bestMoves[rng.Next(bestMoves.Length)] : moves[rng.Next(moves.Length)];

    PrevMoves.Insert(0, nextMove);
    if (PrevMoves.Count > WiggleThreshold)
    {
      PrevMoves.RemoveRange(WiggleThreshold, PrevMoves.Count - WiggleThreshold);
    }

    return nextMove;
  }

  Move[] Rethink(Board board, Move[] bestMoves, int color)
  {
    if (IterativeDepth > 0)
    {
      bestMoves = NegaMaxHandler(bestMoves.AsQueryable().Take(5).ToArray(), board, IterativeDepth, color);
    }

    return bestMoves;
  }

  Move[] NegaMaxHandler(Move[] moves, Board board, int depth, int color)
  {
    List<Move> bestMoves = new List<Move>();
    int bestMove = Min;
    int alpha = Min;
    int beta = Max;

    foreach (Move move in moves)
    {
      // Prevents kings from wiggling back and forth
      if (PrevMoves.Find((prevMove) => prevMove.Equals(move)) != Move.NullMove)
      {
        continue;
      }

      // board.MakeMove(move);
      // _board.MakeMove(board, color, move, Depth, depth);

      // var (score, mod) = EvaluateMove(board, move);
      // if (score == 0) score = -NegaMax(depth, board, alpha, beta, -color);

      // // board.UndoMove(move);
      // _board.UndoMove(board, move, mod, Depth, depth, color, score);
      var (score, mod) = MakeMove(board, move, Depth, alpha, beta, color);
      _board.NegaMaxClosingReport(score, color, bestMove);

      int modScore = score + mod;

      // The move list always favors the king since it's calculated first, this way we can randomly select from all equal moves
      if (modScore == bestMove)
      {
        bestMoves.Add(move);
      }

      else if (modScore > bestMove)
      {
        bestMove = modScore;
        bestMoves.Clear();
        bestMoves.Add(move);
        if (score == CheckMate) break;
      }

      alpha = Math.Max(bestMove, alpha);
    }

    Console.WriteLine("Best turn outcome - {0}", bestMove);
    return bestMoves.ToArray();
  }

  (int score, int mod) MakeMove(Board board, Move move, int depth, int alpha, int beta, int color)
  {
    // board.MakeMove(move);
    _board.MakeMove(board, color, move, Depth, depth);

    var (score, mod) = EvaluateMove(board, move);
    if (score == 0) score = depth == Depth
      ? -NegaMax(depth, board, alpha, beta, -color)
      : -NegaMax(depth - 1, board, -beta, -alpha, -color);

    // board.UndoMove(move);
    _board.UndoMove(board, move, Depth, depth, color, score);

    return (score, mod);
  }

  int NegaMax(int depth, Board board, int alpha, int beta, int color)
  {
    _board.NegaMaxStartingReport(board, Depth, depth, color);
    if (depth == 0)
    {
      int material = color * EvaluateBoard(board);
      int mod = color * Juice.GetJuice(Experiments, board);
      return material + mod;
    }

    Move[] nextMoves = board.GetLegalMoves();
    int bestMove = Min;

    foreach (Move move in nextMoves)
    {
      // board.MakeMove(move);
      _board.MakeMove(board, color, move, Depth, depth);

      var (score, mod) = EvaluateMove(board, move);
      if (score == 0) score = -NegaMax(depth - 1, board, -beta, -alpha, -color);

      // board.UndoMove(move);
      _board.UndoMove(board, move, Depth, depth, color, score);

      int modScore = score + mod;
      if (modScore >= beta) return modScore;

      bestMove = Math.Max(bestMove, modScore);
      alpha = Math.Max(bestMove, alpha);
    }

    return bestMove;
  }

  (int score, int mod) EvaluateMove(Board board, Move move)
  {
    if (board.IsInCheckmate())
    {
      return (CheckMate, 0);
    }

    int mod = Juice.GetJuice(Experiments, board, move);
    return (0, (CapturePriority * PieceVal[move.CapturePieceType]) + mod);
  }

  int EvaluateBoard(Board board)
  {
    PieceList[] pieceList = board.GetAllPieceLists();
    int material = 0;

    foreach (PieceList pieces in pieceList)
    {
      foreach (Piece piece in pieces)
      {
        if (piece.IsWhite)
        {
          material += EvaluatePiece(piece);
        }
        else
        {
          material -= EvaluatePiece(piece);
        }
      }
    }

    return material;
  }

  int EvaluatePiece(Piece piece)
  {
    return Experiments.Length > 0 ? Juice.GetJuice(Experiments, piece) : PieceVal[piece.PieceType];
  }
}