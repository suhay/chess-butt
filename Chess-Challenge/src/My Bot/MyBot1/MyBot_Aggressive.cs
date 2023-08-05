using System;
using System.Collections.Generic;
using ChessChallenge.API;

// Squeeze: false
// Depth: 2
// Capture Priority: 2
// 74:26:0 vs EvilBot in 100
// Brain 583
public class MyBot_Aggressive : IChessBot
{
  bool Squeeze = false; // This must be true in order for any experiments to run
  ExperimentType[] Experiments = new ExperimentType[] { };

  int Depth = 2;
  int capturePriority = 2;

  List<Move> prevMoves = new List<Move>();
  Dictionary<PieceType, int> pieceVal = new Dictionary<PieceType, int>
  {
    {PieceType.None, 0},
    {PieceType.Pawn, 10},
    {PieceType.Rook, 50},
    {PieceType.Knight, 30},
    {PieceType.Bishop, 30},
    {PieceType.Queen, 90},
    {PieceType.King, 1000}
  };
  int CheckMate = 10000;
  int Min = -9999;
  int Max = 9999;

  public static bool Color;

  public Move Think(Board board, Timer timer)
  {
    int color = board.IsWhiteToMove ? 1 : -1;
    Color = board.IsWhiteToMove;

    Move[] moves = board.GetLegalMoves();
    List<Move> bestMoves = new List<Move>();

    int bestMove = Min;
    int alpha = Min;
    int beta = Max;

    foreach (Move move in moves)
    {
      // Prevents kings from wiggling back and forth
      if (prevMoves.Find((prevMove) => prevMove.Equals(move)) != Move.NullMove)
      {
        continue;
      }

      board.MakeMove(move);

      var (score, mod) = CheckMoveOutcome(board, move);
      if (score == 0) score = -NegaMax(Depth, board, alpha, beta, -color);

      board.UndoMove(move);

      // The move list always favors the king since it's calculated first, this way we can randomly select from all equal moves
      if (score + mod == bestMove)
      {
        bestMoves.Add(move);
      }

      else if (score + mod > bestMove)
      {
        bestMove = score + mod;
        bestMoves.Clear();
        bestMoves.Add(move);
        if (score == CheckMate) break;
      }

      alpha = Math.Max(bestMove, alpha);
    }

    Random rng = new();
    Move nextMove = bestMoves.Count > 0 ? bestMoves[rng.Next(bestMoves.Count)] : moves[rng.Next(moves.Length)];

    prevMoves.Insert(0, nextMove);
    if (prevMoves.Count > 5)
    {
      prevMoves.RemoveRange(5, prevMoves.Count - 5);
    }

    return nextMove;
  }

  int NegaMax(int depth, Board board, int alpha, int beta, int color)
  {
    if (depth == 0)
    {
      return color * EvaluateBoard(board);
    }

    Move[] nextMoves = board.GetLegalMoves();
    int bestMove = Min;

    foreach (Move move in nextMoves)
    {
      board.MakeMove(move);

      var (score, mod) = CheckMoveOutcome(board, move);
      if (score == 0) score = -NegaMax(depth - 1, board, -beta, -alpha, -color);

      board.UndoMove(move);

      bestMove = Math.Max(bestMove, score + mod);
      alpha = Math.Max(bestMove, alpha);

      if (alpha >= beta)
      {
        break;
      }
    }

    return bestMove;
  }

  (int score, int mod) CheckMoveOutcome(Board board, Move move)
  {
    if (board.IsInCheckmate())
    {
      return (CheckMate, 0);
    }

    int mod = capturePriority * pieceVal[move.CapturePieceType];

    return (0, mod);
  }

  int EvaluateBoard(Board board)
  {
    PieceList[] pieceList = board.GetAllPieceLists();

    int evaluation = 0;

    foreach (PieceList pieces in pieceList)
    {
      foreach (Piece piece in pieces)
      {
        if (piece.IsWhite)
        {
          evaluation += GetPieceValue(piece);
        }
        else
        {
          evaluation -= GetPieceValue(piece);
        }
      }
    }

    return evaluation;
  }

  int GetPieceValue(Piece piece)
  {
    int mod = Squeeze ? Juice.GetJuice(Experiments, piece) : 0;
    return pieceVal[piece.PieceType] + mod;
  }
}