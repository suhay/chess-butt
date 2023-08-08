using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

// 999
namespace MyBot4
{
  public struct Transposition
  {
    public Transposition(int score, int depth, int flag /*, string move*/)
    {
      Depth = depth;
      Score = score;
      Flag = flag;
      // Move = move;
    }

    public int Depth;
    public int Score;
    /// <summary>
    /// 0 = Exact, 1 = Alpha, 2 = Beta
    /// </summary>
    public int Flag;
    //public string Move;
  }

  public class TranspositionTable
  {
    public SortedList<ulong, Transposition> Table = new();
    int maxTableSize = 20000;

    public int? Get(ulong key, int depth, int alpha, int beta, int ply)
    {
      if (Table.TryGetValue(key, out var entry) && entry.Depth >= depth)
      {
        // Remove the entry and put it on the end to maintain order
        Table.Remove(key);
        Table.Add(key, entry);

        int score = entry.Score;
        if (score < -90000) score += ply;
        if (score > 90000) score -= ply;

        if (entry.Flag == 0)
          return score;
        else if (entry.Flag == 1 && score <= alpha)
          return alpha;
        else if (entry.Flag == 2 && score >= beta)
          return beta;
      }

      return null;
    }

    /// <summary>
    /// Flag: 0 = Exact, 1 = Alpha, 2 = Beta
    /// </summary>
    public void Store(ulong key, int score, int depth, int flag, int ply/*, string move*/)
    {
      if (score < -90000) score -= ply;
      if (score > 90000) score += ply;

      Transposition newTransposition = new(score, depth, flag);
      if (Table.TryGetValue(key, out var existingEntry))
      {
        Table.Remove(key);
        // Replace on Depth if the new entry has higher depth and the existing entry is shallow (depth 0), else, keep the old one
        Table.Add(key, depth > existingEntry.Depth && existingEntry.Depth == 0
          ? newTransposition
          : existingEntry);
      }

      // Else, always replace
      else
        Table.Add(key, newTransposition);

      // Check if the table size exceeds the maximum size, trim
      while (Table.Count >= maxTableSize)
      {
        // Remove the oldest (first) entry to maintain order
        Table.Remove(Table.Keys.First());
      }
    }
  }

  public class MyBot : IChessBot
  {
    int Inf = int.MaxValue;
    //     pub const MVV_LVA: [[u8; NrOf::PIECE_TYPES + 1]; NrOf::PIECE_TYPES + 1] = [
    //     [10, 11, 12, 13, 14, 15, 0], // victim P, attacker K, Q, R, B, N, P, None
    //     [20, 21, 22, 23, 24, 25, 0], // victim N, attacker K, Q, R, B, N, P, None
    //     [30, 31, 32, 33, 34, 35, 0], // victim B, attacker K, Q, R, B, N, P, None
    //     [40, 41, 42, 43, 44, 45, 0], // victim R, attacker K, Q, R, B, N, P, None
    //     [50, 51, 52, 53, 54, 55, 0], // victim Q, attacker K, Q, R, B, N, P, None
    //     [0, 0, 0, 0, 0, 0, 0],       // victim K, attacker K, Q, R, B, N, P, None
    //     [0, 0, 0, 0, 0, 0, 0],       // victim None, attacker K, Q, R, B, N, P, None
    // ];

    /*
        (Victims) Pawn Knight Bishop   Rook  Queen   King
      (Attackers)
            Pawn   105    205    305    405    505    605
          Knight   104    204    304    404    504    604
          Bishop   103    203    303    403    503    603
            Rook   102    202    302    402    502    602
           Queen   101    201    301    401    501    601
            King   100    200    300    400    500    600
    */
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
        TranspositionTable.Table.Clear();

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
      return bestMoves.Count == 0 ? moves : bestMoves.ToArray();
    }

    int MakeMove(Board board, Move move, int depth, int alpha, int beta, int color)
    {
      board.MakeMove(move);
      if (board.IsInCheckmate())
      {
        board.UndoMove(move);
        return depth == Depth ? 100000 : 90000 + board.PlyCount;
      }

      int score = -NegaMax(depth, board, -beta, -alpha, -color/*, move.StartSquare.Name + move.TargetSquare.Name*/);
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

    int NegaMax(int depth, Board board, int alpha, int beta, int color/*, string prevMove*/)
    {
      ulong key = board.ZobristKey;
      int flag = 1, ply = board.PlyCount;

      int? entry = TranspositionTable.Get(key, depth, alpha, beta, ply);
      if (entry != null)
        return (int)entry;

      if (depth == 0)
      {
        int val = Quiescence(board, alpha, beta, color);
        TranspositionTable.Store(key, val, depth, flag: 0, ply/*, prevMove*/);
        return val;
      }

      Move[] orderedMoves = GetOrderedMoves(board);

      // string bestMove = prevMove;
      foreach (Move move in orderedMoves)
      {
        // string currentMove = move.StartSquare.Name + move.TargetSquare.Name;
        int score = MakeMove(board, move, depth - 1, alpha, beta, color);

        if (score >= beta)
        {
          TranspositionTable.Store(key, beta, depth, flag: 2, ply/*, currentMove*/);
          return beta;
        }

        if (score > alpha)
        {
          // bestMove = currentMove;
          flag = 0;
          alpha = score;
        }
      }

      TranspositionTable.Store(key, alpha, depth, flag, ply/*, bestMove*/);
      return alpha;
    }

    int Quiescence(Board board, int alpha, int beta, int color)
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