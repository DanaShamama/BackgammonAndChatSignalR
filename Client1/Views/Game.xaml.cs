using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Client1.Utils;
using Client1.ViewModels;
using Common;
using Microsoft.AspNet.SignalR.Client;

namespace Client1.Views
{
    /// <summary>
    /// Interaction logic for Game.xaml
    /// </summary>
    public partial class Game : Page
    {
        public BoardState CurrentBoardState { get; set; } // the current board state which includes: 1. the array of the board (how many discs in each index, and if they are Red's discs, or black's discs), 2. a string message, 3. bool RedTurn, 4. int Cubes[] 5. int RedMoves, 6. int BlackMoves, 7. bool IsRedWin, 8. int RedEatten 9. int BlackEatten 10. int RedDiscsOnBoard (how many discs the player has on the board, not including the ones that are eatten or the ones he got out of the board) 11. int BlackDiscsOnBoard 12. bool RedAllInHome  ("Home" is the last 6 indexes on the player track, if all his discs are "InHome" he can get discs out of the board) 13. bool BlackAllInHome 14. int OutRed  (how many discs he got out of the game. when all discs are out, he wins) 15. int OutBlack  16. bool CurrentUserCanMove (there is a validation that determine whether the user can move or not in the current move, if it's legal or not)
        private int _allRows; // this is the number of rows on the grid, used for drowing the grid rows and placing discs (ellipses) in the grid cells. this number can be changed on demand, as a factor of how many discs there are in each _array index. the index with most discs in it, will impose the number of rows (_allRows = _mostDiscsInCell * 2 + 1).
        private int[] _cubesShow; // array with 2 ints, only for drowing the cubes according to _cubes[] that were rolled this turn. 
        private int _size; // the size of each disc on the board (height and width). can be changed on demand according to the highst number of discs in one index of CurrentBoardState.BoardArray[].
        private int _highstNumberOfDiscsInTriangle; //  represent the highest number of discs in one index of CurrentBoardState.BoardArray[], will be calculated in order of drowing the discs and the board and determine how many rows are in the grid.
        private bool _firstTimeDrow; // it is true if the board is drawn for the first time in the game. at the rest of the times, first there will be an execute of CleanBoardFromDiscs(), and only then DrawBoard. at the first time there is nothing to clean.
        private int _newRows; // used to determine how many rows there are in addition to the regular 11 of the first draw, the draw method will add new rows as the number of _newRows. 
        public Move CurrentMove { get; set; } // the current move that the user wants to do, only for one cube at a time. include: 1. int From (the valid origin index of the disc on the board array), 2. int ChosenCube (the cube he clicked on to make this move), 3. BoardState CurrentBoardState (the current boardState with all its parameters, before he makes the move)
        private static readonly object padlock = new object(); // an object used by locks to prevent collision of threads

        const int Moves = 2;
        const int DoubleMoves = 4;
        const int HalfTheBoardArrayLength = 12;
        const int QuarterTheBoardArrayLength = 6;
        const int HalfColumnsOnTheGrid = 6;
        const int InitialHighestNumberOfDiscsInTriangle = 5;

        private string _userName;
        public string UserName // this player user name on the system
        {
            get { return _userName; }
            set { _userName = value; }
        }

        private string _otherUser;
        public string OtherUser // the name of the user the this user play with
        {
            get { return _otherUser; }
            set { _otherUser = value; }
        }

        private string _startGameUser;

        public string StartGameUser // the name user that was the first to request the game 
        {
            get { return _startGameUser; }
            set { _startGameUser = value; }
        }

        private bool _isThisUserRed; // boolean to determine if this user is the Red (and start the game) or the black.
        public bool IsThisUserRed
        {
            get { return _isThisUserRed; }
            set { _isThisUserRed = value; }
        }

        private bool _thisPlayerTurn; // boolean to determine if this user is the one that plays now or not.
        public bool ThisPlayerTurn
        {
            get
            {
                if (CurrentBoardState.RedTurn && !IsThisUserRed)
                {
                    return false;
                }
                else if (!CurrentBoardState.RedTurn && IsThisUserRed)
                {
                    return false;
                }
                else return true;
            }
            set { _thisPlayerTurn = value; }
        }

