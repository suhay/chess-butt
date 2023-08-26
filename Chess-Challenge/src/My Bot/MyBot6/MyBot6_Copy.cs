using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

// 979 / 1024
namespace MyBot6_Copy  // #DEBUG
{  // #DEBUG
  public record Transposition(int Score, byte Depth, byte Flag, LinkedListNode<ulong> Node, ushort Move); // ~6 bytes per record

  public class TranspositionTable
  {
    public readonly Dictionary<ulong, Transposition> table = new();
    private readonly LinkedList<ulong> evictionQueue = new();
    private readonly int maxTableSize = 5000000; // # DEBUG 256mb / 6 bytes = ~24,000,000 records is the upper bound

    public int? Get(ulong key, int depth, int alpha, int beta)
    {
      if (table.TryGetValue(key, out var entry) && entry.Depth >= depth)
      {
        MoveToEnd(entry.Node);
        int score = entry.Score;

        return entry.Flag switch
        {
          0 => score, // Exact
          1 when score <= alpha => alpha, // Alpha
          2 when score >= beta => beta, // Beta
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
    readonly int Inf = int.MaxValue;
    readonly int MaxDepth = 4; // #DEBUG
    readonly int[] PieceVal = new int[] { 0, 100, 300, 320, 500, 900, 2500 }; // No, P, N, B, R, Q, K

    int Depth;
    int Ply = 0;

    readonly TranspositionTable transpositionTable = new();
    readonly Dictionary<int, Move> K1 = new();
    readonly Dictionary<int, Move> K2 = new();

    int DeltaCutoff = 200; // #DEBUG
    int QDepth = 4; // #DEBUG
    int MobilityWeight = 8; // #DEBUG

    int FullDepthMoves = 4; // #DEBUG
    int ReductionLimit = 3; // #DEBUG

    int R = 2; // #DEBUG

    public MyBot(int delta, int q, int mo, int fdm, int rl) // #DEBUG
    { // #DEBUG
      DeltaCutoff = delta; // #DEBUG
      QDepth = q; // #DEBUG
      MobilityWeight = mo; // #DEBUG
      FullDepthMoves = fdm; // #DEBUG
      ReductionLimit = rl; // #DEBUG
    } // #DEBUG

    public Move Think(Board board, Timer timer)
    {
      Depth = timer.MillisecondsRemaining <= 12000 ? MaxDepth - 1 : MaxDepth;
      Ply = 0;

      Move[] moves = GetOrderedMoves(board);
      List<Move> bestMoves = new(moves); // If we store the move with the most recent score, also aspiration windows
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

    // alpha becomes -beta in the next iteration
    private int MakeAndUndoMove(Board board, Move move, int depth, int alpha, int beta, int color)
    {
      board.MakeMove(move);
      Ply++;

      int score;
      if (board.IsInCheckmate())
        score = depth == Depth ? 100000 : 90000 + depth;
      else
        score = -NegaMax(depth, board, -beta, -alpha, -color);

      board.UndoMove(move);
      Ply--;

      return score;
    }

    private Move[] GetOrderedMoves(Board board, bool capturesOnly = false)
    {
      Move[] moves = board.GetLegalMoves(capturesOnly);
      transpositionTable.table.TryGetValue(board.ZobristKey, out var entry);

      return moves
        .OrderByDescending(move =>
        {
          if (move.RawValue == entry?.Move) // PV
            return 11000 + entry.Score;
          if (move.IsCapture) // MVV_LVA
            return (100 * (int)move.CapturePieceType) -
              (int)move.MovePieceType + 10006;
          if (K1.ContainsKey(Ply) && K1[Ply] == move) // Killer Move 1
            return 9000;
          if (K2.ContainsKey(Ply) && K2[Ply] == move) // Killer Move 2
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

      /////////////////// TT Lookup
      int? entry = transpositionTable.Get(key, depth, alpha, beta);
      if (entry != null)
        return (int)entry;

      /////////////////// Check Extension
      if (board.IsInCheck())
        depth++;

      /////////////////// Leaf Node
      if (depth == 0)
      {
        int val = Quiescence(board, alpha, beta, color, QDepth);
        transpositionTable.Store(key, val, depth, flag: 0, 0); // no best move to store, at leaf
        return val;
      }

      /////////////////// Null Move Pruning
      if (board.PlyCount <= 70 && depth >= 3 && board.TrySkipTurn())
      {
        int nullScore = -NegaMax(depth - 1 - R, board, -beta, -beta + 1, -color);
        board.UndoSkipTurn();
        if (nullScore >= beta)
          return nullScore;
      }

      Move[] orderedMoves = GetOrderedMoves(board);

      int movesSearched = 0;
      foreach (Move move in orderedMoves)
      {
        int score;

        /////////////////// LMR
        if (movesSearched >= FullDepthMoves && depth >= ReductionLimit && !move.IsCapture && !move.IsPromotion)
        {
          score = MakeAndUndoMove(board, move, depth - 2, alpha, alpha + 1, color);
          if (alpha < score && score < beta)
            score = MakeAndUndoMove(board, move, depth - 1, alpha, beta, color);
        }
        else
          score = MakeAndUndoMove(board, move, depth - 1, alpha, beta, color);

        if (score >= beta)
        {
          /////////////////// Killer Moves
          if (!move.IsCapture)
          {
            if (K1.ContainsKey(Ply))
              K2[Ply] = K1[Ply];
            K1[Ply] = move;
          }

          /////////////////// TT Store
          transpositionTable.Store(key, score, depth, flag: 2, move.RawValue);
          return score; // soft vs hard
        }

        if (score > alpha)
        {
          flag = 0;
          alpha = score;
        }

        movesSearched++;
      }

      /////////////////// TT Store
      transpositionTable.Store(key, alpha, depth, flag, 0); // no best move, they were all pretty bad
      return alpha;
    }

    int Evaluate(Board board)
    {
      return board.GetAllPieceLists()
        .SelectMany(pieces => pieces)
        .Sum(piece => (piece.IsWhite ? 1 : -1) *
          (
            PieceVal[(int)piece.PieceType] +
            (
              BitboardHelper.GetNumberOfSetBits(
                BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite)
              ) * MobilityWeight
            )
          )
        );
    }

    private int Quiescence(Board board, int alpha, int beta, int color, int depth)
    {
      int? entry = transpositionTable.Get(board.ZobristKey, depth, alpha, beta);
      if (entry != null)
        return (int)entry;

      int eval = color * Evaluate(board);

      /////////////////// Q Search Cutoff
      if (depth == 0 || eval >= beta)
        return eval;

      alpha = Math.Max(alpha, eval); // update alpha with the evaluation

      Move[] orderedMoves = GetOrderedMoves(board, true);

      foreach (Move move in orderedMoves)
      {
        Ply++;
        board.MakeMove(move);
        int score = -Quiescence(board, -beta, -alpha, -color, depth - 1);
        board.UndoMove(move);
        Ply--;

        if (score >= beta)
          return score; // Fail-soft beta cutoff

        if (eval < alpha - DeltaCutoff && board.PlyCount <= 70)
          break;

        alpha = Math.Max(alpha, score); // Update alpha with the score
      }

      return alpha;
    }
  }
} // #DEBUG