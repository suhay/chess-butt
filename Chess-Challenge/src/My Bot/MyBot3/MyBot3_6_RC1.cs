using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

namespace MyBot3_6_RC1
{
  public struct Transposition
  {
    public Transposition(int score, int depth, int flag)
    {
      Depth = depth;
      Score = score;
      Flag = flag;
    }

    public int Depth { get; }
    public int Score { get; }
    /// <summary>
    /// 0 = Exact, 1 = Alpha, 2 = Beta
    /// </summary>
    public int Flag { get; }
  }

  public class TranspositionTable
  {
    Dictionary<ulong, Transposition> Table = new();
    Dictionary<ulong, Transposition> MaxTable = new();

    public void Clear()
    {
      Table.Clear();
      MaxTable.Clear();
    }

    public int? Get(ulong key, int depth, int alpha, int beta, int ply)
    {
      if (Table.TryGetValue(key, out var entry))
      {
        int i = 0;
        do
        {
          int score = entry.Score;
          if (score < -90000) score += ply;
          if (score > 90000) score -= ply;

          if (entry.Depth >= depth)
          {
            if (entry.Flag == 0)
              return score;
            else if (entry.Flag == 1 && score <= alpha)
              return alpha;
            else if (entry.Flag == 2 && score >= beta)
              return beta;
          }

          if (!MaxTable.TryGetValue(key, out entry)) break;
          i++;
        } while (i < 2);
      }

      return null;
    }

    /// <summary>
    /// Flag: 0 = Exact, 1 = Alpha, 2 = Beta
    /// </summary>
    public void Store(ulong key, int score, int depth, int flag, int ply)
    {
      if (score < -90000) score -= ply;
      if (score > 90000) score += ply;

      Transposition newTransposition = new(score, depth, flag);
      if (MaxTable.TryGetValue(key, out var entry))
      {
        if (entry.Depth > depth)
          MaxTable[key] = newTransposition;
      }
      else
        MaxTable[key] = newTransposition;

      Table[key] = newTransposition;
    }
  }

  public class MyBot : IChessBot
  {
    int Inf = int.MaxValue;
    int[,] mvv_lva = new int[6, 6]
    {
      { 105, 205, 305, 405, 505, 605 },
      { 104, 204, 304, 404, 504, 604 },
      { 103, 203, 303, 403, 503, 603 },
      { 102, 202, 302, 402, 502, 602 },
      { 101, 201, 301, 401, 501, 601 },
      { 100, 200, 300, 400, 500, 600 },
    };
    int Depth = 3;
    Dictionary<PieceType, int> PieceVal = new()
    {
      { PieceType.None, 0 },
      { PieceType.Pawn, 100 },
      { PieceType.Rook, 500 },
      { PieceType.Knight, 300 },
      { PieceType.Bishop, 300 },
      { PieceType.Queen, 900 },
      { PieceType.King, 0 }
    };
    TranspositionTable TranspositionTable = new();

    public Move Think(Board board, Timer timer)
    {
      Move[] bestMoves = NegaMaxRoot(board, Depth, -Inf, Inf, board.IsWhiteToMove ? 1 : -1);
      Random rng = new();
      Move nextMove = bestMoves[rng.Next(bestMoves.Length)];

      return nextMove;
    }

    Move[] NegaMaxRoot(Board board, int depth, int alpha, int beta, int color)
    {
      // Repetition history clears when a pawn moves or a capture is made. We can safely clear the transposition table
      if (board.GameRepetitionHistory.Length == 0)
        TranspositionTable.Clear();

      Move[] moves = GetOrderedMoves(board);
      if (moves.Length <= 1)
        return moves;

      List<Move> bestMoves = new();

      int bestScore = alpha;
      foreach (Move move in moves) // root move
      {
        int score = MakeMove(board, move, depth, alpha, beta, color); // make root move

        if (score == bestScore)
          bestMoves.Add(move);

        else if (score > bestScore)
        {
          bestScore = score;
          bestMoves.Clear();
          bestMoves.Add(move);

          if (score == 100000)
            break;
        }
      }

      // If we found nothing, then we'll just randomly pick between what we do have.
      if (bestMoves.Count == 0)
        return moves;

      return bestMoves.ToArray();
    }

    int MakeMove(Board board, Move move, int depth, int alpha, int beta, int color)
    {
      board.MakeMove(move);
      if (board.IsInCheckmate())
      {
        board.UndoMove(move);
        return depth == Depth ? 100000 : 90000 + board.PlyCount;
      }

      int score = -NegaMax(depth, board, -beta, -alpha, -color);
      board.UndoMove(move);

      return score;
    }

    Move[] GetOrderedMoves(Board board, bool capturesOnly = false)
    {
      Move[] moves = board.GetLegalMoves(capturesOnly);

      List<MoveEvaluation> orderedMoves = new();
      foreach (Move move in moves)
      {
        int score = move.IsCapture
          ? mvv_lva[(int)move.MovePieceType - 1, (int)move.CapturePieceType - 1]
            + (!board.SquareIsAttackedByOpponent(move.TargetSquare) ? 10700 : 10000)
          : 0;
        orderedMoves.Add(new MoveEvaluation(score, move));
      }

      return orderedMoves.OrderByDescending(o => o.Score).Select(o => o.Move).ToArray();
    }

    int NegaMax(int depth, Board board, int alpha, int beta, int color)
    {
      ulong key = board.ZobristKey;
      int flag = 1, ply = board.PlyCount;

      int? entry = TranspositionTable.Get(key, depth, alpha, beta, ply);
      if (entry != null)
        return (int)entry;

      if (depth == 0)
      {
        int val = Quiescence(board, alpha, beta, color);
        TranspositionTable.Store(key, val, depth, flag: 0, ply);
        return val;
      }

      Move[] orderedMoves = GetOrderedMoves(board);

      foreach (Move move in orderedMoves)
      {
        int score = MakeMove(board, move, depth - 1, alpha, beta, color);

        if (score >= beta)
        {
          TranspositionTable.Store(key, beta, depth, flag: 2, ply);
          return beta;
        }

        if (score > alpha)
        {
          flag = 0;
          alpha = score;
        }
      }

      TranspositionTable.Store(key, alpha, depth, flag, ply);
      return alpha;
    }

    int Quiescence(Board board, int alpha, int beta, int color)
    {
      PieceList[] pieceList = board.GetAllPieceLists();
      int materialVal = 0;

      foreach (PieceList pieces in pieceList)
      {
        foreach (Piece piece in pieces)
        {
          if (piece.IsWhite)
            materialVal += PieceVal[piece.PieceType];
          else
            materialVal -= PieceVal[piece.PieceType];
        }
      }

      int eval = color * materialVal;

      if (eval >= beta)
        return beta;

      if (eval > alpha)
        alpha = eval;

      Move[] orderedMoves = GetOrderedMoves(board, true);

      foreach (Move move in orderedMoves)
      {
        board.MakeMove(move);
        int score = -Quiescence(board, -beta, -alpha, -color);
        board.UndoMove(move);

        if (score >= beta)
          return beta;

        if (score > alpha)
          alpha = score;
      }

      return alpha;
    }
  }
}