        private static IHubProxy gameHubProxy;
        public IHubProxy GameHub
        {
            get
            {
                lock (padlock)
                {
                    return gameHubProxy;
                }
            }
        }

        // ctor
        public Game(string userName, string otherUser, IHubProxy chatHubProxy, string startGameUser)
        {
            InitializeComponent();

            CurrentBoardState = new BoardState();
            UserName = userName;
            OtherUser = otherUser;
            StartGameUser = startGameUser;
            IsThisUserRed = IsThisUserStartsTheGame();
            _size = 45;
            _highstNumberOfDiscsInTriangle = HighstNumberOfDiscsInTriangle(CurrentBoardState.BoardArray);
            _allRows = AllRows();
            _cubesShow = new int[2];
            _firstTimeDrow = true;
            cube1Button.SetValue(Grid.RowProperty, (_allRows - 1) / 2);
            cube2Button.SetValue(Grid.RowProperty, (_allRows - 1) / 2 - 1);
            _newRows = 0;
            ThisPlayerTurn = true;
            CurrentMove = new Move();
            gameHubProxy = chatHubProxy;

            GameHub.On("broadcastBoard", (BoardState boardState) => // update the current board state and draw it
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CurrentBoardState = boardState;
                    DrawBoard();
                }));
            });

            GameHub.On("Message", (string msg) => // broadcast a meesage
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Message(msg);
                }));
            });

            GameHub.On("Win", (BoardState bs) => // update the current board, draw it, and put a winning message
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CurrentBoardState = bs;
                    string msg;
                    if (bs.IsRedWin)
                    {
                        msg = "Red Win!!!";
                    }
                    else
                    {
                        msg = "Black Win!!!";
                    }
                    DrawBoard();
                    Message(msg);
                }));
            });

            if (userName.Equals(startGameUser)) // send a new board at the first time to both users, from the server 
            {
                gameHubProxy.Invoke("SendBoard", userName, otherUser);
            }
        }

        public void DrawBoard() // draw the current board
        {
            if (CurrentBoardState.Message != "") // if there is a message to show in the current board state, it will be showen
            {
                Message(CurrentBoardState.Message);
            }

            if (!CurrentBoardState.CurrentUserCanMove) // if the user can't move there will be a message
            {
                Message(CurrentBoardState.Message);
            }

            if (!_firstTimeDrow) // before drawing, there will be a clean to erase everything from the board, but not at the first draw, there is nothing to erase
            {
                CleanBoardFromDiscs();
            }

            _highstNumberOfDiscsInTriangle = HighstNumberOfDiscsInTriangle(CurrentBoardState.BoardArray); // the value in int of the highest number of discs at any index on the board
            if (_highstNumberOfDiscsInTriangle > InitialHighestNumberOfDiscsInTriangle) // The basic number of rows, at the beginning of the game, is 5 (5 for each side, up and down,  -> 5 * 2 + 1 -> 11 including the one in the middle), after the moves begun, _mostDiscsInCell can be more than 5, which means there are more rows to add to the grid and draw them
            {
                _allRows = AllRows();  // calculates how many rows in total need to be on the board -> on both sides up and down including the additional one in the middle (_mostDiscsInCell * 2 + 1;)
                _size = 500 / _allRows; // 500 is a factor used to determine the relative appropriate size for one disc (height and width)
                _newRows = _allRows - 11; // 11 is the basic number of all rows (5 for each side, up and down,  -> 5 * 2 + 1 -> 11 including the one in the middle)

                for (int i = 0; i < _newRows; i++) // add rows to the grid in addition to the basic 11 for each turn.
                {
                    RowDefinition gridRow = new RowDefinition();
                    Board.RowDefinitions.Add(gridRow);
                }
            }

            DrawElipses();
            ShowDice(); // draw the dice - the cubes that are rolled this turn
            DrawEttean(); // draw all the eatten discs, and a counter that counts how many eatten discs for Red and black
            OutCounter();

            if (CurrentBoardState.RedMoves == DoubleMoves || CurrentBoardState.RedMoves == Moves && !CurrentBoardState.Cubes.IsDouble) // if the turn has just began, this message will appear, for Red or black
            {
                Message("now it's Red's turn");
            }
            else if (CurrentBoardState.BlackMoves == DoubleMoves || CurrentBoardState.BlackMoves == Moves && !CurrentBoardState.Cubes.IsDouble)
            {
                Message("now it's black's turn");
            }

            ShowCubesButtons();

            _firstTimeDrow = false; // after the first time of drawing the board, every draw is preceded by "cleaning" the previous board from the discs and previous added rows (except the basic 11 that are allways there)

            // at the end of the draw, the message is reset 
            CurrentBoardState.Message = "";
        }

        private void DrawElipses()
        {
            for (int i = 0; i < CurrentBoardState.BoardArray.Length; i++) // in each index of the board array
            {
                int numOfDiscs = Math.Abs(CurrentBoardState.BoardArray[i]); // how many discs in the current index 
                int row; // will be the row on the grid to draw an ellipse in
                int column = 0; // will be the column on the grid to draw an ellipse in

                if (i < HalfTheBoardArrayLength) // HalfTheBoardArrayLength is 12, the matched row on the grid is calculated by checking if i < 12, or i >= 12. i is the index on the board, for the first 0-11 indexes the row on the grid will start from 0 (row 0 is the most upper row on the grid)
                {
                    for (int j = 0; j < numOfDiscs; j++) // for each index, all the discs should be drawn in the loop
                    {
                        row = j; // start from top, and goes down with every additional disc
                        if (i < QuarterTheBoardArrayLength) // top right side -> the matched column is calculated by checking if i < 6 (right side) or i >= 6 (left side), there is a grid column in the middle, which is not representing of the array indexes
                        {
                            column = HalfTheBoardArrayLength - i; //  right side, takes into consideration the grid column in the middle, which is not representing of the array indexes
                        }
                        else if (i >= QuarterTheBoardArrayLength) // top left side
                        {
                            column = HalfTheBoardArrayLength - 1 - i; // without the grid column in the middle
                        }
                        Ellipse ellipse = new Ellipse();
                        ellipse.Height = _size;
                        ellipse.Width = _size;
                        if (CurrentBoardState.BoardArray[i] < 0) // negative numbers represent the "black" discs
                        {
                            ellipse.Fill = new SolidColorBrush(Colors.Black);
                        }
                        else if (CurrentBoardState.BoardArray[i] > 0)
                        {
                            ellipse.Fill = new SolidColorBrush(Colors.Red); // negative numbers represent the "Red" discs
                        }
                        Grid.SetColumn(ellipse, column);
                        Grid.SetRow(ellipse, row);
                        Board.Children.Add(ellipse);
                    }
                }
                else //  if(i >= 12) the matched row on the grid is calculated by checking if i < 12, or i >= 12. i is the index on the board, for the first 0-11 indexes the row on the grid will start from _allRows - 1 (row _allRows - j - 1 is the most lower row on the grid)
                {
                    for (int j = 0; j < numOfDiscs; j++)
                    {
                        row = _allRows - j - 1; // start from buttom, buttom = _allRows - j - 1, and goes up with every additional disc

                        if (i < 18) // buttom left side -> the matched column is calculated by checking if i < 18 (left side) or i >= 18 (right side), there is a grid column in the middle, which is not representing of the array indexes
                        {
                            column = i - HalfTheBoardArrayLength; // left side without the grid column in the middle. start from index 12 on the board CurrentBoardState.BoardArray[12], which is the most left column (column = 0), and goes right with every index
                        }
                        else if (i >= 18) // buttom right side
                        {
                            column = i - HalfTheBoardArrayLength + 1; // takes into consideration the grid column in the middle, which is not representing of the array indexes, start from index 18 on the board CurrentBoardState.BoardArray[18], which is the most left column (column = 7), and goes right with every index
                        }

                        Ellipse ellipse = new Ellipse();
                        ellipse.Height = _size;
                        ellipse.Width = _size;
                        if (CurrentBoardState.BoardArray[i] < 0) // for black negative values
                        {
                            ellipse.Fill = new SolidColorBrush(Colors.Black);
                        }
                        else if (CurrentBoardState.BoardArray[i] > 0) // for Red positive values
                        {
                            ellipse.Fill = new SolidColorBrush(Colors.Red);
                        }
                        Grid.SetColumn(ellipse, column);
                        Grid.SetRow(ellipse, row);
                        Board.Children.Add(ellipse);
                    }
                }
            }
        }

        

        /// <summary>
        ///     after the first time of drawing the board, 
        ///     every draw is preceded by "cleaning" the previous board from the discs and previous added rows, 
        ///     (except the basic 11 that are allways there)
        /// </summary>
        private void CleanBoardFromDiscs()
        {
            for (int i = Board.Children.Count - 1; i >= 0; i--)
            {
                Ellipse ellipse = new Ellipse();
                Image image = new Image();
                if (Board.Children[i].GetType() == ellipse.GetType() || Board.Children[i].GetType() == image.GetType()) // removes ellipses and cube images
                {
                    Board.Children.Remove(Board.Children[i]);
                }
            }

            for (int i = 0; i < _newRows; i++)
            {
                Board.RowDefinitions.RemoveAt(i); // remove all the new rows that were added (if there are more than 5 discs in one index), leaving the 11 basic ones
            }
            _newRows = 0; // reset the _newRows value for next time
        }

        /// <summary>
        ///  this fuction draw the cubes that was rolled and were sent by the server to the client,
        ///  each turn according to the CurrentBoardState
        ///  this function also responsible for making the cubesButtons visibility collapsed every new turn
        /// </summary>
        private void ShowDice()
        {

            BitmapImage[] cubesImages = new BitmapImage[2];

            cubesImages[0] = new BitmapImage();
            cubesImages[1] = new BitmapImage();
            int row = (_allRows - 1) / 2; // the middle of all rows
            int column = 6; // the middle of the columns

            _cubesShow[0] = CurrentBoardState.Cubes.CubesNumbers[0]; // _cubesShow have the Image, CurrentBoardState.Cubes.CubesNumbers[i] have the number of the cube, that was rolled this turn
            _cubesShow[1] = CurrentBoardState.Cubes.CubesNumbers[1];

            string path = Directory.GetCurrentDirectory() + @".\..\..\Views\Assets\dice";

            for (int i = 0; i < 2; i++)
            {
                int num = _cubesShow[i];
                switch (num)
                {
                    case 1:

                        cubesImages[i].BeginInit();
                        cubesImages[i].UriSource = new Uri(path + "1.png");
                        cubesImages[i].EndInit();
                        break;
                    case 2:
                        cubesImages[i].BeginInit();
                        cubesImages[i].UriSource = new Uri(path + "2.png");
                        cubesImages[i].EndInit();
                        break;
                    case 3:
                        cubesImages[i].BeginInit();
                        cubesImages[i].UriSource = new Uri(path + "3.png");
                        cubesImages[i].EndInit();
                        break;
                    case 4:
                        cubesImages[i].BeginInit();
                        cubesImages[i].UriSource = new Uri(path + "4.png");
                        cubesImages[i].EndInit();
                        break;
                    case 5:
                        cubesImages[i].BeginInit();
                        cubesImages[i].UriSource = new Uri(path + "5.png");
                        cubesImages[i].EndInit();
                        break;
                    case 6:
                        cubesImages[i].BeginInit();
                        cubesImages[i].UriSource = new Uri(path + "6.png");
                        cubesImages[i].EndInit();
                        break;
                }
            }

            if (_cubesShow[0] != 0)
            {
                Image dynamicImage = new Image();
                dynamicImage.Source = cubesImages[0];
                dynamicImage.SetValue(Grid.ColumnProperty, column);
                dynamicImage.SetValue(Grid.RowProperty, row);
                Board.Children.Add(dynamicImage);
                Panel.SetZIndex(dynamicImage, -1); // the cubeButton has higher zIndex than the cube Image, so the Image won't cover the button
                cube1Button.SetValue(Grid.RowProperty, (row));
            }

            if (_cubesShow[1] != 0)
            {
                Image dynamicImage2 = new Image();
                dynamicImage2.Source = cubesImages[1];
                dynamicImage2.SetValue(Grid.ColumnProperty, column);
                dynamicImage2.SetValue(Grid.RowProperty, row - 1);
                Board.Children.Add(dynamicImage2);
                Panel.SetZIndex(dynamicImage2, -1);

                cube2Button.SetValue(Grid.RowProperty, (row - 1));
            }

            cube1Button.Visibility = Visibility.Collapsed;
            cube2Button.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        ///      draws all the eatten discs, and a counter that counts how many eatten discs for Red and black
        /// </summary>
        private void DrawEttean()
        {
            if (CurrentBoardState.RedEatten > 0)
            {
                int row = ((_allRows - 1) / 2) + 2; // calculates the place for the ellipse, near the middle of the board
                Ellipse ellipse = new Ellipse();
                ellipse.Height = _size;
                ellipse.Width = _size;

                ellipse.Fill = new SolidColorBrush(Colors.Red);

                Grid.SetColumn(ellipse, HalfColumnsOnTheGrid);
                Grid.SetRow(ellipse, row);
                Board.Children.Add(ellipse);
                RedEttanCounter.Visibility = Visibility.Visible;
                RedEttanCounter.Text = "Red Eaten: " + CurrentBoardState.RedEatten.ToString();
                Grid.SetRow(RedEttanCounter, row + 1);

            }
            else if (CurrentBoardState.RedEatten == 0)
            {
                RedEttanCounter.Visibility = Visibility.Collapsed;
            }

            if (CurrentBoardState.BlackEatten > 0)
            {
                int row = ((_allRows - 1) / 2 + 1) - 3; // calculates the place for the ellipse, near the middle of the board
                Ellipse ellipse = new Ellipse();
                ellipse.Height = _size;
                ellipse.Width = _size;

                ellipse.Fill = new SolidColorBrush(Colors.Black);

                Grid.SetColumn(ellipse, HalfColumnsOnTheGrid);
                Grid.SetRow(ellipse, row);
                Board.Children.Add(ellipse);
                BlackEttanCounter.Visibility = Visibility.Visible;
                BlackEttanCounter.Text = "Black Eaten: " + CurrentBoardState.BlackEatten.ToString();
                Grid.SetRow(BlackEttanCounter, row - 1);

            }
            else if (CurrentBoardState.BlackEatten == 0)
            {
                BlackEttanCounter.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowCubesButtons()
        {
            // only for this player's turn, the cube button is visible, and only for the cubes he hasn't used yet (CubesNumbers[i] != 0), 
            // and only if the player has eatten discs, 
            // otherwise the cube buttons are visible and available only after clicking a disc. 
            // as a default, the cubeButtons visibility is collapsed, 
            // unless there are eatten discs for this player's turn, or he clicked a disc in order to move it
            if (CurrentBoardState.RedTurn && CurrentBoardState.RedEatten > 0 && ThisPlayerTurn)
            {
                if (CurrentBoardState.Cubes.CubesNumbers[0] != 0)
                {
                    cube1Button.Visibility = Visibility.Visible;
                }
                if (CurrentBoardState.Cubes.CubesNumbers[1] != 0)
                {
                    cube2Button.Visibility = Visibility.Visible;
                }
            }

            if (!CurrentBoardState.RedTurn && CurrentBoardState.BlackEatten > 0 && ThisPlayerTurn)
            {
                if (CurrentBoardState.Cubes.CubesNumbers[0] != 0)
                {
                    cube1Button.Visibility = Visibility.Visible;
                }
                if (CurrentBoardState.Cubes.CubesNumbers[1] != 0)
                {
                    cube2Button.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        ///     a converter from a grid cell to the mathing index of the board. 
        ///     this function is used in the event of clicking a disc in order to move it
        /// </summary>
        /// <param name="row"> int, grid row </param>
        /// <param name="column">int, grid column </param>
        /// <returns>int, index on the board</returns>
        private int ConvertFromGridCellToBoardArrayIndex(int row, int column)
        {
            int indexOfBoard = -1;

            if (row < (_allRows - 1) / 2) // for the top of the grid
            {
                if (column < HalfColumnsOnTheGrid) // for the left side of the grid
                {
                    indexOfBoard = HalfTheBoardArrayLength - 1 - column; // without the grid column in the middle, which is not representing of the array indexes
                }
                else if (column > HalfColumnsOnTheGrid) // for the right side of the grid
                {
                    indexOfBoard = HalfTheBoardArrayLength - column; // takes into consideration the grid column in the middle, which is not representing of the array indexes
                }
            }
            else if (row > (_allRows - 1) / 2) // for the buttom of the grid
            {
                if (column < HalfColumnsOnTheGrid) // for the left side of the grid
                {
                    indexOfBoard = HalfTheBoardArrayLength + column; // without the grid column in the middle, which is not representing of the array indexes
                }
                else if (column > HalfColumnsOnTheGrid) // for the right side of the grid
                {
                    indexOfBoard = HalfTheBoardArrayLength - 1 + column; // takes into consideration the grid column in the middle, which is not representing of the array indexes
                }
            }

            return indexOfBoard;
        }

        private void OutCounter()
        {
            if (CurrentBoardState.OutRed > 0)
            {
                int row = _allRows - 1; // calculates the place for the out counter, near the middle of the board
                OutRedCounter.Visibility = Visibility.Visible;
                OutRedCounter.Text = "Red Out: " + CurrentBoardState.OutRed.ToString();
                Grid.SetRow(OutRedCounter, row);
            }
            if (CurrentBoardState.OutBlack > 0)
            {
                int row = 0; // calculates the place for the out counter, near the middle of the board
                OutBlackCounter.Visibility = Visibility.Visible;
                OutBlackCounter.Text = "Black Out: " + CurrentBoardState.OutBlack.ToString();
                Grid.SetRow(OutBlackCounter, row);
            }
        }

        /// <summary>
        ///     calculates how many rows there are on the board/need to be on the grid. 
        ///     _mostDiscsInCell is the highest number of discs of all board indexes, 
        ///     for symmetry the number of all rows will be _mostDiscsInCell * 2 + 1 (the "+1" is the middle row )
        /// </summary>
        /// <returns> int, how many rows on the board</returns>
        private int AllRows()
        {
            if (_highstNumberOfDiscsInTriangle > InitialHighestNumberOfDiscsInTriangle)
            {
                return _highstNumberOfDiscsInTriangle * 2 + 1;
            }
            else
            {
                return 2 * InitialHighestNumberOfDiscsInTriangle + 1; // return 11, the basic number of row if there are only up to 5 discs in each index of the board
            }
        }

        /// <summary>
        /// This function calculates the highst number (an absolute value) of all cells in a given array. 
        /// for example: { 2, 0, 0, 0, 0, -5, 0, -3, 0, 0, 0} will return 5.
        /// </summary>
        /// <param name="array">int[], array of the board with int numbers which represent the discs on board</param>
        /// <returns> int, the highest absolute number of all numbers of the board indexes</returns>
        private int HighstNumberOfDiscsInTriangle(int[] array)
        {
            int biggestNum = Math.Abs(array[0]);

            for (int i = 1; i < array.Length; i++)
            {
                if (Math.Abs(array[i]) > biggestNum)
                {
                    biggestNum = Math.Abs(array[i]);
                }
            }

            return biggestNum;
        }

        /// <summary>
        /// // calculates the index of the board, where the disc is clicked. 
        /// each triangle of the image, with its discs, belongs to an index on the board
        /// after clicking the disc, the player will choose a cube and send it to the server for a validation check
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Board_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UIElement element = (UIElement)e.Source;

            int c = Grid.GetColumn(element);
            int r = Grid.GetRow(element);
            int from = -1; // reset the value to a not legal index
            int indexOfBoard = ConvertFromGridCellToBoardArrayIndex(r, c);

            if (indexOfBoard >= 0 && indexOfBoard <= 23) // the range of the board indexes
            {
                from = indexOfBoard;
            }

            CurrentMove.From = from;

            if (ThisPlayerTurn && from != -1 && !(CurrentBoardState.RedTurn && CurrentBoardState.RedEatten > 0) && !(!CurrentBoardState.RedTurn && CurrentBoardState.BlackEatten > 0)) // if there are eattens, player can't move until he puts the eatten back on board, and can't choose a disc from the discs on the board to move it
            {
                if (CurrentBoardState.Cubes.CubesNumbers[0] != 0)
                {
                    cube1Button.Visibility = Visibility.Visible;
                }
                if (CurrentBoardState.Cubes.CubesNumbers[1] != 0)
                {
                    cube2Button.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        ///    an event that receives the clicking on the first cube button,
        ///    and assigns the number of the cube [1-6] in the ChosenCube property of the current move,
        ///    (works with the function Board_MouseDown)
        ///    assign the current board state into the current board state property in the current move, 
        ///    and send the current move to the server to check if the user's move is legal or not,
        ///    by invoking GetMoveFromUser in the server.
        ///    
        ///    pay attention! the cube image and the button underneath are visible only if the user haven't use that cube yet, 
        ///    or if the move was'nt legal (if the move was'nt legal - then no move was made, so the cube is still available)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cube1Button_Click(object sender, RoutedEventArgs e)
        {

            CurrentMove.ChosenCube = CurrentBoardState.Cubes.CubesNumbers[0];
            CurrentMove.CurrentBoardState = CurrentBoardState;
            GameHub.Invoke("GetMoveFromUser", CurrentMove, UserName, OtherUser);
        }

        /// <summary>
        ///    an event that receives the clicking on the second cube button,
        ///    and assigns the number of the cube [1-6] in the ChosenCube property of the current move,
        ///    (works with the function Board_MouseDown)
        ///    assign the current board state into the current board state property in the current move, 
        ///    and send the current move to the server to check if the user's move is legal or not,
        ///    by invoking GetMoveFromUser in the server.
        ///    
        ///    pay attention! the cube image and the button underneath are visible only if the user haven't use that cube yet, 
        ///    or if the move was'nt legal (if the move was'nt legal - then no move was made, so the cube is still available)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cube2Button_Click(object sender, RoutedEventArgs e)
        {
            CurrentMove.ChosenCube = CurrentBoardState.Cubes.CubesNumbers[1];
            CurrentMove.CurrentBoardState = CurrentBoardState;
            GameHub.Invoke("GetMoveFromUser", CurrentMove, UserName, OtherUser);
        }

        /// <summary>
        /// startGameUser is a string parameter that is received in this class c'tor as the game starts (the name of the user that offerd to play ),
        /// it has the same value for both users. 
        /// this function checks if this userClient (the UserName) is the one that invoked the starting of a game (his name is equals startGameUser).
        /// If this user is the StartGameUser, then the boolean data member IsThisUserRed assigned true.
        /// otherwise IsThisUserRed is assigned as false.
        /// </summary>
        /// <returns> boolean that determines whether this user is the one that starts the game (the Red discs) or not</returns>
        private bool IsThisUserStartsTheGame()
        {
            if (StartGameUser.Equals(UserName))
            {
                IsThisUserRed = true;
                return true;
            }
            IsThisUserRed = false;
            return false;
        }

        private void Message(string msg)
        {
            MessageBox.Show(msg);
        }
    }
}
