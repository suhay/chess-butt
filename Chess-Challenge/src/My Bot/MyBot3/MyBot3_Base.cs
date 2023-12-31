﻿using System.Collections.Generic;
using ChessChallenge.API;

public abstract partial class MyBot3_Base : IChessBot
{
  protected TT.TranspositionTable transpositionTable = new();
  protected ExperimentType[] experiments;
  protected KillerMoves KillerMoves = new();
  protected Dictionary<int, Move> PVTable = new();
  protected Dictionary<int, Move> movesToScore = new();

  int color = 1;
  bool logging = false;
  int depth = 2;
  int wiggleThreshold = 5;
  int sortDepth = 0;
  int bestGuess = 0;
  int iterativeDepth = 0;
  bool useMTD = false;
  int capturePriority = 0;
  bool useQuiescence = false;
  int quiescenceHardPlyLimit = 0;
  bool useCheckInQuiescence = false;
  bool useTT = false;
  bool useTT2 = true;
  int nodes = 0;
  string moveSort = "";
  bool useKillerMoves = false;
  bool endGameDeepening = false;
  bool usePV = false;
  bool useNullMovePruning = false;
  bool isInEndGame = false;
  protected Move nextMove;

  bool failHard = false; // NegaMax fail hard
  bool abTest = false;

  public static Dictionary<PieceType, int> PieceVal = new()
  {
    {PieceType.None, 0},
    {PieceType.Pawn, 100},
    {PieceType.Rook, 500},
    {PieceType.Knight, 300},
    {PieceType.Bishop, 300},
    {PieceType.Queen, 900},
    {PieceType.King, 10000}
  };

  public static readonly int CheckMate = 100000; // Checkmate in 1
  public static readonly int CheckMateSoon = 90000; // Checkmate in N turns
  public static readonly int Inf = int.MaxValue;

  public int Color { get => color; protected set => color = value; }
  public int Depth { get => depth; protected set => depth = value; }
  public int WiggleThreshold { get => wiggleThreshold; protected set => wiggleThreshold = value; }

  public string MoveSort { get => moveSort; protected set => moveSort = value; }
  public int SortDepth { get => sortDepth; protected set => sortDepth = value; }
  public int IterativeDepth { get => iterativeDepth; protected set => iterativeDepth = value; }
  public int CapturePriority { get => capturePriority; protected set => capturePriority = value; }

  public bool UseTT { get => useTT; protected set => useTT = value; }
  public bool UseTT2 { get => useTT2; protected set => useTT2 = value; }

  public bool UseMTD { get => useMTD; protected set => useMTD = value; }
  public int BestGuess { get => bestGuess; protected set => bestGuess = value; }

  public bool UseQuiescence { get => useQuiescence; protected set => useQuiescence = value; }
  public bool UseCheckInQuiescence { get => useCheckInQuiescence; protected set => useCheckInQuiescence = value; }
  public int QuiescenceHardPlyLimit { get => quiescenceHardPlyLimit; protected set => quiescenceHardPlyLimit = value; }

  public bool UseKillerMoves { get => useKillerMoves; protected set => useKillerMoves = value; }
  public bool UsePV { get => usePV; protected set => usePV = value; }
  public bool UseNullMovePruning { get => useNullMovePruning; protected set => useNullMovePruning = value; }

  public int Nodes { get => nodes; protected set => nodes = value; }
  // public int Ply { get => ply; protected set => ply = value; }
  public bool IsInEndGame { get => isInEndGame; protected set => isInEndGame = value; }
  public bool Logging { get => logging; protected set => logging = value; }
  public bool ABTest { get => abTest; protected set => abTest = value; }
  public bool FailHard { get => failHard; protected set => failHard = value; }
  public bool EndGameDeepening { get => endGameDeepening; protected set => endGameDeepening = value; }

  public abstract Move Think(Board board, Timer timer);
}