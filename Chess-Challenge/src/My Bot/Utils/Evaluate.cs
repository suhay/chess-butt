using System;
using ChessChallenge.API;

public partial class MyBot3_Base
{
  int[,] mvv_lva = new int[6, 6]
  {
    {105, 205, 305, 405, 505, 605},
    {104, 204, 304, 404, 504, 604},
    {103, 203, 303, 403, 503, 603},
    {102, 202, 302, 402, 502, 602},
    {101, 201, 301, 401, 501, 601},
    {100, 200, 300, 400, 500, 600},
  };

  protected int Evaluate(Board board, int alpha, int beta, int color)
  {
    if (UseQuiescence)
      return Quiescence(board, alpha, beta, color);

    return EvaluateBoard(board, color);
  }

  int Quiescence(Board board, int alpha, int beta, int color, int? depth = null)
  {
    if (UseCheckInQuiescence && board.IsInCheck())
      return -NegaMax(1, board, -beta, -alpha, -color);

    int eval = EvaluateBoard(board, color);

    if (eval >= beta)
      return beta;

    if (eval > alpha)
      alpha = eval;

    if (depth != null)
    {
      if (depth == 0)
        return alpha;
      depth--;
    }

    if (QuiescenceHardPlyLimit > 0 && depth == null)
      depth = QuiescenceHardPlyLimit;

    Move[] captureMoves = board.GetLegalMoves(true);
    Move[] orderedMoves = SortMoves(captureMoves, board);

    foreach (Move move in orderedMoves)
    {
      board.MakeMove(move);
      int score = -Quiescence(board, -beta, -alpha, -color, depth);
      board.UndoMove(move);

      if (score >= beta)
        return beta;

      if (score > alpha)
        alpha = score;
    }

    return alpha;
  }

  protected int EvaluateBoard(Board board, int color)
  {
    PieceList[] pieceList = board.GetAllPieceLists();
    int materialVal = 0;

    foreach (PieceList pieces in pieceList)
    {
      foreach (Piece piece in pieces)
      {
        if (piece.IsWhite)
          materialVal += EvaluatePiece(piece);
        else
          materialVal -= EvaluatePiece(piece);
      }
    }

    int boardVal = Juice.GetJuice(experiments, board);

    return color * (materialVal + boardVal);
  }

  int EvaluatePiece(Piece piece)
  {
    return Juice.GetJuice(experiments, piece);
  }

  int EvaluateMove(Board board, Move move, int ply, bool isRoot = false)
  {
    if (UsePV && isRoot && PVTable.ContainsKey(ply) && PVTable[ply] == move)
    {
      Console.WriteLine("All according to plan...");
      return 20000;
    }

    if (move.IsCapture)
    {
      int weight = mvv_lva[(int)move.MovePieceType - 1, (int)move.CapturePieceType - 1] + 10000;
      return weight + (!board.SquareIsAttackedByOpponent(move.TargetSquare) ? 700 : 0);
    }

    if (KillerMoves.K1.ContainsKey(ply) && KillerMoves.K1[ply] == move)
      return 9000;

    if (KillerMoves.K2.ContainsKey(ply) && KillerMoves.K2[ply] == move)
      return 8000;

    return 0;
  }
}

// static inline int score_move(int move)
// {
//     // PV move
//     if (pv_table[0][ply] == move)
//         // score 20000 ( search it first )
//         return 20000;

//     // init current move score
//     int score;

//     // score MVV LVA (scores 0 for quiete moves)
//     score = mvv_lva[board[get_move_source(move)]][board[get_move_target(move)]];         

//     // on capture
//     if (get_move_capture(move))
//     {
//         // add 10000 to current score
//         score += 10000;
//     }

//     // on quiete move
//     else {
//         // on 1st killer move
//         if (killer_moves[0][ply] == move)
//             // score 9000
//             score = 9000;

//         // on 2nd killer move
//         else if (killer_moves[1][ply] == move)
//             // score 8000
//             score = 8000;

//         // on history move (previous alpha's best score)
//         else
//             // score with history depth
//             score = history_moves[board[get_move_source(move)]][get_move_target(move)] + 7000;
//     }

//     // return move score
//     return score;
// }