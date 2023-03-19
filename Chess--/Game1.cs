using System;
using System.Collections.Generic;
using Devcade;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Chess;

public class Game1 : Game {
    private readonly GraphicsDeviceManager _graphics;
    private sbyte _curHoveredFile;
    private sbyte _curHoveredRank;
    private ChessGame _game;
    private bool _isKeyDown;
    private int _result; // 0 = game in progress; 1 = black victory; 2 = white victory; 3 = draw
    private float _scale;
    private sbyte _selectedFile;
    private sbyte _selectedRank;
    private SpriteBatch _spriteBatch;
    private int _squareSize;
    private Texture2D b;
    private Texture2D B;
    private Texture2D black_wins;
    private Texture2D dark_square;
    private Texture2D draw;
    private Texture2D glhf;
    private Texture2D hovered_square;
    private Texture2D info;
    private Texture2D k;
    private Texture2D K;
    private Texture2D light_square;
    private Texture2D n;
    private Texture2D N;
    private Texture2D p0;
    private Texture2D P0;
    private Texture2D p1;
    private Texture2D P1;
    private Texture2D p2;
    private Texture2D P2;
    private Texture2D q;
    private Texture2D Q;
    private Texture2D r;
    private Texture2D R;
    private Texture2D white_wins;
    private bool _waitingForPromotion;
    private int _promotionIndex;
    private int _fiftyMoveRuleCounter; // half-moves since last capture or pawn move
    private HashSet<ChessGame> _threefoldRepetitionCounterPartOne;
    private HashSet<ChessGame> _threefoldRepetitionCounterPartTwo;
    private List<int> _legalMovesCache;
    private Texture2D _legalMoveIndicator;

    private static readonly ChessPiece[] _wPromotionOptions =
        { ChessPiece.Q, ChessPiece.B, ChessPiece.R, ChessPiece.N };
    private static readonly ChessPiece[] _bPromotionOptions =
        { ChessPiece.q, ChessPiece.b, ChessPiece.r, ChessPiece.n };

    /// <summary>
    ///     Game constructor
    /// </summary>
    public Game1() {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
    }

    /// <summary>
    ///     Does any setup prior to the first frame that doesn't need loaded content.
    /// </summary>
    protected override void Initialize() {
        Input.Initialize(); // Sets up the input library

        // Set window size if running debug (in release it will be fullscreen)

        #region

#if DEBUG
        _graphics.PreferredBackBufferWidth = 420;
        _graphics.PreferredBackBufferHeight = 980;
        _graphics.ApplyChanges();
#else
			_graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
			_graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
			_graphics.ApplyChanges();
#endif

        #endregion

        // TODO: Add your initialization logic here

        _game = new ChessGame();
        _selectedFile = -1;
        _selectedRank = -1;
        _curHoveredFile = 1;
        _curHoveredRank = 1;
        _squareSize = _graphics.PreferredBackBufferWidth / 8;
        _scale = _squareSize / 135f;
        _result = 0;
        _waitingForPromotion = false;
        _fiftyMoveRuleCounter = 0;
        _threefoldRepetitionCounterPartOne = new HashSet<ChessGame>();
        _threefoldRepetitionCounterPartTwo = new HashSet<ChessGame>();

        _game.CurrentPlayer = ChessColor.White;
        _threefoldRepetitionCounterPartOne.Add(_game.DeepCopy());
        _game.CurrentPlayer = ChessColor.Black;
        _legalMovesCache = new List<int>();

        base.Initialize();
    }

    /// <summary>
    ///     Does any setup prior to the first frame that needs loaded content.
    /// </summary>
    protected override void LoadContent() {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // TODO: use this.Content to load your game content here
        // ex.
        // texture = Content.Load<Texture2D>("fileNameWithoutExtention");

        b = Content.Load<Texture2D>("bb");
        B = Content.Load<Texture2D>("B");
        k = Content.Load<Texture2D>("kk");
        K = Content.Load<Texture2D>("K");
        n = Content.Load<Texture2D>("nn");
        N = Content.Load<Texture2D>("N");
        q = Content.Load<Texture2D>("qq");
        Q = Content.Load<Texture2D>("Q");
        r = Content.Load<Texture2D>("rr");
        R = Content.Load<Texture2D>("R");
        p0 = Content.Load<Texture2D>("pp0");
        p1 = Content.Load<Texture2D>("pp1");
        p2 = Content.Load<Texture2D>("pp2");
        P0 = Content.Load<Texture2D>("P0");
        P1 = Content.Load<Texture2D>("P1");
        P2 = Content.Load<Texture2D>("P2");
        dark_square = Content.Load<Texture2D>("dark_square");
        light_square = Content.Load<Texture2D>("light_square");
        black_wins = Content.Load<Texture2D>("black_wins");
        white_wins = Content.Load<Texture2D>("white_wins");
        draw = Content.Load<Texture2D>("draw");
        info = Content.Load<Texture2D>("info");
        glhf = Content.Load<Texture2D>("glhf");
        _legalMoveIndicator = Content.Load<Texture2D>("legal_move");

        Color[] data = new Color[135 * 135];
        for (int i = 0; i < 135 * 135; i++) {
            data[i] = Color.Yellow;
        }

        hovered_square = new Texture2D(GraphicsDevice, 135, 135);
        hovered_square.SetData(data);
    }

