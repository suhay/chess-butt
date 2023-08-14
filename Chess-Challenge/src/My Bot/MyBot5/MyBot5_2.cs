using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

// 969 / 1024
namespace MyBot5_2  // #DEBUG
{  // #DEBUG
  public record Transposition(int Score, byte Depth, byte Flag, LinkedListNode<ulong> Node, ushort Move); // ~6 bytes per record

  public class TranspositionTable
  {
    public readonly Dictionary<ulong, Transposition> table = new();
    private readonly LinkedList<ulong> evictionQueue = new();
    private readonly int maxTableSize = 5000000; // 256mb / 6 bytes = ~24,000,000 records is the upper bound

    public int? Get(ulong key, int depth, int alpha, int beta)
    {
      if (table.TryGetValue(key, out var entry) && entry.Depth >= depth)
      {
        MoveToEnd(entry.Node);
        int score = entry.Score;

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
    public void Store(ulong key, int score, int depth, int flag, ushort move)
    {
      if (table.TryGetValue(key, out var existingEntry))
      {
        // Replace on Depth if the new entry has higher depth, or always replace if it's the same
        if (depth >= existingEntry.Depth)
        {
          MoveToEnd(existingEntry.Node);
          table[key] = new(score, (byte)depth, (byte)flag, existingEntry.Node, move);
        }
      }
      else
      {
        var node = evictionQueue.AddLast(key);
        table.Add(key, new(score, (byte)depth, (byte)flag, node, move));
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
  }

  public class MyBot : IChessBot
  {
    private int Inf = int.MaxValue;
    private int Depth = 3;
    private int Ply = 0;
    private readonly int[] PieceVal = new int[] { 0, 100, 300, 300, 500, 900, 0 }; // No, P, N, B, R, Q, K
    private readonly TranspositionTable transpositionTable = new();
    Dictionary<int, Move> K1 = new();
    Dictionary<int, Move> K2 = new();

    public Move Think(Board board, Timer timer)
    {
      Ply = 0;

      Move[] moves = GetOrderedMoves(board);
      List<Move> bestMoves = new(moves);
      int bestScore = -Inf;

      foreach (Move move in moves)
      {
        K1.Clear();
        K2.Clear();

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
        return depth == Depth ? 100000 : 90000 + depth;
      }
      Ply++;
      int score = -NegaMax(depth, board, -beta, -alpha, -color);
      Ply--;
      board.UndoMove(move);

      return score;
    }

    private Move[] GetOrderedMoves(Board board, bool capturesOnly = false)
    {
      Move[] moves = board.GetLegalMoves(capturesOnly);
      transpositionTable.table.TryGetValue(board.ZobristKey, out var entry);

      return moves
        .OrderByDescending(move =>
        {
          if (move.RawValue == entry?.Move)
            return 11000;
          if (move.IsCapture)
            return (100 * (int)move.CapturePieceType) - (int)move.MovePieceType + 10006;
          if (K1.ContainsKey(Ply) && K1[Ply] == move)
            return 9000;
          if (K2.ContainsKey(Ply) && K2[Ply] == move)
            return 8000;
          if (move.IsPromotion)
            return 4000;
          return 0;
        })
        .ToArray();
    }

    private int NegaMax(int depth, Board board, int alpha, int beta, int color)
    {
      ulong key = board.ZobristKey;
      int flag = 1;

      int? entry = transpositionTable.Get(key, depth, alpha, beta);
      if (entry != null)
        return (int)entry;

      // if in check, increase depth?

      if (depth == 0)
      {
        int val = Quiescence(board, alpha, beta, color);
        transpositionTable.Store(key, val, depth, flag: 0, 0);
        return val;
      }

      Move[] orderedMoves = GetOrderedMoves(board);

      foreach (Move move in orderedMoves)
      {
        int score = MakeAndUndoMove(board, move, depth - 1, alpha, beta, color);

        if (score >= beta)
        {
          if (!move.IsCapture)
          {
            if (K1.ContainsKey(Ply)) K2[Ply] = K1[Ply];
            K1[Ply] = move;
          }

          transpositionTable.Store(key, beta, depth, flag: 2, move.RawValue);
          return beta; // soft vs hard
        }

        if (score > alpha)
        {
          flag = 0;
          alpha = score;
        }
      }

      transpositionTable.Store(key, alpha, depth, flag, 0);
      return alpha;
    }

    private int Quiescence(Board board, int alpha, int beta, int color, int depth = 3)
    {
      int? entry = transpositionTable.Get(board.ZobristKey, depth, alpha, beta);
      if (entry != null)
        return (int)entry;

      Move[] orderedMoves = GetOrderedMoves(board, true);

      int eval = color * board.GetAllPieceLists()
        .SelectMany(pieces => pieces)
        .Sum(piece => (piece.IsWhite ? 1 : -1) *
          (PieceVal[(int)piece.PieceType]
            + (orderedMoves.Length * 10))
            );

      if (depth == 0 || eval >= beta)
        return eval;

      alpha = Math.Max(alpha, eval); // Update alpha with the stand-pat evaluation

      foreach (Move move in orderedMoves)
      {
        Ply++;
        board.MakeMove(move);
        int score = -Quiescence(board, -beta, -alpha, -color, depth - 1);
        board.UndoMove(move);
        Ply--;

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
} // #DEBUG