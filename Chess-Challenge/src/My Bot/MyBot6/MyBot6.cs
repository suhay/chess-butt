using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

// 969 / 1024
namespace MyBot6  // #DEBUG
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
    private readonly int Inf = int.MaxValue;
    private readonly int Depth = 3;
    private int Ply = 0;
    private readonly int[] PieceVal = new int[] { 0, 100, 300, 300, 500, 900, 0 }; // No, P, N, B, R, Q, K
    private int nodes; // #DEBUG

    private readonly TranspositionTable transpositionTable = new();
    private readonly Dictionary<int, Move> K1 = new();
    private readonly Dictionary<int, Move> K2 = new();

    // Each beam needs the root node, the relative scores (eval, alpha, beta), killer moves, tbe ordered moves at depth for
    // subsequent iterative deepenings
    // readonly Dictionary<int, Move> Beams = new();

    public Move Think(Board board, Timer timer)
    {
      nodes = 0; // #DEBUG
      Ply = 0;



      Move[] moves = GetOrderedMoves(board);
      List<Move> bestMoves = new(moves); // If we store the move with the most recent score, also aspiration windows
      int bestScore = -Inf;
      int currentDepth = 0;

      try
      {
        //   do
        //   {
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

          // if (timer.ElapsedMilliseconds >= TimeLimitMilliseconds)
          //   throw new Exception("Times up");
        }
        // if ((val <= alpha) || (val >= beta))
        // {
        //   alpha = -INFINITY;    // We fell outside the window, so try again with a
        //   beta = INFINITY;      //  full-width window (and the same depth).
        //   continue;
        // }

        // alpha = val - valWINDOW;  // Set up the window for the next iteration.
        // beta = val + valWINDOW;
        // depth++;

        // currentDepth++;
        // split the moves for deeper looking and reorder them
        // } while (currentDepth <= Depth);
      }
      catch (Exception e)
      {
        // ejected from search, bring score and move along
      }

      Random rng = new();
      Move nextMove = bestMoves[rng.Next(bestMoves.Count)];
      Console.WriteLine("Nodes: {0}, Moves: {1}", nodes, bestMoves.Count); // #DEBUG
      return nextMove;
    }

    // alpha becomes -beta in the next iteration
    private int MakeAndUndoMove(Board board, Move move, int depth, int alpha, int beta, int color)
    {
      board.MakeMove(move);
      nodes++; // #DEBUG
      if (board.IsInCheckmate())
      {
        board.UndoMove(move);
        return depth == Depth ? 100000 : 90000 + depth;
      }
      Ply++;
      int score = -NegaMax(depth, board, -beta, -alpha, -color);
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

      if (board.IsInCheck())
        depth++;

      if (depth == 0)
      {
        int val = Quiescence(board, alpha, beta, color);
        transpositionTable.Store(key, val, depth, flag: 0, 0); // no best move to store, at leaf
        return val;
      }

      // Null move pruning. With R = 2, Depth will need to be > 4 for this to run beyond evaluating the next position
      // if (board.PlyCount <= 70 && depth >= 3 && board.TrySkipTurn())
      // {
      //   int nullScore = -NegaMax(depth - 1 - 2, board, -beta, -beta + 1, -color);
      //   board.UndoSkipTurn();
      //   if (nullScore >= beta)
      //   {
      //     Console.WriteLine(".");
      //     return beta;
      //   }
      // }

      Move[] orderedMoves = GetOrderedMoves(board);

      bool firstMove = true;
      foreach (Move move in orderedMoves)
      {
        //////////// NegaScout
        // int score;
        // if (firstMove)
        // {
        //   score = MakeAndUndoMove(board, move, depth - 1, alpha, beta, color);
        //   firstMove = false;
        // }
        // else
        // {
        //   score = MakeAndUndoMove(board, move, depth - 1, alpha, alpha + 1, color);
        //   if (alpha < score && score < beta)
        //     score = MakeAndUndoMove(board, move, depth - 1, score, beta, color);
        // }

        // if (score > alpha)
        // {
        //   flag = 0;
        //   alpha = score;
        // }

        // if (alpha >= beta)
        // {
        //   transpositionTable.Store(key, beta, depth, flag: 2, move.RawValue);
        //   return beta;
        // }
        ////////////



        /////////// AlphaBeta
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
        ///////////
      }

      transpositionTable.Store(key, alpha, depth, flag, 0); // no best move, they were all pretty bad
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
              (
                PieceVal[(int)piece.PieceType]
                + (orderedMoves.Length * 10)
              )
            );

      if (depth == 0 || eval >= beta)
        return eval;

      alpha = Math.Max(alpha, eval); // update alpha with the evaluation

      foreach (Move move in orderedMoves)
      {
        Ply++;
        nodes++; // #DEBUG
        board.MakeMove(move);

        // int seeScore = SEE(board, move);

        // if (eval + seeScore >= beta)
        // {
        //   board.UndoMove(move);
        //   return beta;
        // }

        int score = -Quiescence(board, -beta, -alpha, -color, depth - 1);
        board.UndoMove(move);
        Ply--;

        if (score >= beta)
          return beta; // Fail-hard beta cutoff

        alpha = Math.Max(alpha, score); // Update alpha with the score

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
} // #DEBUG