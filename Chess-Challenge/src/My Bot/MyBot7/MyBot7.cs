using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

// 979 / 1024
namespace MyBot7 // #DEBUG
{  // #DEBUG
  public record Transposition(int Score, byte Depth, byte Flag, LinkedListNode<ulong> Node, ushort BestNextMove); // ~6 bytes per record

  public class TranspositionTable
  {
    readonly Dictionary<ulong, Transposition> table = new();
    readonly LinkedList<ulong> evictionQueue = new();
    readonly int maxTableSize = 5000000; // # DEBUG 256mb / 6 bytes = ~24,000,000 records is the upper bound

    public Transposition Get(ulong key) => table[key];

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
    public void Store(ulong key, int score, int depth, int flag, ushort nextMove)
    {
      if (table.TryGetValue(key, out var existingEntry))
      {
        // Replace on Depth if the new entry has higher depth, or always replace if it's the same
        if (depth >= existingEntry.Depth)
        {
          MoveToEnd(existingEntry.Node);
          table[key] = new(score, (byte)depth, (byte)flag, existingEntry.Node, nextMove);
        }
      }
      else
      {
        var node = evictionQueue.AddLast(key);
        table.Add(key, new(score, (byte)depth, (byte)flag, node, nextMove));
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

  struct Beam
  {
    public Beam(int guess, Move move)
    {
      Guess = guess;
      Move = move;
    }

    public Move Move;
    public int Guess;
    public int OnWatch = 0;
  }

  public class MyBot : IChessBot
  {
    public static int Inf = int.MaxValue;
    readonly int Depth = 3;
    readonly int[] PieceVal = new int[] { 0, 100, 300, 300, 500, 900, 0 }; // No, P, N, B, R, Q, K

    int Ply = 0;
    int Nodes; // #DEBUG

    readonly Dictionary<ushort, Beam> Beams = new();
    readonly List<ushort> PriorityQueue = new();
    readonly TranspositionTable TranspositionTable = new();

    public Move Think(Board board, Timer timer)
    {
      int currentDepth = 0;
      Beams.Clear();

      GetOrderedLegalMoves(board).ForEach((move) =>
      {
        ushort key = move.RawValue;
        Beams[key] = new Beam(0, move);
        PriorityQueue.Add(key);
      });

      do
      {
        foreach (ushort key in PriorityQueue)
        {
          Beam beam = Beams[key];
          if (beam.OnWatch > 2) continue;

          int score = MakeAndUndoMove(board, beam.Move, beam, Depth, -Inf, Inf, board.IsWhiteToMove ? 1 : -1);
          beam.Guess = score;

          /////////////////// Aspiration window
          // if ((val <= alpha) || (val >= beta))
          // {
          //   alpha = -INFINITY;    // We fell outside the window, so try again with a
          //   beta = INFINITY;      //  full-width window (and the same depth).
          //   continue;
          // }

          // alpha = val - 50;  // Set up the window for the next iteration.
          // beta = val + 50;
          // depth++;
          ///////////////////
        }

        PriorityQueue.Sort((ushort a, ushort b) => Beams[a].Guess.CompareTo(Beams[b].Guess));
        PriorityQueue
          .TakeLast((int)Math.Round(PriorityQueue.Count * 0.3))
          .ToList()
          .ForEach(key =>
          {
            var beam = Beams[key];
            beam.OnWatch += 1;
          });

        currentDepth++;
      } while (currentDepth <= Depth);

      Console.WriteLine("Nodes: {0}, {1}", Nodes, Beams[PriorityQueue[0]].Move.ToString()); // #DEBUG
      return Beams[PriorityQueue[0]].Move;
    }

    private List<Move> GetOrderedLegalMoves(Board board, bool capturesOnly = false)
    {
      return board.GetLegalMoves(capturesOnly)
        .OrderByDescending(move =>
        {
          if (Beams.ContainsKey(move.RawValue))
            return 200_000;
          if (move.IsCapture)
            return (100 * (int)move.CapturePieceType) - (int)move.MovePieceType + 10_006;
          if (move.IsPromotion)
            return 4_000;
          return 0;
        }).ToList();
    }

    private int MakeAndUndoMove(Board board, Move move, Beam beam, int alpha, int beta, int depth, int color)
    {
      Ply++;
      Nodes++; // #DEBUG
      board.MakeMove(move);

      int score;
      if (board.IsInCheckmate())
        score = depth == Depth ? 100_000 : 90_000 + depth;
      else
        score = -NegaMax(board, beam, depth, -beta, -alpha, -color);

      board.UndoMove(move);
      Ply--;

      return score;
    }

    private int NegaMax(Board board, Beam beam, int depth, int alpha, int beta, int color)
    {
      ulong key = board.ZobristKey;
      int flag = 1;

      int? entry = TranspositionTable.Get(key, depth, alpha, beta);
      if (entry != null)
        return (int)entry;

      // Check extension
      if (board.IsInCheck())
        depth++;

      if (depth == 0)
      {
        int val = Quiescence(board, alpha, beta, color, 3);
        TranspositionTable.Store(key, val, depth, flag: 0, 0); // no best move to store, at leaf
        return val;
      }

      List<Move> orderedMoves = GetOrderedLegalMoves(board);

      foreach (Move move in orderedMoves)
      {
        int score = MakeAndUndoMove(board, move, beam, depth - 1, alpha, beta, color);
        if (score >= beta)
        {
          TranspositionTable.Store(key, score, depth, flag: 2, move.RawValue);
          return score; // soft vs hard
        }

        if (score > alpha)
        {
          flag = 0;
          alpha = score;
        }
      }

      TranspositionTable.Store(key, alpha, depth, flag, 0); // no best move, they were all pretty bad
      return alpha;
    }

    private int Quiescence(Board board, int alpha, int beta, int color, int depth)
    {
      ulong key = board.ZobristKey;
      int flag = 1;
      List<Move> orderedMoves = GetOrderedLegalMoves(board, true);

      int eval = color * board.GetAllPieceLists()
        .SelectMany(pieces => pieces)
        .Sum(piece => (piece.IsWhite ? 1 : -1) *
          (
            PieceVal[(int)piece.PieceType]
            + (orderedMoves.Count * 10)
          )
        );

      if (depth == 0 || eval >= beta)
      {
        TranspositionTable.Store(key, eval, depth, flag: 0, 0); // no best move to store, at leaf
        return eval;
      }

      alpha = Math.Max(alpha, eval); // update alpha with the evaluation

      foreach (Move move in orderedMoves)
      {
        Ply++;
        Nodes++; // #DEBUG
        board.MakeMove(move);

        int score = -Quiescence(board, -beta, -alpha, -color, depth - 1);
        board.UndoMove(move);
        Ply--;

        if (score >= beta)
        {
          TranspositionTable.Store(key, score, depth, flag: 2, move.RawValue);
          return score; // Fail-soft beta cutoff
        }

        if (score > alpha)
        {
          flag = 0;
          alpha = score;
        }

        int delta = score - eval;
        if (delta > 0 && delta >= 100) // Delta pruning condition, adjust the threshold as needed
          break; // Stop searching if the improvement is significant
      }

      TranspositionTable.Store(key, alpha, depth, flag, 0); // no best move, they were all pretty bad
      return alpha;
    }
  }
}