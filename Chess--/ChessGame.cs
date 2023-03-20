using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Chess;

public class ChessGame {
    private ChessColor _currentPlayer = ChessColor.Black;
    private readonly ChessPiece[] _game;
    private bool _bkCastlingRights = true;
    private bool _bqCastlingRights = true;
    private ChessMove _lastMove;
    private bool _wkCastlingRights = true;
    private bool _wqCastlingRights = true;
    private readonly bool _isEnPassantPossible;

    public ChessGame() {
        _game = new[] {
            ChessPiece.R, ChessPiece.N, ChessPiece.B, ChessPiece.Q, ChessPiece.K, ChessPiece.B, ChessPiece.N,
            ChessPiece.R, ChessPiece.P, ChessPiece.P, ChessPiece.P, ChessPiece.P, ChessPiece.P, ChessPiece.P,
            ChessPiece.P, ChessPiece.P, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty,
            ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty,
            ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty,
            ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty,
            ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty,
            ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.Empty, ChessPiece.p, ChessPiece.p,
            ChessPiece.p, ChessPiece.p, ChessPiece.p, ChessPiece.p, ChessPiece.p, ChessPiece.p, ChessPiece.r,
            ChessPiece.n, ChessPiece.b, ChessPiece.q, ChessPiece.k, ChessPiece.b, ChessPiece.n, ChessPiece.r
        };
        for (int i = 16; i < 48; i++) {
            _game[i] = ChessPiece.Empty;
        }

        _lastMove = null;

        CurrentPlayer = _currentPlayer;
        Game = _game;
    }

    private ChessGame(ChessPiece[] game, ChessColor currentPlayer, bool isEnPassantPossible, bool bkCastlingRights, bool bqCastlingRights, bool wkCastlingRights, bool wqCastlingRights) {
        _game = game;
        _currentPlayer = currentPlayer;
        _lastMove = null;
        _isEnPassantPossible = isEnPassantPossible;
        _bkCastlingRights = bkCastlingRights;
        _bqCastlingRights = bqCastlingRights;
        _wkCastlingRights = wkCastlingRights;
        _wqCastlingRights = wqCastlingRights;
        CurrentPlayer = _currentPlayer;
    }

    public ChessPiece[] Game { get; }

    public ChessColor CurrentPlayer {
        get => _currentPlayer;
        set => _currentPlayer = value;
    }

    private bool IsEnPassantPossible() {
        return _lastMove != null && (_lastMove.ERank - _lastMove.SRank == 2 && _game[8*_lastMove.ERank+_lastMove.EFile] == ChessPiece.P && _currentPlayer == ChessColor.Black ||
               _lastMove.ERank - _lastMove.SRank == -2 && _game[8*_lastMove.ERank+_lastMove.EFile] == ChessPiece.p && _currentPlayer == ChessColor.White);
    }

    public ChessGame DeepCopy() {
        ChessPiece[] newGame = new ChessPiece[64];
        for (int i = 0; i < 64; i++) {
            newGame[i] = _game[i];
        }

        return new ChessGame(newGame, _currentPlayer == ChessColor.Black ? ChessColor.White : ChessColor.Black, IsEnPassantPossible(), _bkCastlingRights, _bqCastlingRights,
            _wkCastlingRights, _wqCastlingRights);
    }

    private bool IsSquareInCheck(sbyte rank, sbyte file, ChessColor opponent) {
        for (int i = 0; i < 64; i++) {
            if ((char.IsUpper(_game[i].ToString(), 0) && opponent == ChessColor.White) ||
                (char.IsLower(_game[i].ToString(), 0) && opponent == ChessColor.Black)) {
                ChessMove move = new((sbyte)(i % 8), (sbyte)(i / 8), file, rank);
                if (ValidateMove(move, opponent, _game, true)) {
                    return true;
                }
            }
        }

        return false;
    }

    public bool IsPlayerInCheck(ChessColor player, ChessPiece[] board) {
        int k = Array.IndexOf(board, player == ChessColor.White ? ChessPiece.K : ChessPiece.k);
        for (int i = 0; i < 64; i++) {
            if (((char.IsUpper(board[i].ToString(), 0) && player == ChessColor.Black) ||
                 (char.IsLower(board[i].ToString(), 0) && player == ChessColor.White)) && ValidateMove(
                    new ChessMove((sbyte)(i % 8), (sbyte)(i / 8), (sbyte)(k % 8), (sbyte)(k / 8)),
                    player == ChessColor.White ? ChessColor.Black : ChessColor.White, board, true)) {
                return true;
            }
        }

        return false;
    }

