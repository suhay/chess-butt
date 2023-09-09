using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot2 : IChessBot
{
  public static Dictionary<PieceType, int> PieceVal = new Dictionary<PieceType, int>
  {
    {PieceType.None, 0},
    {PieceType.Pawn, 100},
    {PieceType.Rook, 500},
    {PieceType.Knight, 300},
    {PieceType.Bishop, 300},
    {PieceType.Queen, 900},
    {PieceType.King, 10000}
  };

  ExperimentType[] Experiments = new ExperimentType[]
  {
  };
  int Depth = 2;
  public static int CapturePriority = 2;
  int WiggleThreshold = 15;
  int IterativeDepth = 0;

  int CheckMate = 10000;
  int Min = -9999;
  int Max = 9999;

  Dictionary<ulong, TT.Transposition> TranspositionTable = new Dictionary<ulong, TT.Transposition>();

  public static bool Color;

  public Move Think(Board board, Timer timer)
  {
    if (board.GameRepetitionHistory.Length == 0)
    {
      TranspositionTable.Clear();
    }

    int color = board.IsWhiteToMove ? 1 : -1;
    Color = board.IsWhiteToMove;

    // Move[] moves = board.GetLegalMoves();
    Move[] moves = _board.GetLegalMoves(board, color);
    // order moves

    Move[] bestMoves = NegaMaxHandler(moves, board, Depth, color);

    // Deep thinking
    bestMoves = IterativeDepth > 0
      ? NegaMaxHandler(bestMoves.AsQueryable().Take(5).ToArray(), board, IterativeDepth, color)
      : bestMoves;

    Random rng = new();
    Move nextMove = bestMoves.Length > 0
      ? bestMoves[rng.Next(bestMoves.Length)]
      : moves[rng.Next(moves.Length)];

    return nextMove;
  }

  Move[] NegaMaxHandler(Move[] moves, Board board, int depth, int color)
  {
    List<Move> bestMoves = new List<Move>();
    int bestMove = Min;
    int alpha = Min;
    int beta = Max;

    foreach (Move move in moves)
    {
      // Prevents kings from wiggling back and forth
      if (board.GameMoveHistory.Take(WiggleThreshold).Where((prevMove) => prevMove.Equals(move)).Count() > 0)
      {
        continue;
      }

      var score = MakeMove(board, move, Depth, alpha, beta, color);
      _board.NegaMaxClosingReport(score, color, bestMove);

      // The move list always favors the king since it's calculated first, this way we can randomly select from all equal moves
      if (score == bestMove)
      {
        bestMoves.Add(move);
      }

      else if (score > bestMove)
      {
        bestMove = score;
        bestMoves.Clear();
        bestMoves.Add(move);
        if (score == CheckMate) break;
      }

      alpha = Math.Max(bestMove, alpha);
    }

    // Console.WriteLine("Best turn outcome - {0}", bestMove);
    return bestMoves.ToArray();
  }

  int MakeMove(Board board, Move move, int depth, int alpha, int beta, int color)
  {
    // board.MakeMove(move);
    _board.MakeMove(board, color, move, Depth, depth);

    if (board.IsInCheckmate())
    {
      _board.UndoMove(board, move, Depth, depth, color, CheckMate);
      return CheckMate;
    }

    // So the root node and child nodes can share code
    int score = depth == Depth
      ? -NegaMax(depth, board, alpha, beta, -color)
      : -NegaMax(depth, board, -beta, -alpha, -color);

    // TranspositionTable.Add(board.ZobristKey, new Transposition(score, score, alpha, beta));

    // board.UndoMove(move);
    _board.UndoMove(board, move, Depth, depth, color, score);

    return score + (CapturePriority * PieceVal[move.CapturePieceType]);
  }

  int NegaMax(int depth, Board board, int alpha, int beta, int color)
  {
    int oldAlpha = alpha;
    ulong key = board.ZobristKey;

    if (TranspositionTable.ContainsKey(key))
    {
      TT.Transposition entry = TranspositionTable[key];
      if (entry.Depth > depth)
      {
        if (entry.Flag == 0)
          return entry.Score;
        else if (entry.Flag == -1)
          alpha = Math.Max(alpha, entry.Score);
        else if (entry.Flag == 1)
          beta = Math.Min(beta, entry.Score);

        if (alpha > beta) return entry.Score;
      }
    }

    _board.NegaMaxStartingReport(board, Depth, depth, color);
    if (depth == 0)
    {
      return color * EvaluateBoard(board);
    }

    Move[] nextMoves = board.GetLegalMoves();
    // Order moves
    int score = Min;

    foreach (Move move in nextMoves)
    {
      score = Math.Max(score, MakeMove(board, move, depth - 1, alpha, beta, color));
      alpha = Math.Max(alpha, score);

      if (alpha >= beta) break;
    }

    int flag = 0;
    if (score < oldAlpha) flag = 1;
    else if (score > beta) flag = -1;

    TranspositionTable[key] = new TT.Transposition(score, depth, flag);

    return score;
  }

  int EvaluateBoard(Board board)
  {
    PieceList[] pieceList = board.GetAllPieceLists();
    int materialVal = 0;

    foreach (PieceList pieces in pieceList)
    {
      foreach (Piece piece in pieces)
      {
        if (piece.IsWhite)
        {
          materialVal += EvaluatePiece(piece);
        }
        else
        {
          materialVal -= EvaluatePiece(piece);
        }
      }
    }

    int boardVal = Juice.GetJuice(Experiments, board);

    return materialVal + boardVal;
  }

  int EvaluatePiece(Piece piece)
  {
    return Juice.GetJuice(Experiments, piece);
  }
}