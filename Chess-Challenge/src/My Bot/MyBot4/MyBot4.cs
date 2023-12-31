using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

// 1014 / 1024 (+26 for Node counting)
namespace MyBot4
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

        // if (entry.Flag == 0)
        //   return score;
        // else if (entry.Flag == 1 && score <= alpha)
        //   return alpha;
        // else if (entry.Flag == 2 && score >= beta)
        //   return beta;
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
    private int Depth = 4;
    private readonly int[] PieceVal = new int[] { 0, 100, 300, 300, 500, 900, 0 }; // No, P, N, B, R, Q, K
    private readonly TranspositionTable transpositionTable = new();
    private int nodes;

    public Move Think(Board board, Timer timer)
    {
      nodes = 0;
      // Move bestMove = new();

      // for (int currentDepth = 1; currentDepth <= Depth; currentDepth++)
      // {
      //   Move[] bestMoves = NegaMaxRoot(board, currentDepth, -Inf, Inf, board.IsWhiteToMove ? 1 : -1);
      //   // if (timer.ElapsedMilliseconds >= TimeLimitMilliseconds)
      //   //     break;
      //   bestMove = bestMoves[0]; // Store the best move from the current depth
      // }

      // Console.WriteLine("Nodes: {0}", nodes);
      // return bestMove;

      /////////////////////////

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
      Console.WriteLine("Nodes: {0}", nodes);
      return nextMove;
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
          move.IsCapture // MVV_LVA algebra calculation
            ? (100 * (int)move.CapturePieceType) - (int)move.MovePieceType + 6
            // + (!board.SquareIsAttackedByOpponent(move.TargetSquare) ? 700 : 0) // 26:54:20 
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
        nodes++;

        // int seeScore = SEE(board, move);

        // if (eval + seeScore >= beta)
        // {
        //   board.UndoMove(move);
        //   return beta;
        // }

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

    // private int SEE() {
    //   int score = 0;
    //   int capturedValue = PieceVal[move.CapturePieceType];
    //   int attackerValue = PieceVal[move.MovePieceType];

    //   piece = get_smallest_attacker(square, side);


    //    /* skip if the square isn't attacked anymore by this side */
    //    if ( piece )
    //    {
    //       make_capture(piece, square);
    //       /* Do not consider captures if they lose material, therefor max zero */
    //       value = max (0, piece_just_captured() -see(square, other(side)) );
    //       undo_capture(piece, square);
    //    }
    //    return value;
    // }






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