    public bool PlayerHasLegalMove(ChessColor player) {
        for (int i = 0; i < 64; i++) {
            for (int j = 0; j < 64; j++) {
                if (ValidateMove(new ChessMove((sbyte)(i % 8), (sbyte)(i / 8), (sbyte)(j % 8), (sbyte)(j / 8)), player,
                        _game,
                        false)) {
                    return true;
                }
            }
        }

        return false;
    }

    // im so sorry for anyone who has to touch this ever
    public bool ValidateMove(ChessMove move, ChessColor player, ChessPiece[] game, bool isHypothetical) {
        int sIndex = move.SRank * 8 + move.SFile;
        // check that a piece is being moved
        if (game[sIndex] == ChessPiece.Empty) {
            return false;
        }

        // check that the move distance is nonzero
        if (move.EFile - move.SFile == 0 && move.ERank - move.SRank == 0) {
            return false;
        }

        int eIndex = move.ERank * 8 + move.EFile;
        ChessPiece start = game[sIndex];

        // check that the right player is moving
        if ((char.IsUpper(start.ToString(), 0) && player == ChessColor.Black) ||
            (char.IsLower(start.ToString(), 0) && player == ChessColor.White)) {
            return false;
        }

        // check that the shape of the move is valid
        switch (start) {
            case ChessPiece.R:
            case ChessPiece.r:
                if (move.EFile - move.SFile != 0 && move.ERank - move.SRank != 0) {
                    return false;
                }

                if (move.EFile - move.SFile == 0) {
                    for (int i = Math.Min(move.SRank, move.ERank) + 1; i < Math.Max(move.ERank, move.SRank); i++) {
                        if (game[i * 8 + move.SFile] != ChessPiece.Empty) {
                            return false;
                        }
                    }
                }

                if (move.ERank - move.SRank == 0) {
                    for (int i = Math.Min(move.SFile, move.EFile) + 1; i < Math.Max(move.SFile, move.EFile); i++) {
                        if (game[move.SRank * 8 + i] != ChessPiece.Empty) {
                            return false;
                        }
                    }
                }

                break;
            case ChessPiece.N:
            case ChessPiece.n:
                if ((move.EFile - move.SFile) * (move.EFile - move.SFile) +
                    (move.ERank - move.SRank) * (move.ERank - move.SRank) != 5) {
                    return false;
                }

                break;
            case ChessPiece.B:
            case ChessPiece.b:
                if (Math.Abs(move.EFile - move.SFile) != Math.Abs(move.ERank - move.SRank)) {
                    return false;
                }

                if (move.ERank - move.SRank == move.EFile - move.SFile) {
                    if (move.EFile < move.SFile) {
                        for (int i = 1; i < move.SFile - move.EFile; i++) {
                            if (game[8 * move.SRank + move.SFile - 9 * i] != ChessPiece.Empty) {
                                return false;
                            }
                        }
                    } else {
                        for (int i = 1; i < move.EFile - move.SFile; i++) {
                            if (game[8 * move.SRank + move.SFile + 9 * i] != ChessPiece.Empty) {
                                return false;
                            }
                        }
                    }
                }

                if (move.ERank - move.SRank == move.SFile - move.EFile) {
                    if (move.SFile < move.EFile) {
                        for (int i = 1; i < move.EFile - move.SFile; i++) {
                            if (game[8 * move.SRank + move.SFile - 7 * i] != ChessPiece.Empty) {
                                return false;
                            }
                        }
                    } else {
                        for (int i = 1; i < move.SFile - move.EFile; i++) {
                            if (game[8 * move.SRank + move.SFile + 7 * i] != ChessPiece.Empty) {
                                return false;
                            }
                        }
                    }
                }

                break;
            case ChessPiece.K:
                if (Math.Pow(move.EFile - move.SFile, 2) + Math.Pow(move.ERank - move.SRank, 2) > 4 || Math.Pow(move.EFile - move.SFile, 2) + Math.Pow(move.ERank - move.SRank, 2) > 2.5 && move.ERank - move.SRank != 0) {
                    return false;
                }

                if (IsSquareInCheck(move.ERank, move.EFile, ChessColor.Black)) {
                    return false;
                }

                if (move.EFile - move.SFile == 2) {
                    if (!_wkCastlingRights) {
                        return false;
                    }
                    for (int i = 4; i < 7; i++) {
                        if (game[i] != ChessPiece.Empty ||
                            IsSquareInCheck((sbyte)(i / 8), (sbyte)(i % 8), ChessColor.Black)) {
                            return false;
                        }
                    }
                }

                if (move.EFile - move.SFile == -2) {
                    if (!_wqCastlingRights) {
                        return false;
                    }
                    for (int i = 2; i <= 4; i++) {
                        if (game[i] != ChessPiece.Empty ||
                            IsSquareInCheck((sbyte)(i / 8), (sbyte)(i % 8), ChessColor.Black)) {
                            return false;
                        }
                    }
                }

                break;
            case ChessPiece.k:
                if (Math.Pow(move.EFile - move.SFile, 2) + Math.Pow(move.ERank - move.SRank, 2) > 4 || Math.Pow(move.EFile - move.SFile, 2) + Math.Pow(move.ERank - move.SRank, 2) > 2.5 && move.ERank - move.SRank != 0) {
                    return false;
                }

                if (IsSquareInCheck(move.ERank, move.EFile, ChessColor.White)) {
                    return false;
                }

                if (move.EFile - move.SFile == 2) {
                    if (!_bkCastlingRights) {
                        return false;
                    }
                    for (int i = 61; i < 63; i++) {
                        if (game[i] != ChessPiece.Empty ||
                            IsSquareInCheck((sbyte)(i / 8), (sbyte)(i % 8), ChessColor.White)) {
                            return false;
                        }
                    }
                }

                if (move.EFile - move.SFile == -2) {
                    if (!_bqCastlingRights) {
                        return false;
                    }
                    for (int i = 58; i < 60; i++) {
                        if (game[i] != ChessPiece.Empty ||
                            IsSquareInCheck((sbyte)(i / 8), (sbyte)(i % 8), ChessColor.White)) {
                            return false;
                        }
                    }
                }

                break;
            case ChessPiece.Q:
            case ChessPiece.q:
                if (move.EFile - move.SFile != 0 && move.ERank - move.SRank != 0 &&
                    Math.Abs(move.EFile - move.SFile) != Math.Abs(move.ERank - move.SRank)) {
                    return false;
                }

                if (move.EFile - move.SFile == 0) {
                    for (int i = Math.Min(move.SRank, move.ERank) + 1; i < Math.Max(move.ERank, move.SRank); i++) {
                        if (game[i * 8 + move.SFile] != ChessPiece.Empty) {
                            return false;
                        }
                    }
                }

                if (move.ERank - move.SRank == 0) {
                    for (int i = Math.Min(move.SFile, move.EFile) + 1; i < Math.Max(move.SFile, move.EFile); i++) {
                        if (game[move.SRank * 8 + i] != ChessPiece.Empty) {
                            return false;
                        }
                    }
                }

                if (move.ERank - move.SRank == move.EFile - move.SFile) {
                    if (move.EFile < move.SFile) {
                        for (int i = 1; i < move.SFile - move.EFile; i++) {
                            if (game[8 * move.SRank + move.SFile - 9 * i] != ChessPiece.Empty) {
                                return false;
                            }
                        }
                    } else {
                        for (int i = 1; i < move.EFile - move.SFile; i++) {
                            if (game[8 * move.SRank + move.SFile + 9 * i] != ChessPiece.Empty) {
                                return false;
                            }
                        }
                    }
                }

                if (move.ERank - move.SRank == move.SFile - move.EFile) {
                    if (move.SFile < move.EFile) {
                        for (int i = 1; i < move.EFile - move.SFile; i++) {
                            if (game[8 * move.SRank + move.SFile - 7 * i] != ChessPiece.Empty) {
                                return false;
                            }
                        }
                    } else {
                        for (int i = 1; i < move.SFile - move.EFile; i++) {
                            if (game[8 * move.SRank + move.SFile + 7 * i] != ChessPiece.Empty) {
                                return false;
                            }
                        }
                    }
                }

                break;
            case ChessPiece.P:
                if (Math.Abs(move.EFile - move.SFile) > 1 ||
                    (Math.Abs(move.EFile - move.SFile) == 1 && move.ERank - move.SRank != 1) ||
                    (move.EFile - move.SFile == 0 &&
                     ((move.SRank == 1 && (move.ERank - move.SRank > 2 || move.ERank - move.SRank < 1)) ||
                      (move.SRank != 1 && move.ERank - move.SRank != 1)))) {
                    return false;
                }

                if (move.EFile - move.SFile == 0 && game[8 * move.ERank + move.EFile] != ChessPiece.Empty) {
                    return false;
                }

                if (move.SFile - move.EFile != 0 && game[8 * move.ERank + move.EFile] == ChessPiece.Empty && (_lastMove == null ||
                    !_lastMove.Equals(new ChessMove(move.EFile, 1, move.EFile, 5)) || move.ERank != 2 || move.SRank != 4)) {
                    return false;
                }

                break;
            case ChessPiece.p:
                if (Math.Abs(move.EFile - move.SFile) > 1 ||
                    (Math.Abs(move.EFile - move.SFile) == 1 && move.SRank - move.ERank != 1) ||
                    (move.EFile - move.SFile == 0 &&
                     ((move.SRank == 6 && (move.ERank - move.SRank < -2 || move.ERank - move.SRank > -1)) ||
                      (move.SRank != 6 && move.SRank - move.ERank != 1)))) {
                    return false;
                }

                if (move.EFile - move.SFile == 0 && game[8 * move.ERank + move.EFile] != ChessPiece.Empty) {
                    return false;
                }

                if (move.SFile - move.EFile != 0 && game[8 * move.ERank + move.EFile] == ChessPiece.Empty && (_lastMove == null ||
                    !_lastMove.Equals(new ChessMove(move.EFile, 1, move.EFile, 3)) || move.ERank != 2 || move.SRank != 3)) {
                    return false;
                }

                break;
        }

        // check that the destination square is valid (no piece or opposing piece)
        if (game[eIndex] != ChessPiece.Empty &&
            ((char.IsUpper(game[eIndex].ToString(), 0) && player == ChessColor.White) ||
             (char.IsLower(game[eIndex].ToString(), 0) && player == ChessColor.Black))) {
            return false;
        }

        ChessPiece[] board = new ChessPiece[64];
        for (int i = 0; i < 64; i++) {
            board[i] = game[i];
        }

        ExecuteMove(move, true, board);
        if (!isHypothetical && IsPlayerInCheck(player, board)) {
            return false;
        }

        return true;
    }

