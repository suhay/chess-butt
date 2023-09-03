using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

// 1020 / 1024
namespace ChessButt  // #DEBUG
{  // #DEBUG
  record Transposition(int Score, byte Depth, byte Flag, LinkedListNode<ulong> Node, ushort Move); // ~6 bytes per record

  public class MyBot : IChessBot
  {
    readonly Dictionary<ulong, Transposition> TranspositionTable = new();
    readonly LinkedList<ulong> EvictionQueue = new();

    readonly int Inf = int.MaxValue;
    readonly int[] PieceVal = new int[] { 0, 100, 300, 320, 500, 900, 2500 }; // No, P, N, B, R, Q, K

    readonly Dictionary<int, Move> K1 = new();
    readonly Dictionary<int, Move> K2 = new();

    Board board;
    int Depth;
    int Ply;

    int DeltaCutoff = 300; // #DEBUG
    int MobilityWeight = 8; // #DEBUG

    int FullDepthMoves = 3; // #DEBUG
    int ReductionLimit = 3; // #DEBUG

    int R = 2; // #DEBUG

    int Panic = 10000; // #DEBUG
    int PanicD = 2; // #DEBUG  2 prevented all over tines, but was slightly worse

    int LateGamePly = 70; // #DEBUG

    int TMax = 3000000;  // #DEBUG

    public MyBot(int dc, int mw, int fdm, int rl, int r, int p, int pd, int lgp, int tm) // #DEBUG
    { // #DEBUG
      DeltaCutoff = dc; // #DEBUG
      MobilityWeight = mw; // #DEBUG
      FullDepthMoves = fdm; // #DEBUG
      ReductionLimit = rl; // #DEBUG
      R = r; // #DEBUG
      Panic = p; // #DEBUG
      PanicD = pd; // #DEBUG
      LateGamePly = lgp; // #DEBUG
      TMax = tm; // #DEBUG
    } // #DEBUG

    public Move Think(Board _board, Timer timer)
    {
      board = _board;
      Depth = timer.MillisecondsRemaining <= Panic ? 4 - PanicD : 4;
      // Depth = 4;
      Ply = 0;

      Move[] moves = GetOrderedMoves();
      List<Move> bestMoves = new(moves);
      int bestScore = -Inf;

      foreach (Move move in moves)
      {
        K1.Clear();
        K2.Clear();
        int score = MakeAndUndoMove(move, Depth, -Inf, Inf, board.IsWhiteToMove ? 1 : -1);

        if (score > bestScore)
        {
          bestScore = score;
          bestMoves.Clear();
        }

        if (score == bestScore)
          bestMoves.Add(move);

        if (score == 100000)
          break;
      }

      // Random rng = new();
      // return bestMoves[rng.Next(bestMoves.Count)];
      return bestMoves[0];
    }

    // alpha becomes -beta in the next iteration
    private int MakeAndUndoMove(Move move, int depth, int alpha, int beta, int color)
    {
      board.MakeMove(move);
      Ply++;

      int score = board.IsInCheckmate()
        ? 100000 - (Depth - depth) // Prioritize checkmates, giving more to checkmate in one
        : -NegaMax(depth, -beta, -alpha, -color);

      board.UndoMove(move);
      Ply--;

      return score;
    }

    private Move[] GetOrderedMoves(bool capturesOnly = false)
    {
      TranspositionTable.TryGetValue(board.ZobristKey, out var entry);

      return board.GetLegalMoves(capturesOnly)
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
          return 0;
        })
        .ToArray();
    }

    private int NegaMax(int depth, int alpha, int beta, int color)
    {
      ulong key = board.ZobristKey;
      byte flag = 1;

      /////////////////// TT Lookup
      int? entry = GetTranspositionScore(key, depth, alpha, beta);
      if (entry != null)
        return (int)entry;

      /////////////////// Check Extension
      if (board.IsInCheck())
        depth++;

      /////////////////// Leaf Node
      if (depth <= 0)
      {
        int val = Quiescence(alpha, beta, color, 3);
        StoreTransposition(key, val, depth, 0, 0); // no best move to store, at leaf
        return val;
      }

      /////////////////// Null Move Pruning
      if (board.PlyCount <= LateGamePly && depth >= 3 && board.TrySkipTurn())
      {
        int nullScore = -NegaMax(depth - 1 - R, -beta, -beta + 1, -color);
        board.UndoSkipTurn();
        if (nullScore >= beta)
          return nullScore;
      }

      Move[] orderedMoves = GetOrderedMoves();

      int movesSearched = 0;
      foreach (Move move in orderedMoves)
      {
        int score = (movesSearched >= FullDepthMoves && depth >= ReductionLimit && !move.IsCapture && !move.IsPromotion)
          ? (alpha < (score = MakeAndUndoMove(move, depth - 2, alpha, alpha + 1, color)) && score < beta)
              ? MakeAndUndoMove(move, depth - 1, alpha, beta, color)
              : score
          : MakeAndUndoMove(move, depth - 1, alpha, beta, color);

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
          StoreTransposition(key, score, depth, 2, move.RawValue);
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
      StoreTransposition(key, alpha, depth, flag, 0); // no best move, they were all pretty bad
      return alpha;
    }

    private int Quiescence(int alpha, int beta, int color, int depth)
    {
      int? entry = GetTranspositionScore(board.ZobristKey, depth, alpha, beta);
      if (entry != null)
        return (int)entry;

      int eval = color * board.GetAllPieceLists()
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

      /////////////////// Q Search Cutoff
      if (depth <= 0 || eval >= beta)
        return eval;

      alpha = Math.Max(alpha, eval); // update alpha with the evaluation

      Move[] orderedMoves = GetOrderedMoves(true);

      if (eval < alpha - DeltaCutoff && board.PlyCount <= LateGamePly)
        return eval;

      foreach (Move move in orderedMoves)
      {
        board.MakeMove(move);
        int score = -Quiescence(-beta, -alpha, -color, depth - 1);
        board.UndoMove(move);

        if (score >= beta)
          return score; // Fail-soft beta cutoff

        alpha = Math.Max(alpha, score); // Update alpha with the score
      }

      return alpha;
    }

    // Used to be own class, moved it here to cut down on token count
    private int? GetTranspositionScore(ulong key, int depth, int alpha, int beta)
    {
      if (TranspositionTable.TryGetValue(key, out var entry) && entry.Depth >= depth)
      {
        MoveNodeToEnd(entry.Node);
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
    private void StoreTransposition(ulong key, int score, int depth, byte flag, ushort move)
    {
      if (TranspositionTable.TryGetValue(key, out var existingEntry))
      {
        // Replace on Depth if the new entry has higher depth, or always replace if it's the same
        if (depth >= existingEntry.Depth)
        {
          MoveNodeToEnd(existingEntry.Node);
          TranspositionTable[key] = new(score, (byte)depth, flag, existingEntry.Node, move);
        }
      }
      else
        TranspositionTable.Add(key, new(score, (byte)depth, flag, EvictionQueue.AddLast(key), move));

      // Trim excess
      if (TranspositionTable.Count >= TMax) // 256mb / 6 bytes = ~24,000,000 records is the upper bound
      {
        TranspositionTable.Remove(EvictionQueue.First.Value);
        EvictionQueue.RemoveFirst();
      }
    }

    private void MoveNodeToEnd(LinkedListNode<ulong> node)
    {
      EvictionQueue.Remove(node);
      EvictionQueue.AddLast(node);
    }
  }
} // #DEBUG