    /// <summary>
    ///     Your main update loop. This runs once every frame, over and over.
    /// </summary>
    /// <param name="gameTime">This is the gameTime object you can use to get the time since last frame.</param>
    protected override void Update(GameTime gameTime) {
        Input.Update(); // Updates the state of the input library

        // Exit when both menu buttons are pressed (or escape for keyboard debuging)
        // You can change this but it is suggested to keep the keybind of both menu
        // buttons at once for gracefull exit.
        if (Keyboard.GetState().IsKeyDown(Keys.Escape) ||
            (Input.GetButton(1, Input.ArcadeButtons.Menu) &&
             Input.GetButton(2, Input.ArcadeButtons.Menu))) {
            Exit();
        }

        if (!_isKeyDown && _result != 0) {
            if (Input.GetButton(1, Input.ArcadeButtons.A2) || Input.GetButton(2, Input.ArcadeButtons.A2) || Keyboard.GetState().IsKeyDown(Keys.R)) {
                _result = 0;
                _game = new ChessGame();
            }

            return;
        }

        if (Input.GetButton(1, Input.ArcadeButtons.A2) && Input.GetButton(2, Input.ArcadeButtons.A2) || _fiftyMoveRuleCounter >= 100) {
            _result = 3;
            _isKeyDown = true;
            return;
        }

        if (!_isKeyDown && _waitingForPromotion &&
            (Input.GetButton(_game.CurrentPlayer == ChessColor.White ? 1 : 2, Input.ArcadeButtons.A3) || Keyboard.GetState().IsKeyDown(Keys.I))) {
            ChessPiece[] options = _game.CurrentPlayer == ChessColor.White ? _wPromotionOptions : _bPromotionOptions;
            _game.Game[_promotionIndex] = options[(Array.IndexOf(options, _game.Game[_promotionIndex]) + 3) % 4];
        }
        
        if (!_isKeyDown && _waitingForPromotion &&
            (Input.GetButton(_game.CurrentPlayer == ChessColor.White ? 1 : 2, Input.ArcadeButtons.A4) || Keyboard.GetState().IsKeyDown(Keys.O))) {
            ChessPiece[] options = _game.CurrentPlayer == ChessColor.White ? _wPromotionOptions : _bPromotionOptions;
            _game.Game[_promotionIndex] = options[(Array.IndexOf(options, _game.Game[_promotionIndex]) + 1) % 4];
        }
        
        if (!_isKeyDown && _waitingForPromotion &&
            (Input.GetButton(_game.CurrentPlayer == ChessColor.White ? 1 : 2, Input.ArcadeButtons.A1) || Keyboard.GetState().IsKeyDown(Keys.P))) {
            _waitingForPromotion = false;
            _game.CurrentPlayer = _game.CurrentPlayer == ChessColor.White ? ChessColor.Black : ChessColor.White;
        }

        if (!_isKeyDown && !_waitingForPromotion && (Keyboard.GetState().IsKeyDown(Keys.Enter) ||
            Input.GetButton(_game.CurrentPlayer == ChessColor.White ? 1 : 2, Input.ArcadeButtons.A1))) {
            if (_selectedFile == -1 || _selectedRank == -1) {
                _selectedFile = _curHoveredFile;
                _selectedRank = _curHoveredRank;
                for (int i = 0; i < 64; i++) {
                    if (_game.ValidateMove(new ChessMove(_selectedFile, _selectedRank, (sbyte)(i % 8), (sbyte)(i / 8)),
                            _game.CurrentPlayer, _game.Game, true)) {
                        _legalMovesCache.Add(i);
                    }
                }
            } else if (_selectedFile != _curHoveredFile || _selectedRank != _curHoveredRank) {
                ChessMove move = new(_selectedFile, _selectedRank, _curHoveredFile, _curHoveredRank);
                if (_game.ValidateMove(move, _game.CurrentPlayer, _game.Game, false)) {
                    _legalMovesCache = new List<int>();
                    if (_game.Game[8 * move.ERank + move.EFile] != ChessPiece.Empty || _game.Game[8*move.SRank+move.SFile] == ChessPiece.p || _game.Game[8*move.SRank+move.SFile] == ChessPiece.P) {
                        _fiftyMoveRuleCounter = 0;
                        _threefoldRepetitionCounterPartOne = new HashSet<ChessGame>();
                        _threefoldRepetitionCounterPartTwo = new HashSet<ChessGame>();
                    }
                    _waitingForPromotion = _game.ExecuteMove(move, false);
                    _promotionIndex = 8 * move.ERank + move.EFile;
                    _fiftyMoveRuleCounter++;

                    ChessGame temp = _game.DeepCopy();
                    if (_threefoldRepetitionCounterPartOne.Contains(temp)) {
                        if (_threefoldRepetitionCounterPartTwo.Contains(temp)) {
                            _result = 3;
                            return;
                        } 
                        _threefoldRepetitionCounterPartTwo.Add(temp);
                    } else {
                        _threefoldRepetitionCounterPartOne.Add(temp);
                    }
                    if (!_waitingForPromotion) {
                        _game.CurrentPlayer = _game.CurrentPlayer == ChessColor.Black
                            ? ChessColor.White
                            : ChessColor.Black;
                    }

                    _selectedFile = -1;
                    _selectedRank = -1;
                }

                if (!_game.PlayerHasLegalMove(_game.CurrentPlayer)) {
                    _result = _game.IsPlayerInCheck(_game.CurrentPlayer, _game.Game)
                        ? _game.CurrentPlayer == ChessColor.Black ? 2 : 1
                        : 3;
                }
            } else {
                _legalMovesCache = new List<int>();
                _selectedFile = -1;
                _selectedRank = -1;
            }
        }

        if (!_isKeyDown && !_waitingForPromotion &&
            (Keyboard.GetState().IsKeyDown(_game.CurrentPlayer == ChessColor.White ? Keys.A : Keys.Right) || (_game.CurrentPlayer == ChessColor.White ? Input.GetButton(1, Input.ArcadeButtons.StickLeft) || Input.GetButton(1, Input.ArcadeButtons.B1) : Input.GetButton(2, Input.ArcadeButtons.StickRight) || Input.GetButton(2, Input.ArcadeButtons.B4)))) {
            _curHoveredFile = (sbyte)Math.Max(0, _curHoveredFile - 1);
        }

        if (!_isKeyDown && !_waitingForPromotion &&
            (Keyboard.GetState().IsKeyDown(_game.CurrentPlayer == ChessColor.White ? Keys.D : Keys.Left) || (_game.CurrentPlayer == ChessColor.White ? Input.GetButton(1, Input.ArcadeButtons.StickRight) || Input.GetButton(1, Input.ArcadeButtons.B4) : Input.GetButton(2, Input.ArcadeButtons.StickLeft) || Input.GetButton(2, Input.ArcadeButtons.B1)))) {
            _curHoveredFile = (sbyte)Math.Min(7, _curHoveredFile + 1);
        }

        if (!_isKeyDown && !_waitingForPromotion && (Keyboard.GetState().IsKeyDown(_game.CurrentPlayer == ChessColor.White ? Keys.W : Keys.Up) || Input.GetButton(_game.CurrentPlayer == ChessColor.White ? 1 : 2, Input.ArcadeButtons.StickUp) || Input.GetButton(_game.CurrentPlayer == ChessColor.White ? 1 : 2, Input.ArcadeButtons.B3))) {
            _curHoveredRank = _game.CurrentPlayer == ChessColor.White
                ? (sbyte)Math.Min(7, _curHoveredRank + 1)
                : (sbyte)Math.Max(0, _curHoveredRank - 1);
        }

        if (!_isKeyDown && !_waitingForPromotion &&
            (Keyboard.GetState().IsKeyDown(_game.CurrentPlayer == ChessColor.White ? Keys.S : Keys.Down) || Input.GetButton(_game.CurrentPlayer == ChessColor.White ? 1 : 2, Input.ArcadeButtons.StickDown) || Input.GetButton(_game.CurrentPlayer == ChessColor.White ? 1 : 2, Input.ArcadeButtons.B2))) {
            _curHoveredRank = _game.CurrentPlayer == ChessColor.White
                ? (sbyte)Math.Max(0, _curHoveredRank - 1)
                : (sbyte)Math.Min(7, _curHoveredRank + 1);
        }

        // im so sorry
        if (Keyboard.GetState().GetPressedKeys().Length == 0 && !Input.GetButton(1, Input.ArcadeButtons.A1) && !Input.GetButton(1, Input.ArcadeButtons.A2) && !Input.GetButton(1, Input.ArcadeButtons.A3) && !Input.GetButton(1, Input.ArcadeButtons.A4) && !Input.GetButton(1, Input.ArcadeButtons.B1) && !Input.GetButton(1, Input.ArcadeButtons.B2) && !Input.GetButton(1, Input.ArcadeButtons.B3) && !Input.GetButton(1, Input.ArcadeButtons.B4) && !Input.GetButton(1, Input.ArcadeButtons.StickUp) && !Input.GetButton(1, Input.ArcadeButtons.StickDown) && !Input.GetButton(1, Input.ArcadeButtons.StickLeft) && !Input.GetButton(1, Input.ArcadeButtons.StickRight)&& !Input.GetButton(2, Input.ArcadeButtons.A1) && !Input.GetButton(2, Input.ArcadeButtons.A2) && !Input.GetButton(2, Input.ArcadeButtons.A3) && !Input.GetButton(2, Input.ArcadeButtons.A4) && !Input.GetButton(2, Input.ArcadeButtons.B1) && !Input.GetButton(2, Input.ArcadeButtons.B2) && !Input.GetButton(2, Input.ArcadeButtons.B3) && !Input.GetButton(2, Input.ArcadeButtons.B4) && !Input.GetButton(2, Input.ArcadeButtons.StickUp) && !Input.GetButton(2, Input.ArcadeButtons.StickDown) && !Input.GetButton(2, Input.ArcadeButtons.StickLeft) && !Input.GetButton(2, Input.ArcadeButtons.StickRight)) {
            _isKeyDown = false;
        } else {
            _isKeyDown = true;
        }

        base.Update(gameTime);
    }

