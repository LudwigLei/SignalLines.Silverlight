﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SignalLines.Common;
using SignalLines.Common.GamePieces;
using Line = SignalLines.Common.GamePieces.Line;
using SquareCompletedEventArgs = SignalLines.Common.GamePieces.SquareCompletedEventArgs;

namespace SignalR.Silverlight
{
    public partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();

            _connectionManager = new ConnectionManager(Dispatcher);
            _connectionManager.MessageReceived += ConnectionManagerOnMessageReceived;
            _connectionManager.LineClicked += ConnectionManagerOnLineClicked;
            _connectionManager.PlayerJoined += ConnectionManagerOnPlayerJoined;
            _connectionManager.PlayerLeft += ConnectionManagerOnPlayerLeft;
            _connectionManager.GameReset += ConnectionManagerOnGameReset;
            _connectionManager.AddedToGame += ConnectionManagerOnAddedToGame;
        }

        private void ConnectionManagerOnAddedToGame(object sender, AddedToGameEventArgs e)
        {
            if (e.GameState != null)
            {
                CreateWorld(e.GameState);
                GameView.Visibility = Visibility.Visible;
                LoginView.Visibility = Visibility.Collapsed;
            }
        }

        private readonly ConnectionManager _connectionManager;
        private GameModel _model;
        private Grid _gameGrid;
        private IList<Button> _lineButtons;
        private IList<Grid> _squareGrids;
        private ObservableCollection<Player> _players;

        private void ConnectionManagerOnPlayerLeft(object sender, PlayerLeftEventArgs playerLeftEventArgs)
        {
            var player = _players.FirstOrDefault(p => playerLeftEventArgs.Player != null && p.Name == playerLeftEventArgs.Player.Name);
            if (player != null)
            {
                _players.Remove(player);
                RaisePropertyChanged("_players");
            }
        }

        private void ConnectionManagerOnGameReset(object sender, GameResetEventArgs gameResetEventArgs)
        {
            CreateWorld(gameResetEventArgs.GameState);
        }

        private void ConnectionManagerOnPlayerJoined(object sender, PlayerJoinedEventArgs eventArgs)
        {
            if (_players.Count(p => p.PlayerId == eventArgs.Player.PlayerId) == 0)
                _players.Add(eventArgs.Player);
        }

        private void ConnectionManagerOnLineClicked(object sender, LineClickedEventArgs e)
        {
            var piece = _model.GetElementAt(e.Row, e.Column) as Line;
            if (piece != null)
            {
                piece.Occupy(e.PlayerId);

                var player = GetPlayer(e.PlayerId);

                player.Score++;

                var button = FindButton(e.Row, e.Column);
                SetPieceColor(button);

            }
        }

        private Player GetPlayer(int playerId)
        {
            return _players.FirstOrDefault(p => p.PlayerId == playerId);
        }

        private void SetPieceColor(Button button)
        {
            var line = button.Tag as Line;
            if (line != null)
            {
                switch (line.PlayerId)
                {
                    case 0:
                        button.Background = new SolidColorBrush(Colors.Transparent);
                        break;
                    case 1:
                        button.Background = new SolidColorBrush(Colors.Red);
                        break;
                    case 2:
                        button.Background = new SolidColorBrush(Colors.Yellow);
                        break;
                    case 3:
                        button.Background = new SolidColorBrush(Colors.Blue);
                        break;
                    case 4:
                        button.Background = new SolidColorBrush(Colors.Orange);
                        break;
                    case 5:
                        button.Background = new SolidColorBrush(Colors.Green);
                        break;
                    case 6:
                        button.Background = new SolidColorBrush(Colors.Purple);
                        break;
                    case 7:
                        button.Background = new SolidColorBrush(Colors.Magenta);
                        break;
                    default:
                        button.Background = new SolidColorBrush(Colors.Black);
                        break;
                }
            }
        }

        private Button FindButton(int row, int column)
        {
            var firstOrDefault = _lineButtons
                .Select(x => new { Button = x, Line = x.Tag as Line })
                .FirstOrDefault(x => x.Line.Row == row && x.Line.Column == column);

            if (firstOrDefault != null)
                return firstOrDefault
                    .Button;
            return null;
        }

        private void ConnectionManagerOnMessageReceived(object sender,  MessageReceivedEventArgs messageReceivedEventArgs)
        {
            if (!string.IsNullOrEmpty(ChatMessages.Text))
                ChatMessages.Text += "\n";
            ChatMessages.Text += messageReceivedEventArgs.Message;
        }

        private void CreateWorld(GameState state)
        {
            _model = new GameModel(state.Size.Item1, state.Size.Item2);
            _players = new ObservableCollection<Player>(state.Players);
            PlayerList.ItemsSource = _players;

            _gameGrid = new Grid();
            _lineButtons = new List<Button>();
            _squareGrids = new List<Grid>();

            SetRowDefinitions();
            SetColumnsDefinitions();

            var lst = _model.GetAllElements().ToList();

            foreach (var piece in lst)
            {
                var element = GetElementForPiece(piece);
                element.SetValue(Grid.RowProperty, piece.Row);
                element.SetValue(Grid.ColumnProperty, piece.Column);
                element.Tag = piece;
                _gameGrid.Children.Add(element);

                if (element is Button)
                {
                    _lineButtons.Add(element as Button);
                }

                if (piece is Square)
                {
                    var square = piece as Square;
                    square.Completed += SquareCompleted;

                    _squareGrids.Add(element as Grid);
                }
            }

            Board.Children.Clear();
            Board.Children.Add(_gameGrid);

            foreach (var button in _lineButtons)
                SetPieceColor(button);

            foreach (var occupied in state.OccupiedLines)
            {
                var line = _model.GetElementAt(occupied.Row, occupied.Column) as Line;
                if (line != null) line.Occupy(occupied.PlayerId);
                var button = FindButton(occupied.Row, occupied.Column);
                SetPieceColor(button);
                var player = GetPlayer(occupied.PlayerId);
                if (player != null) player.Score++;
            }
        }

        private void SquareCompleted(object sender, SquareCompletedEventArgs eventArgs)
        {
            var square = sender as Square;
            if (square != null)
            {
                var squareGrid = _squareGrids
                    .Select(x => new { Grid = x, Square = x.Tag as Square })
                    .FirstOrDefault(x => x.Square.Row == square.Row && x.Square.Column == square.Column);
                if (squareGrid != null)
                {
                    var player = GetPlayer(eventArgs.PlayerId);

                    if (player != null) player.Score = player.Score + 5;

                    switch (eventArgs.PlayerId)
                    {
                        case 0:
                         squareGrid.Grid.Background  = new SolidColorBrush(Colors.Transparent);
                        break;
                    case 1:
                         squareGrid.Grid.Background  = new SolidColorBrush(Colors.Red);
                        break;
                    case 2:
                        squareGrid.Grid.Background = new SolidColorBrush(Colors.Yellow);
                        break;
                    case 3:
                         squareGrid.Grid.Background = new SolidColorBrush(Colors.Blue);
                        break;
                    case 4:
                        squareGrid.Grid.Background  = new SolidColorBrush(Colors.Orange);
                        break;
                    case 5:
                         squareGrid.Grid.Background = new SolidColorBrush(Colors.Green);
                        break;
                    case 6:
                         squareGrid.Grid.Background = new SolidColorBrush(Colors.Purple);
                        break;
                    case 7:
                        squareGrid.Grid.Background  = new SolidColorBrush(Colors.Magenta);
                        break;
                    default:
                         squareGrid.Grid.Background  = new SolidColorBrush(Colors.Black);
                        break;
                    }
                }
            }  
        }

        private FrameworkElement GetElementForPiece(GamePiece piece)
        {
            if (piece is Dot)
            {
                var item = new Grid { Style = (Style)Application.Current.Resources["DotStyle"] };
                return item;
            }

            if (piece is Line)
            {
                var button = new Button
                                 {
                                     Style = (Style)Application.Current.Resources["LineStyle"],
                                     DataContext = piece
                                 };
                // Bind tag's PlayerId

                button.Click += LineClicked;
                return button;
            }

            var square = new Grid { Style = (Style)Application.Current.Resources["SquareStyle"] };
            return square;
        }

        private void LineClicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                var piece = button.Tag as Line;
                if (piece != null) _connectionManager.ClickLine(piece.Row, piece.Column);
            }
        }

        private void SetColumnsDefinitions()
        {
            for (var i = 0; i < _model.NumColumns; i++)
            {
                if (i.IsEven())
                {
                    _gameGrid.ColumnDefinitions.Add(new ColumnDefinition
                                                        {
                                                            Width = new GridLength(1, GridUnitType.Star)
                                                        });
                }
                else
                {
                    _gameGrid.ColumnDefinitions.Add(new ColumnDefinition
                                                        {
                                                            Width = new GridLength(8, GridUnitType.Star)
                                                        });
                }
            }
        }

        private void SetRowDefinitions()
        {
            for (var i = 0; i < _model.NumRows; i++)
            {
                if (i.IsEven())
                {
                    _gameGrid.RowDefinitions.Add(new RowDefinition
                                                    {
                                                        Height = new GridLength(1, GridUnitType.Star)
                                                    });
                }
                else
                {
                    _gameGrid.RowDefinitions.Add(new RowDefinition
                                                    {
                                                        Height = new GridLength(8, GridUnitType.Star)
                                                    });
                }
            }
        }

        private void SendButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(NameText.Text))
            {
                _connectionManager.SendMessage(MessageText.Text);
                MessageText.Text = string.Empty;
            }
        }

        private void MessageTextKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!string.IsNullOrEmpty(MessageText.Text))
                {
                    _connectionManager.SendMessage(MessageText.Text);
                    MessageText.Text = string.Empty;
                }
            }
        }

        private void NewGameClicked(object sender, RoutedEventArgs e)
        {
            _connectionManager.ResetGame();
        }

        private void JoinGameClicked(object sender, RoutedEventArgs e)
        {
            JoinGame();
        }

        private void JoinGame()
        {
            if (NameText.Text != null)
            {
                _connectionManager.JoinGame(NameText.Text);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private void NameTextKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                JoinGame();
            }
        }
    }
}