    public bool ExecuteMove(ChessMove move, bool isHypothetical, ChessPiece[] board = null) {
        bool result = false;
        
        board ??= _game;

        int startIndex = 8 * move.SRank + move.SFile;
        int destIndex = 8 * move.ERank + move.EFile;

        if (board[startIndex] == ChessPiece.p && move.ERank == 0 || board[startIndex] == ChessPiece.P && move.ERank == 7) {
            result = true;
        }

        if ((board[startIndex] == ChessPiece.p || board[startIndex] == ChessPiece.P) && move.SFile != move.EFile &&
            board[destIndex] == ChessPiece.Empty) {
            board[destIndex > 31 ? destIndex - 8 : destIndex + 8] = ChessPiece.Empty;
        } else if ((board[startIndex] == ChessPiece.k || board[startIndex] == ChessPiece.K) &&
                   Math.Abs(move.EFile - move.SFile) == 2) {
            if (move.EFile > move.SFile) {
                board[destIndex - 1] = board[destIndex + 1];
                board[destIndex + 1] = ChessPiece.Empty;
            } else {
                board[destIndex + 1] = board[destIndex - 2];
                board[destIndex - 2] = ChessPiece.Empty;
            }
        }

        board[destIndex] = board[startIndex];
        board[startIndex] = ChessPiece.Empty;
        if (result) {
            board[destIndex] = _currentPlayer == ChessColor.Black ? ChessPiece.q : ChessPiece.Q;
        }
        if (!isHypothetical && board[destIndex] == ChessPiece.k) {
            _bkCastlingRights = false;
            _bqCastlingRights = false;
        }

        if (!isHypothetical && board[destIndex] == ChessPiece.K) {
            _wkCastlingRights = false;
            _wqCastlingRights = false;
        }

        if (!isHypothetical && board[destIndex] == ChessPiece.R) {
            if (move.SFile == 0 && move.SRank == 0) {
                _wqCastlingRights = false;
            } else if (move.SFile == 7 && move.SRank == 0) {
                _wkCastlingRights = false;
            }
        }

        if (!isHypothetical && board[destIndex] == ChessPiece.r) {
            if (move.SFile == 0 && move.SRank == 7) {
                _bqCastlingRights = false;
            } else if (move.SFile == 7 && move.SRank == 7) {
                _bkCastlingRights = false;
            }
        }

        if (!isHypothetical) {
            _lastMove = move;
        }

        return result;
    }

    public override int GetHashCode() {
        return _currentPlayer.GetHashCode() + ((IStructuralEquatable)_game).GetHashCode(EqualityComparer<Enum>.Default) + _bkCastlingRights.GetHashCode() +
               _bqCastlingRights.GetHashCode() + _wkCastlingRights.GetHashCode() + _wqCastlingRights.GetHashCode() +
               _isEnPassantPossible.GetHashCode();
    }

    public override bool Equals(object obj) {
        if (obj.GetType() != typeof(ChessGame)) {
            return false;
        }

        return _currentPlayer == ((ChessGame)obj)._currentPlayer && _game.SequenceEqual(((ChessGame)obj)._game) &&
               _bkCastlingRights == ((ChessGame)obj)._bkCastlingRights &&
               _bqCastlingRights == ((ChessGame)obj)._bqCastlingRights &&
               _wkCastlingRights == ((ChessGame)obj)._wkCastlingRights &&
               _wqCastlingRights == ((ChessGame)obj)._wqCastlingRights &&
               _isEnPassantPossible == ((ChessGame)obj)._isEnPassantPossible;
    }
}