using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

// 884
namespace MyBot4
{
  public record Transposition(int Score, byte Depth, byte Flag); // ~6 bytes per record

  public class TranspositionTable
  {
    private readonly SortedDictionary<ulong, Transposition> table = new();
    private readonly int maxTableSize = 2000000; // 256mb / 6 bytes = ~24,000,000 records is the upper bound

    public int? Get(ulong key, int depth, int alpha, int beta, int ply)
    {
      if (table.TryGetValue(key, out var entry) && entry.Depth >= depth)
      {
        // Remove the entry and put it on the end to maintain order
        table.Remove(key);
        table.Add(key, entry);

        int score = AdjustScore(entry.Score, ply);

        if (entry.Flag == 0)
          return score;
        else if (entry.Flag == 1 && score <= alpha)
          return alpha;
        else if (entry.Flag == 2 && score >= beta)
          return beta;
      }

      return null;
    }

    // Flag: 0 = Exact, 1 = Alpha, 2 = Beta
    public void Store(ulong key, int score, int depth, int flag, int ply)
    {
      score = AdjustScore(score, -ply);

      if (table.TryGetValue(key, out var existingEntry))
        // Replace on Depth if the new entry has higher depth, or always replace if it's the same
        if (depth >= existingEntry.Depth)
          table[key] = new Transposition(score, (byte)depth, (byte)flag);
        else
          table.Add(key, new Transposition(score, (byte)depth, (byte)flag));

      while (table.Count > maxTableSize)
        table.Remove(table.Keys.First());
    }

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
    private readonly Dictionary<PieceType, int> PieceVal = new()
    {
      { PieceType.None, 0 },
      { PieceType.Pawn, 100 },
      { PieceType.Knight, 300 },
      { PieceType.Bishop, 300 },
      { PieceType.Rook, 500 },
      { PieceType.Queen, 900 },
      { PieceType.King, 0 }
    };
    private readonly TranspositionTable transpositionTable = new();
    private int nodes;

    public Move Think(Board board, Timer timer)
    {
      nodes = 0;
      Move[] bestMoves = NegaMaxRoot(board, Depth, -Inf, Inf, board.IsWhiteToMove ? 1 : -1);
      Random rng = new();
      Move nextMove = bestMoves[rng.Next(bestMoves.Length)];
      Console.WriteLine("Nodes: {0}", nodes);
      return nextMove;
    }

    private Move[] NegaMaxRoot(Board board, int depth, int alpha, int beta, int color)
    {
      Move[] moves = GetOrderedMoves(board);
      if (moves.Length <= 1)
        return moves;

      List<Move> bestMoves = new();

      int bestScore = alpha;
      foreach (Move move in moves)
      {
        int score = MakeAndUndoMove(board, move, depth, alpha, beta, color);

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
      return bestMoves.Count == 0 ? moves : bestMoves.ToArray();
    }

    private int MakeAndUndoMove(Board board, Move move, int depth, int alpha, int beta, int color)
    {
      board.MakeMove(move);
      nodes++;
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
          move.IsCapture
            ? (100 * (int)move.CapturePieceType) - (int)move.MovePieceType + 6 // MVV_LVA calculation
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

      if (depth == 0)
      {
        int val = Quiescence(board, alpha, beta, color);
        transpositionTable.Store(key, val, depth, flag: 0, ply);
        return val;
      }

      Move[] orderedMoves = GetOrderedMoves(board);

      foreach (Move move in orderedMoves)
      {
        int score = MakeAndUndoMove(board, move, depth - 1, alpha, beta, color);

        if (score >= beta)
        {
          transpositionTable.Store(key, beta, depth, flag: 2, ply);
          return beta;
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

    private int Quiescence(Board board, int alpha, int beta, int color)
    {
      int eval = color * board.GetAllPieceLists()
        .SelectMany(pieces => pieces)
        .Sum(piece => (piece.IsWhite ? 1 : -1) * PieceVal[piece.PieceType]);

      if (eval >= beta)
        return beta;

      if (eval > alpha)
        alpha = eval;

      Move[] orderedMoves = GetOrderedMoves(board, true);

      foreach (Move move in orderedMoves)
      {
        board.MakeMove(move);
        nodes++;

        // int seeScore = SEE(board, move);

        // if (eval + seeScore >= beta)
        // {
        //   board.UndoMove(move);
        //   return beta;
        // }

        int score = -Quiescence(board, -beta, -alpha, -color);
        board.UndoMove(move);

        if (score >= beta)
          return beta;

        if (score > alpha)
          alpha = score;
      }

      return alpha;
    }

    // private int SEE(Board board, Move move)
    // {
    //   int score = 0;

    //   if (!move.IsCapture)
    //     return score;

    //   int capturedValue = PieceVal[move.CapturePieceType];
    //   int attackerValue = PieceVal[move.MovePieceType];

    //   Square targetSquare = move.TargetSquare;
    //   int ply = board.PlyCount;

    //   for (int newAttackerValue = capturedValue; newAttackerValue <= attackerValue; newAttackerValue += 100)
    //   {
    //     score = Math.Max(score, newAttackerValue - SEE(move, board, targetSquare, newAttackerValue - capturedValue, ply));
    //   }

    //   return score;
    // }

    // private int SEE(Move move, Board board, Square targetSquare, int gain, int ply)
    // {
    //   int score = Math.Max(0, gain);


    //   foreach (Piece attacker in board.SquareIsAttackedByOpponent(targetSquare))
    //   {
    //     int attackerValue = PieceVal[attacker.PieceType];
    //     int capturedValue = PieceVal[targetSquare?.PieceType ?? PieceType.None];
    //     int newGain = attackerValue - capturedValue;

    //     if (newGain > gain)
    //     {
    //       board.MakeMove(move);
    //       score = Math.Max(score, newGain - SEE(board, targetSquare, newGain, ply));
    //       board.UndoMove(move);
    //     }
    //   }

    //   return score;
    // }

  }
}