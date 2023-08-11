using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

// 924 / 1024
namespace MyBot4_4_RC1
{
  public record Transposition(int Score, byte Depth, byte Flag, LinkedListNode<ulong> Node); // ~6 bytes per record

  public class TranspositionTable
  {
    private readonly Dictionary<ulong, Transposition> table = new();
    private readonly LinkedList<ulong> evictionQueue = new();
    private readonly int maxTableSize = 5000000; // 256mb / 6 bytes = ~24,000,000 records is the upper bound

    public int? Get(ulong key, int depth, int alpha, int beta, int ply)
    {
      if (table.TryGetValue(key, out var entry) && entry.Depth >= depth)
      {
        MoveToEnd(entry.Node);
        int score = AdjustScore(entry.Score, ply);

        return entry.Flag switch
        {
          0 => score,
          1 when score <= alpha => alpha,
          2 when score >= beta => beta,
          _ => null
        };
      }

      return null;
    }

    // Flag: 0 = Exact, 1 = Alpha, 2 = Beta
    public void Store(ulong key, int score, int depth, int flag, int ply)
    {
      score = AdjustScore(score, -ply);

      if (table.TryGetValue(key, out var existingEntry))
      {
        // Replace on Depth if the new entry has higher depth, or always replace if it's the same
        if (depth >= existingEntry.Depth)
        {
          MoveToEnd(existingEntry.Node);
          table[key] = new(score, (byte)depth, (byte)flag, existingEntry.Node);
        }
      }
      else
      {
        var node = evictionQueue.AddLast(key);
        table.Add(key, new(score, (byte)depth, (byte)flag, node));
      }

      // Trim excess
      if (table.Count >= maxTableSize)
      {
        ulong oldestKey = evictionQueue.First.Value;
        table.Remove(oldestKey);
        evictionQueue.RemoveFirst();
      }
    }

    private void MoveToEnd(LinkedListNode<ulong> node)
    {
      evictionQueue.Remove(node);
      evictionQueue.AddLast(node);
    }

    // We need to take into consideration "soon to be check" moves need to be independent of play
    private int AdjustScore(int score, int ply)
    {
      if (score < -90000) score += ply;
      if (score > 90000) score -= ply;
      return score;
    }
  }

  public class MyBot : IChessBot
  {
    private int Inf = int.MaxValue;
    private int Depth = 3;
    private readonly int[] PieceVal = new int[] { 0, 100, 300, 300, 500, 900, 0 }; // No, P, N, B, R, Q, K
    private readonly TranspositionTable transpositionTable = new();

    public Move Think(Board board, Timer timer)
    {
      Move[] moves = GetOrderedMoves(board);
      List<Move> bestMoves = new(moves);
      int bestScore = -Inf;

      foreach (Move move in moves)
      {
        int score = MakeAndUndoMove(board, move, Depth, -Inf, Inf, board.IsWhiteToMove ? 1 : -1);

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

      Random rng = new();
      Move nextMove = bestMoves[rng.Next(bestMoves.Count)];
      return nextMove;
    }

    private int MakeAndUndoMove(Board board, Move move, int depth, int alpha, int beta, int color)
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

    private Move[] GetOrderedMoves(Board board, bool capturesOnly = false)
    {
      Move[] moves = board.GetLegalMoves(capturesOnly);
      return moves
        .OrderByDescending(move =>
          move.IsCapture // MVV_LVA algebra calculation
            ? (100 * (int)move.CapturePieceType) - (int)move.MovePieceType + 6
            : 0)
        .ToArray();
    }

    private int NegaMax(int depth, Board board, int alpha, int beta, int color)
    {
      ulong key = board.ZobristKey;
      int flag = 1, ply = board.PlyCount;

      int? entry = transpositionTable.Get(key, depth, alpha, beta, ply);
      if (entry != null)
        return (int)entry;

      // if in check, increase depth?

      if (depth == 0)
      {
        int val = Quiescence(board, alpha, beta, color);
        transpositionTable.Store(key, val, depth, flag: 0, ply);
        return val;
      }

      // Null move pruning. With R = 2, Depth will need to be > 4 for this to run
      if (ply <= 70 && depth >= 3 && board.TrySkipTurn())
      {
        int nullScore = -NegaMax(depth - 1 - 2, board, -beta, -beta + 1, -color);
        board.UndoSkipTurn();
        if (nullScore >= beta)
          return beta;
      }

      Move[] orderedMoves = GetOrderedMoves(board);
      foreach (Move move in orderedMoves)
      {
        int score = MakeAndUndoMove(board, move, depth - 1, alpha, beta, color);

        if (score >= beta)
        {
          transpositionTable.Store(key, beta, depth, flag: 2, ply);
          return beta; // soft vs hard
        }

        if (score > alpha)
        {
          flag = 0;
          alpha = score;
        }
      }

      transpositionTable.Store(key, alpha, depth, flag, ply);
      return alpha;
    }

    private int Quiescence(Board board, int alpha, int beta, int color, int depth = 3)
    {
      int? entry = transpositionTable.Get(board.ZobristKey, depth, alpha, beta, board.PlyCount);
      if (entry != null)
        return (int)entry;

      int eval = color * board.GetAllPieceLists()
        .SelectMany(pieces => pieces)
        .Sum(piece => (piece.IsWhite ? 1 : -1) * PieceVal[(int)piece.PieceType]);

      if (depth == 0 || eval >= beta)
        return eval;

      alpha = Math.Max(alpha, eval); // Update alpha with the stand-pat evaluation

      Move[] orderedMoves = GetOrderedMoves(board, true);
      foreach (Move move in orderedMoves)
      {
        board.MakeMove(move);
        int score = -Quiescence(board, -beta, -alpha, -color, depth - 1);
        board.UndoMove(move);

        if (score >= beta)
          return beta; // Fail-hard beta cutoff

        alpha = Math.Max(alpha, score); // Update alpha with the stand-pat evaluation

        int delta = score - eval;
        if (delta > 0 && delta >= 100) // Delta pruning condition, adjust the threshold as needed
          break; // Stop searching if the improvement is significant
      }

      return alpha;
    }
  }
}