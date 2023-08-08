using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

// 924
namespace MyBot4
{
  public record Transposition(int Score, byte Depth, byte Flag /*, string move*/);

  public class TranspositionTable
  {
    public SortedDictionary<ulong, Transposition> Table = new();
    int maxTableSize = 200000;

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

      Transposition newTransposition = new(score, (byte)depth, (byte)flag);
      if (Table.TryGetValue(key, out var existingEntry))
      {
        Table.Remove(key);
        // Replace on Depth if the new entry has higher depth, or always replace if it's the same
        Table.Add(key, depth >= existingEntry.Depth
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
    int Depth = 3;
    Dictionary<PieceType, int> PieceVal = new()
    {
      { PieceType.None, 0 },
      { PieceType.Pawn, 100 },
      { PieceType.Knight, 300 },
      { PieceType.Bishop, 300 },
      { PieceType.Rook, 500 },
      { PieceType.Queen, 900 },
      { PieceType.King, 0 }
    };
    TranspositionTable TranspositionTable = new();
    int Nodes = 0;

    public Move Think(Board board, Timer timer)
    {
      Nodes = 0;
      Move[] bestMoves = NegaMaxRoot(board, Depth, -Inf, Inf, board.IsWhiteToMove ? 1 : -1);
      Random rng = new();
      Move nextMove = bestMoves[rng.Next(bestMoves.Length)];
      Console.WriteLine("Nodes: {0}", Nodes);
      return nextMove;
    }

    Move[] NegaMaxRoot(Board board, int depth, int alpha, int beta, int color)
    {
      // Repetition history clears when a pawn moves or a capture is made. We can safely clear the transposition table
      // if (board.GameRepetitionHistory.Length == 0)
      //   TranspositionTable.Table.Clear();

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
      Nodes++;
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
          // ? mvv_lva[(int)move.MovePieceType - 1, (int)move.CapturePieceType - 1]
          //   + (!board.SquareIsAttackedByOpponent(move.TargetSquare) ? 10700 : 10000)
          ? (100 * (int)move.CapturePieceType) - (int)move.MovePieceType + 6
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
        Nodes++;
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