    /// <summary>
    ///     Your main draw loop. This runs once every frame, over and over.
    /// </summary>
    /// <param name="gameTime">This is the gameTime object you can use to get the time since last frame.</param>
    protected override void Draw(GameTime gameTime) {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();
        // TODO: Add your drawing code here
        int index = 8 * _curHoveredRank + _curHoveredFile;
        int selIndex = 8 * _selectedRank + _selectedFile;
        Vector2 position;
        Vector2 oppPos;
        Vector2 selPos;
        Vector2 selOppPos;
        if (_game.CurrentPlayer == ChessColor.Black) {
            position = new Vector2(_graphics.PreferredBackBufferWidth - _squareSize - index % 8 * _squareSize, index / 8 * _squareSize);
            oppPos = new Vector2(index % 8 * _squareSize, _graphics.PreferredBackBufferHeight - _squareSize - index / 8 * _squareSize);
            selPos = new Vector2(_graphics.PreferredBackBufferWidth - _squareSize - selIndex % 8 * _squareSize, selIndex / 8 * _squareSize);
            selOppPos = new Vector2(selIndex % 8 * _squareSize, _graphics.PreferredBackBufferHeight - _squareSize - selIndex / 8 * _squareSize);
        } else {
            position = new Vector2(index % 8 * _squareSize, _graphics.PreferredBackBufferHeight - _squareSize - index / 8 * _squareSize);
            oppPos = new Vector2(_graphics.PreferredBackBufferWidth - _squareSize - index % 8 * _squareSize, index / 8 * _squareSize);
            selPos = new Vector2(selIndex % 8 * _squareSize, _graphics.PreferredBackBufferHeight - _squareSize - selIndex / 8 * _squareSize);
            selOppPos = new Vector2(_graphics.PreferredBackBufferWidth - _squareSize - selIndex % 8 * _squareSize, selIndex / 8 * _squareSize);
        }
        
        _spriteBatch.Draw(hovered_square, position, null,
            Color.White, 0, new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
        _spriteBatch.Draw((index / 8 + index % 8) % 2 == 1 ? light_square : dark_square,
            oppPos, null, Color.White,
            0, new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
        
        _spriteBatch.Draw(hovered_square, selPos, null,
            Color.Gray, 0, new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
        _spriteBatch.Draw((selIndex / 8 + selIndex % 8) % 2 == 1 ? light_square : dark_square,
            selOppPos, null, Color.White,
            0, new Vector2(0, 0), _scale, SpriteEffects.None, 0f);

        for (int i = 0; i < 64; i++) {
            if (i != index && i != selIndex) {
                if ((i / 8 + i % 8) % 2 == 1) {
                    _spriteBatch.Draw(light_square, new Vector2(_graphics.PreferredBackBufferWidth - _squareSize - i % 8 * _squareSize, i / 8 * _squareSize), null,
                        Color.White, 0, new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                    _spriteBatch.Draw(light_square,
                        new Vector2(i % 8 * _squareSize,
                            _graphics.PreferredBackBufferHeight - _squareSize - i / 8 * _squareSize), null, Color.White,
                        0,
                        new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                } else {
                    _spriteBatch.Draw(dark_square, new Vector2(_graphics.PreferredBackBufferWidth - _squareSize - i % 8 * _squareSize, i / 8 * _squareSize), null,
                        Color.White,
                        0, new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                    _spriteBatch.Draw(dark_square,
                        new Vector2(i % 8 * _squareSize,
                            _graphics.PreferredBackBufferHeight - _squareSize - i / 8 * _squareSize), null, Color.White,
                        0,
                        new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                }
            }


            Texture2D piece = null;
            switch (_game.Game[i]) {
                case ChessPiece.b:
                    piece = b;
                    break;
                case ChessPiece.B:
                    piece = B;
                    break;
                case ChessPiece.k:
                    piece = k;
                    break;
                case ChessPiece.K:
                    piece = K;
                    break;
                case ChessPiece.n:
                    piece = n;
                    break;
                case ChessPiece.N:
                    piece = N;
                    break;
                case ChessPiece.q:
                    piece = q;
                    break;
                case ChessPiece.Q:
                    piece = Q;
                    break;
                case ChessPiece.r:
                    piece = r;
                    break;
                case ChessPiece.R:
                    piece = R;
                    break;
                case ChessPiece.p:
                    piece = p0;
                    break;
                case ChessPiece.P:
                    piece = P0;
                    break;
            }

            if (piece != null) {
                _spriteBatch.Draw(piece, new Vector2(_graphics.PreferredBackBufferWidth - _squareSize - i % 8 * _squareSize, i / 8 * _squareSize), null, Color.White, 0,
                    new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                _spriteBatch.Draw(piece,
                    new Vector2(i % 8 * _squareSize,
                        _graphics.PreferredBackBufferHeight - _squareSize - i / 8 * _squareSize), null, Color.White,
                    0, new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
            }
        }

        _spriteBatch.Draw(info, new Vector2(0, 8 * _squareSize), null, Color.White, 0, new Vector2(0, 0), _scale,
            SpriteEffects.None, 0f);
        switch (_result) {
            case 0:
                _spriteBatch.Draw(glhf, new Vector2(_squareSize * 330f / 135, 8 * _squareSize), null, Color.White, 0,
                    new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                break;
            case 1:
                _spriteBatch.Draw(black_wins, new Vector2(_squareSize * 330f / 135, 8 * _squareSize), null, Color.White,
                    0, new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                break;
            case 2:
                _spriteBatch.Draw(white_wins, new Vector2(_squareSize * 330f / 135, 8 * _squareSize), null, Color.White,
                    0, new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                break;
            case 3:
                _spriteBatch.Draw(draw, new Vector2(_squareSize * 330f / 135, 8 * _squareSize), null, Color.White, 0,
                    new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                break;
        }

        foreach (int i in _legalMovesCache) {
            switch (_game.CurrentPlayer) {
                case ChessColor.Black:
                    _spriteBatch.Draw(_legalMoveIndicator, new Vector2(_graphics.PreferredBackBufferWidth - _squareSize - i % 8 * _squareSize, i / 8 * _squareSize), null, Color.White, 0,
                        new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                    break;
                case ChessColor.White:
                    _spriteBatch.Draw(_legalMoveIndicator,
                    new Vector2(i % 8 * _squareSize,
                        _graphics.PreferredBackBufferHeight - _squareSize - i / 8 * _squareSize), null, Color.White,
                    0, new Vector2(0, 0), _scale, SpriteEffects.None, 0f);
                    break;
            }
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}