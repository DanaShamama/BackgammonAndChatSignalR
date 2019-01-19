using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common;
using Microsoft.AspNet.SignalR.Hubs;
using System.Threading.Tasks;
using ChatAndGameProject.DAL;


namespace ChatAndGameProject.Hubs
{
    [HubName("GameAndChatManager")]
    public class GameAndChatManager : Hub
    {
        private static readonly object _lockObj1 = new object(); // an object used by locks to prevent collision of threads
        private static readonly object _lockObj2 = new object(); // an object used by locks to prevent collision of threads
        private static readonly object _lockObj3 = new object(); // an object used by locks to prevent collision of threads
        private static readonly object _lockObj4 = new object(); // an object used by locks to prevent collision of threads
        private static readonly object _lockObj5 = new object(); // an object used by locks to prevent collision of threads
        private static readonly object _lockObj6 = new object(); // an object used by locks to prevent collision of threads
        private static readonly object _lockObj7 = new object(); // an object used by locks to prevent collision of threads
        private static readonly object _lockObj8 = new object(); // an object used by locks to prevent collision of threads
        private static readonly object _lockObj9 = new object(); // an object used by locks to prevent collision of threads
        private static readonly object _lockObj10 = new object(); // an object used by locks to prevent collision of threads
        private static readonly object _lockObj11 = new object(); // an object used by locks to prevent collision of threads

        private static Dictionary<string, string> _usersConnections = new Dictionary<string, string>();
        public static Dictionary<string, string> UsersConnections
        {
            get
            {
                lock (_lockObj1)
                {
                    return _usersConnections;
                }
            }
            set
            {
                lock (_lockObj1)
                {
                    _usersConnections = value;
                }
            }
        }

        private static Dictionary<string, string> _usersNames = new Dictionary<string, string>();
        public static Dictionary<string, string> UsersNames
        {
            get
            {
                lock (_lockObj2)
                {
                    return _usersNames;
                }
            }
            set
            {
                lock (_lockObj2)
                {
                    _usersNames = value;
                }
            }
        }

        private static List<UserWithStatus> _usersWithStatus = new List<UserWithStatus>();
        public static List<UserWithStatus> UsersWithStatus
        {
            get
            {
                lock (_lockObj3)
                {
                    return _usersWithStatus;
                }
            }
            set
            {
                lock (_lockObj3)
                {
                    _usersWithStatus = value;
                }
            }
        }

        // ctor
        public GameAndChatManager()
        {
            lock (_lockObj11)
            {
                if (UsersWithStatus.Count == 0)
                 {
                    List<User> users = UsersRepository.GetUsers().ToList();
                    foreach (User user in users)
                    {
                        UserWithStatus userToAdd = new UserWithStatus { UserName = user.UserName, Status = StatusOfConnection.Offline };
                        UsersWithStatus.Add(userToAdd);
                    }
                }
            }           
        }

        // register
        [HubMethodName("GetNewUser")]
        public bool GetNewUser(User newUser)
        {
            lock (_lockObj5)
            {
                if (IsExistUser(newUser))
                {
                    return false;
                }
                UsersRepository.Add(newUser);
                UserWithStatus userWithStatus = new UserWithStatus { UserName = newUser.UserName, Status = StatusOfConnection.Offline };
                UsersWithStatus.Add(userWithStatus);
                UpdateAllUsersNamesAndConnectionStatus();
                return true;
            }
        }

        [HubMethodName("ValidateLogin")]
        public Response ValidateLogin(User userToValidate)
        {
            lock (_lockObj7)
            {
                Response response = new Response();

                if (userToValidate == null)
                {
                    response.IsOk1 = false;
                    response.Message1 = "Something went wrong, please fill in all the parameters";
                    return response;
                }

                IEnumerable<User> users = UsersRepository.GetUsers();



                foreach (User user in users)
                {
                    if (user.UserName.Equals(userToValidate.UserName))
                    {
                        if (user.Password.Equals(userToValidate.Password))
                        {
                            foreach (var item in UsersNames)
                            {
                                if ((item.Key).Equals(userToValidate.UserName))
                                {
                                    response.IsOk1 = false;
                                    response.Message1 = "The user is already connected!!";
                                    return response;
                                }
                            }

                            SignIn(userToValidate.UserName);
                            response.Message1 = "You are now connected to the system, Have fun! You can chat or play backgammon with other users";
                            response.IsOk1 = true;
                            return response;
                        }
                        response.Message1 = "Password is not correct, Please try again";
                        response.IsOk1 = false;
                        return response;
                    }

                }
                response.Message1 = "Username is not correct, Please try again";
                response.IsOk1 = false;
                return response;
            }
        }

        public void SignIn(string name)
        {
            lock (_lockObj10)
            {
                UsersConnections.Add(Context.ConnectionId, name);
                UsersNames.Add(name, Context.ConnectionId);
                foreach (var user in UsersWithStatus.Where(u => u.UserName == name))
                {
                    user.Status = StatusOfConnection.Available;
                }
                UpdateAllUsersNamesAndConnectionStatus();
            }
            
        }

        private bool IsExistUser(User newUser)
        {
            IEnumerable<User> users = UsersRepository.GetUsers();

            //return users.Where(u => u.UserName == newUser.UserName).Count() > 0;
            return users.FirstOrDefault(u => u.UserName == newUser.UserName) != null;
        }

        public void UpdateAllUsersNamesAndConnectionStatus()
        {
            lock (_lockObj6)
            {
                Clients.All.UpdateAllUsersNamesAndConnectionStatus(UsersWithStatus);
            }
        }

        [HubMethodName("ValidateAvailabilityOfOtherUser")]
        public bool ValidateAvailabilityOfOtherUser(string userToConnectTo, GameOrChatRequestEnum request)
        {
            string connectionId;
            if (UsersNames.TryGetValue(userToConnectTo, out connectionId))
            {
                foreach (UserWithStatus user in UsersWithStatus)
                {
                    if (user.UserName.Equals(userToConnectTo))
                    {
                        if ((user.Status == StatusOfConnection.Available || user.Status == StatusOfConnection.BusyOnGame) && request == GameOrChatRequestEnum.Chat)
                        {
                            return true;
                        }
                        if ((user.Status == StatusOfConnection.Available || user.Status == StatusOfConnection.BusyOnChat) && request == GameOrChatRequestEnum.Game)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        [HubMethodName("AskUserToStartChat")]
        public void AskUserToStartChat(string receiverName, string sender)
        {
            string connectionIdOfReceiver = UsersNames[receiverName];
            Clients.Client(connectionIdOfReceiver).ChatRequest(sender);
        }

        [HubMethodName("ApproveToChat")]
        public void ApproveToChat(bool approve, string receiver, string sender)
        {
            string connectionIdOfSender = UsersNames[sender];
            Clients.Client(connectionIdOfSender).responseOfChatRequestToSender(approve, receiver);
            if (approve)
            {
                string connectionIdOfReceiver = UsersNames[receiver];
                Clients.Client(connectionIdOfReceiver).responseOfChatRequestToSender(approve, sender);
                foreach (var item in UsersWithStatus)
                {
                    if ((item.UserName).Equals(receiver) || (item.UserName).Equals(sender))
                    {
                        if (item.Status == StatusOfConnection.BusyOnGame)
                        {
                            item.Status = StatusOfConnection.BusyOnChatAndGame;
                        }
                        else
                        {
                            item.Status = StatusOfConnection.BusyOnChat;
                        }
                    }
                }
                UpdateAllUsersNamesAndConnectionStatus();
            }
        }

        [HubMethodName("SendMessage")]
        public void SendMessage(string messageToSend, string sender, string receiver)
        {
            lock (_lockObj8)
            {
                string connectionIdOfReceiver = UsersNames[receiver];
                Clients.Client(connectionIdOfReceiver).GetMessage(messageToSend);
            }
        }

        [HubMethodName("ApproveToGame")]
        public void ApproveToGame(bool approve, string receiver, string sender)
        {
            string connectionIdOfSender = UsersNames[sender];
            Clients.Client(connectionIdOfSender).responseOfGameRequestToSender(approve, receiver, sender);
            if (approve)
            {
                string connectionIdOfReceiver = UsersNames[receiver];
                Clients.Client(connectionIdOfReceiver).responseOfGameRequestToSender(approve, sender, sender);
                foreach (var item in UsersWithStatus)
                {
                    if ((item.UserName).Equals(receiver) || (item.UserName).Equals(sender))
                    {
                        if (item.Status == StatusOfConnection.BusyOnChat)
                        {
                            item.Status = StatusOfConnection.BusyOnChatAndGame;
                        }
                        else
                        {
                            item.Status = StatusOfConnection.BusyOnGame;
                        }
                    }
                }
                UpdateAllUsersNamesAndConnectionStatus();
            }
        }

        [HubMethodName("AskUserToStartGame")]
        public void AskUserToStartGame(string receiverName, string sender)
        {
            string connectionIdOfReceiver = UsersNames[receiverName];
            Clients.Client(connectionIdOfReceiver).GameRequest(sender);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            if (stopCalled)
            {
                var con = Context.ConnectionId;
                string nameToRemove = UsersConnections[con];
                UsersNames.Remove(nameToRemove);
                UsersConnections.Remove(Clients.Caller);
            
                foreach (var item in UsersWithStatus)
                {
                    if ((item.UserName).Equals(nameToRemove))
                    {
                        item.Status = StatusOfConnection.Offline;
                    }
                }
                UpdateAllUsersNamesAndConnectionStatus();
            }
            return base.OnDisconnected(stopCalled);
        }

        [HubMethodName("SendBoard")]
        public void SendBoard(string userName, string otherUser)
        {
            lock (_lockObj4)
            {
                BoardState bs = new BoardState();
                bs.Cubes = RollDice();
                string connectionIdOfSender = UsersNames[userName];
                string connectionIdOfReceiver = UsersNames[otherUser];

                bool canMove = ValidateCanMoveAnywhereBeforeMakingAMove(bs);

                while (!canMove)
                {
                    bs.Cubes = RollDice();
                    canMove = ValidateCanMoveAnywhereBeforeMakingAMove(bs);
                    if (canMove)
                    {
                        break;
                    }
                    bs.CurrentUserCanMove = false;
                    if (bs.RedTurn)
                    {
                        bs.RedMoves = 0;
                        bs.RedTurn = false;
                        bs.Message = "Red can't do any move, swithing turns to Black!";

                    }
                    else
                    {
                        bs.BlackMoves = 0;
                        bs.RedTurn = true;
                        bs.Message = "Black can't do any move, swithing turns to Red!";
                    }
                    Clients.Client(connectionIdOfSender).broadcastBoard(bs);
                    Clients.Client(connectionIdOfReceiver).broadcastBoard(bs);
                }

                if (bs.RedTurn)
                {
                    bs.BlackMoves = 0;
                    bs.RedMoves = bs.Cubes.Moves;
                }
                else
                {
                    bs.RedMoves = 0;
                    bs.BlackMoves = bs.Cubes.Moves;
                }

                bs.CurrentUserCanMove = true;
                Clients.Client(connectionIdOfSender).broadcastBoard(bs);
                Clients.Client(connectionIdOfReceiver).broadcastBoard(bs);
            }
        }

        [HubMethodName("GetMoveFromUser")]
        public void GetMoveFromUser(Move currentMove, string userName, string otherUser)
        {
            lock (_lockObj9)
            {
                Response res = new Response();
                res = ValidateCurrentMoveIsLegalAndPerformIt(currentMove);
                string connectionIdOfSender = UsersNames[userName];
                string connectionIdOfReceiver = UsersNames[otherUser];
                bool canMoveThisTurn = res.IsOk1;
                bool canMoveNextTurnForThisUser;
                bool canMoveNextTurnForNextUser;
                string msg;

                if (!canMoveThisTurn)
                {
                    Clients.Client(connectionIdOfSender).Message("This move is not legal, please try another move");
                    return;
                }

                if (canMoveThisTurn)
                {
                    res = CheckWin(res.CurrentBoardState);
                    if (res.IsWin)
                    {
                        Clients.Client(connectionIdOfReceiver).Win(res.CurrentBoardState);
                        Clients.Client(connectionIdOfSender).Win(res.CurrentBoardState);
                        return;
                    }

                    for (int i = res.CurrentBoardState.Cubes.CubesNumbers.Length - 1; i >= 0; i--)
                    {
                        if (res.CurrentBoardState.Cubes.CubesNumbers[i] == currentMove.ChosenCube)
                        {
                            res.CurrentBoardState.Cubes.CubesNumbers[i] = 0;
                            break;
                        }
                    }
                    canMoveNextTurnForThisUser = ValidateCanMoveAnywhereBeforeMakingAMove(res.CurrentBoardState); // only if ther is a move and cubes are not zero
                    if (res.CurrentBoardState.RedMoves != 0 && res.CurrentBoardState.RedTurn || res.CurrentBoardState.BlackMoves != 0 && !res.CurrentBoardState.RedTurn)
                    {

                        if (!canMoveNextTurnForThisUser)
                        {
                            if (res.CurrentBoardState.RedTurn)
                            {
                                res.CurrentBoardState.RedMoves = 0;
                                res.CurrentBoardState.CurrentUserCanMove = false;
                                msg = "Red can't do any move, swithing turns to Black!";
                                Clients.Client(connectionIdOfReceiver).Message(msg);
                                Clients.Client(connectionIdOfSender).Message(msg);
                            }
                            else
                            {
                                res.CurrentBoardState.BlackMoves = 0;
                                res.CurrentBoardState.CurrentUserCanMove = false;
                                msg = "Black can't do any move, swithing turns to Red!";
                                Clients.Client(connectionIdOfReceiver).Message(msg);
                                Clients.Client(connectionIdOfSender).Message(msg);
                            }
                        }
                    }
                    if (res.CurrentBoardState.RedMoves == 0 && res.CurrentBoardState.BlackMoves == 0 || !canMoveNextTurnForThisUser)
                    {
                        res.CurrentBoardState.Cubes = RollDice();
                        if (res.CurrentBoardState.RedTurn)
                        {
                            res.CurrentBoardState.BlackMoves = res.CurrentBoardState.Cubes.Moves;
                            res.CurrentBoardState.RedTurn = false;
                            res.CurrentBoardState.RedMoves = 0;
                        }
                        else
                        {
                            res.CurrentBoardState.RedMoves = res.CurrentBoardState.Cubes.Moves;
                            res.CurrentBoardState.RedTurn = true;
                            res.CurrentBoardState.BlackMoves = 0;
                        }

                        canMoveNextTurnForNextUser = ValidateCanMoveAnywhereBeforeMakingAMove(res.CurrentBoardState);

                        while (!canMoveNextTurnForNextUser)
                        {
                            res.CurrentBoardState.Cubes = RollDice();

                            res.CurrentBoardState.CurrentUserCanMove = false;
                            if (res.CurrentBoardState.RedTurn)
                            {
                                res.CurrentBoardState.BlackMoves = res.CurrentBoardState.Cubes.Moves;
                                res.CurrentBoardState.RedMoves = 0;
                                res.CurrentBoardState.RedTurn = false;
                                msg = "Red can't do any move, swithing turns to Black!";
                            }
                            else
                            {
                                res.CurrentBoardState.RedMoves = res.CurrentBoardState.Cubes.Moves;
                                res.CurrentBoardState.BlackMoves = 0;
                                res.CurrentBoardState.RedTurn = true;
                                msg = "Black can't do any move, swithing turns to Red!";
                            }

                            Clients.Client(connectionIdOfReceiver).Message(msg);
                            Clients.Client(connectionIdOfSender).Message(msg);
                            canMoveNextTurnForNextUser = ValidateCanMoveAnywhereBeforeMakingAMove(res.CurrentBoardState);
                        }

                        res.CurrentBoardState.CurrentUserCanMove = true;
                        Clients.Client(connectionIdOfSender).broadcastBoard(res.CurrentBoardState);
                        Clients.Client(connectionIdOfReceiver).broadcastBoard(res.CurrentBoardState);

                        return;
                    }

                    res.CurrentBoardState.CurrentUserCanMove = true;
                    Clients.Client(connectionIdOfReceiver).broadcastBoard(res.CurrentBoardState);
                    Clients.Client(connectionIdOfSender).broadcastBoard(res.CurrentBoardState);
                }
            }
        }

        private bool ValidateCanMoveAnywhereBeforeMakingAMove(BoardState bs)
        {
            bs.BlackAllInHome = IsAllInHomeBlack(bs);
            bs.RedAllInHome = IsAllInHomeRed(bs);

            if (bs.RedTurn) // Red turn:
            {
                if (bs.RedAllInHome) // it is Red's turn and he has all the discs at "home", and want to get them out (Red all in home means that there are no eatten discs by the other player)
                {
                    for (int i = bs.BoardArray.Length - 6; i < bs.BoardArray.Length; i++)
                    {
                        for (int j = 0; j < bs.Cubes.CubesNumbers.Length; j++)
                        {
                            if (i + bs.Cubes.CubesNumbers[j] > 23) // if the cubes bring the disc behind the end, it means the user can get the disc "out". cubes[j] == 0, when not double, the condition will not happen.
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (bs.RedEatten > 0) // it is Red's turn and he has eatten discs which he wants to bring back to the board
                {
                    for (int i = 0; i < bs.Cubes.CubesNumbers.Length; i++)
                    {
                        if (bs.Cubes.CubesNumbers[i] == 0)
                        {
                            continue;
                        }
                        if (bs.Cubes.CubesNumbers[i] != 0 && bs.BoardArray[bs.Cubes.CubesNumbers[i] - 1] >= -1) // if the cube is with a number and not the zero ones (for double), the place on the board 0,1,2,3,4,5 should be available (if it has -1 in it (one black disc) or 0 (none discs) or more (Red discs)), for example: cube[0]=1, board[cube[0]-1] means board 0, should be available. at least on place should be available!!
                        {
                            return true;
                        }
                    }
                }
                else // it is Red's turn and he wants to make a move on board, not all discs are at home, and none discs are eatten.
                {
                    for (int i = 0; i < bs.BoardArray.Length; i++)
                    {
                        for (int j = 0; j < bs.Cubes.CubesNumbers.Length; j++)
                        {
                            if (i + bs.Cubes.CubesNumbers[j] > 23 || bs.Cubes.CubesNumbers[j] == 0) // this move is not legal (going outside the board, when not all the discs are at home), the for loop is going to the next iteration (next cube)
                            {
                                continue;
                            }
                            if (bs.BoardArray[i] > 0 && bs.BoardArray[i + bs.Cubes.CubesNumbers[j]] >= -1) // can make the move, the board place is available (if it has -1 in it (one black disc) or 0 (none discs) or more (Red discs))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            else // if black turn:
            {
                if (bs.BlackAllInHome) // it is black's turn and he has all the discs at "home", and want to get them out
                {
                    for (int i = 0; i < 6; i++)
                    {
                        for (int j = 0; j < bs.Cubes.CubesNumbers.Length; j++)
                        {
                            if (bs.BoardArray[i] < 0 && i - bs.Cubes.CubesNumbers[j] < 0)
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (bs.BlackEatten > 0) // it is black's turn and he has eatten discs which he wants to bring back to the board
                {
                    for (int i = 0; i < bs.Cubes.CubesNumbers.Length; i++)
                    {
                        if (bs.Cubes.CubesNumbers[i] == 0)
                        {
                            continue;
                        }
                        if (bs.BoardArray[bs.BoardArray.Length - bs.Cubes.CubesNumbers[i]] <= 1)
                        {
                            return true;
                        }
                    }
                }
                else
                {            // it is black's turn and he wants to make a move on board       
                    for (int i = 0; i < bs.BoardArray.Length; i++)
                    {
                        for (int j = 0; j < bs.Cubes.CubesNumbers.Length; j++)
                        {
                            if (i - bs.Cubes.CubesNumbers[j] < 0 || bs.Cubes.CubesNumbers[j] == 0)
                            {
                                continue;
                            }
                            if (bs.BoardArray[i] < 0 && bs.BoardArray[i - bs.Cubes.CubesNumbers[j]] <= 1)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        // this function check if a move is valid, by checking who's turn is it, if the player have "eatten" discs or not, and if the chosen disc and the chosen cube enables the move according to the game's rules. which means, only if the cell he is coming from have discs, and only if the cell he wants to go to, is either empty/has the player's own discs in it/has just 1 disc of the other player. If the player wants to get the disc "out", (only when all discs are at "home") - there is a special button for it.
        private Response ValidateCurrentMoveIsLegalAndPerformIt(Move move)
        {
            Response res = new Response();
            move.CurrentBoardState.BlackAllInHome = IsAllInHomeBlack(move.CurrentBoardState);
            move.CurrentBoardState.RedAllInHome = IsAllInHomeRed(move.CurrentBoardState);
            move.CurrentBoardState = HowManyOnBoard(move.CurrentBoardState);
            res.CurrentBoardState = move.CurrentBoardState;

            if (move.CurrentBoardState.RedTurn && move.CurrentBoardState.RedEatten == 0 && move.CurrentBoardState.BoardArray[move.From] <= 0)
            {
                res.IsOk1 = false;
                return res;
            }
            if (!move.CurrentBoardState.RedTurn && move.CurrentBoardState.BlackEatten == 0 && move.CurrentBoardState.BoardArray[move.From] >= 0)
            {
                res.IsOk1 = false;
                return res;
            }

            // logic of movment for Red:
            if (move.CurrentBoardState.RedTurn && move.CurrentBoardState.RedAllInHome)
            {
                if (move.From + move.ChosenCube > 23)
                {
                    move.CurrentBoardState.RedDiscsOnBoard--;
                    move.CurrentBoardState.OutRed++;
                    move.CurrentBoardState.BoardArray[move.From]--;
                    move.CurrentBoardState.RedMoves--;
                    res.IsOk1 = true;
                    res.CurrentBoardState = move.CurrentBoardState;
                    return res;
                }
            }
            if (!move.CurrentBoardState.RedTurn && move.CurrentBoardState.BlackAllInHome)
            {
                if (move.From - move.ChosenCube < 0)
                {
                    move.CurrentBoardState.BlackDiscsOnBoard--;
                    move.CurrentBoardState.OutBlack++;
                    move.CurrentBoardState.BoardArray[move.From]++;
                    move.CurrentBoardState.BlackMoves--;
                    res.IsOk1 = true;
                    res.CurrentBoardState = move.CurrentBoardState;
                    return res;
                }
            }
            if (move.CurrentBoardState.RedEatten == 0 && move.CurrentBoardState.RedTurn)
            {
                if (move.CurrentBoardState.BoardArray[move.From + move.ChosenCube] >= -1)
                {
                    if (move.CurrentBoardState.BoardArray[move.From + move.ChosenCube] == -1)
                    {
                        move.CurrentBoardState.BoardArray[move.From]--;
                        move.CurrentBoardState.BoardArray[move.From + move.ChosenCube] = 1;
                        move.CurrentBoardState.BlackEatten++;
                        move.CurrentBoardState.RedMoves--;
                        res.IsOk1 = true;
                        res.CurrentBoardState = move.CurrentBoardState;
                        return res;
                    }
                    else if (move.CurrentBoardState.BoardArray[move.From + move.ChosenCube] < -1)
                    {
                        res.IsOk1 = false;
                        return res;
                    }
                    else if (move.CurrentBoardState.BoardArray[move.From + move.ChosenCube] >= 0)
                    {
                        move.CurrentBoardState.BoardArray[move.From]--;
                        move.CurrentBoardState.BoardArray[move.From + move.ChosenCube]++;
                        move.CurrentBoardState.RedMoves--;
                        res.IsOk1 = true;
                        res.CurrentBoardState = move.CurrentBoardState;
                        return res;
                    }
                }

            }
            else if (move.CurrentBoardState.RedEatten > 0 && move.CurrentBoardState.BoardArray[move.ChosenCube - 1] >= -1 && move.CurrentBoardState.RedTurn)
            {
                if (move.CurrentBoardState.BoardArray[move.ChosenCube - 1] == -1)
                {
                    move.CurrentBoardState.BlackEatten++;
                    move.CurrentBoardState.BoardArray[move.ChosenCube - 1] = 1;
                    move.CurrentBoardState.RedEatten--;
                    move.CurrentBoardState.RedMoves--;
                    res.IsOk1 = true;
                    res.CurrentBoardState = move.CurrentBoardState;
                    return res;
                }
                else
                {
                    move.CurrentBoardState.BoardArray[move.ChosenCube - 1]++;
                    move.CurrentBoardState.RedEatten--;
                    move.CurrentBoardState.RedMoves--;
                    res.IsOk1 = true;
                    res.CurrentBoardState = move.CurrentBoardState;
                    return res;
                }
            }

            // logic of movment for black:
            if (move.CurrentBoardState.BlackEatten == 0 && !move.CurrentBoardState.RedTurn)
            {
                if (move.CurrentBoardState.BoardArray[move.From - move.ChosenCube] <= 1)
                {
                    if (move.CurrentBoardState.BoardArray[move.From - move.ChosenCube] == 1)
                    {
                        move.CurrentBoardState.BoardArray[move.From]++;
                        move.CurrentBoardState.BoardArray[move.From - move.ChosenCube] = -1;
                        move.CurrentBoardState.RedEatten++;
                        move.CurrentBoardState.BlackMoves--;
                        res.IsOk1 = true;
                        res.CurrentBoardState = move.CurrentBoardState;
                        return res;
                    }
                    else if (move.CurrentBoardState.BoardArray[move.From - move.ChosenCube] > 1)
                    {
                        res.IsOk1 = false;
                        return res;
                    }
                    else if (move.CurrentBoardState.BoardArray[move.From - move.ChosenCube] <= 0)
                    {
                        move.CurrentBoardState.BoardArray[move.From]++;
                        move.CurrentBoardState.BoardArray[move.From - move.ChosenCube]--;
                        move.CurrentBoardState.BlackMoves--;
                        res.IsOk1 = true;
                        res.CurrentBoardState = move.CurrentBoardState;
                        return res;
                    }
                }
            }
            else if (move.CurrentBoardState.BlackEatten > 0 && move.CurrentBoardState.BoardArray[24 - move.ChosenCube] <= 1 && !move.CurrentBoardState.RedTurn)
            {
                if (move.CurrentBoardState.BoardArray[24 - move.ChosenCube] == 1)
                {
                    move.CurrentBoardState.RedEatten++;
                    move.CurrentBoardState.BoardArray[24 - move.ChosenCube] = -1;
                    move.CurrentBoardState.BlackEatten--;
                    move.CurrentBoardState.BlackMoves--;
                    res.IsOk1 = true;
                    res.CurrentBoardState = move.CurrentBoardState;
                    return res;
                }
                else
                {
                    move.CurrentBoardState.BoardArray[24 - move.ChosenCube]--;
                    move.CurrentBoardState.BlackEatten--;
                    move.CurrentBoardState.BlackMoves--;
                    res.IsOk1 = true;
                    res.CurrentBoardState = move.CurrentBoardState;
                    return res;
                }
            }
            return res;
        }

        // this function "rolls" the dice: puts random numbers in the array named "cubes", if 2 cubes have the same number, all the cubes (there are 4 of them) will get the same number. It returns the cubes which include an array of the cube numbers, the number of moves, and a boolean to tell if there is a double or not in the cubes number (both cubes are the same)
        private Cubes RollDice()
        {
            Cubes cubes = new Cubes();
            Random rnd = new Random();
            cubes.CubesNumbers[0] = rnd.Next(1, 7);
            cubes.CubesNumbers[1] = rnd.Next(1, 7);

            if (cubes.CubesNumbers[0] == cubes.CubesNumbers[1]) // means that there is a "double", and the player gets 4 moves to make insted of 2.
            {
                cubes.CubesNumbers[2] = cubes.CubesNumbers[3] = cubes.CubesNumbers[0];
                cubes.IsDouble = true;
                cubes.Moves = 4;
            }
            return cubes;
        }

        private BoardState HowManyOnBoard(BoardState bs)
        {
            int discsOnBoardRed = 0;
            int discsOnBoardBlack = 0;
            for (int i = 0; i < bs.BoardArray.Length; i++)
            {
                if (bs.BoardArray[i] > 0)
                {
                    discsOnBoardRed += bs.BoardArray[i];
                }
                else if (bs.BoardArray[i] < 0)
                {
                    discsOnBoardBlack += Math.Abs(bs.BoardArray[i]);
                }
            }
            bs.RedDiscsOnBoard = discsOnBoardRed;
            bs.BlackDiscsOnBoard = discsOnBoardBlack;
            return bs;
        }

        public bool IsAllInHomeRed(BoardState bs)
        {
            if (bs.RedEatten == 0)
            {
                for (int i = 0; i < bs.BoardArray.Length - 6; i++)
                {
                    if (bs.BoardArray[i] > 0)
                    {
                        return false;
                    }
                }
                bs.RedAllInHome = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsAllInHomeBlack(BoardState bs)
        {
            if (bs.BlackEatten == 0)
            {
                for (int i = 6; i < bs.BoardArray.Length; i++)
                {
                    if (bs.BoardArray[i] < 0)
                    {
                        return false;
                    }
                }
                bs.BlackAllInHome = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public Response CheckWin(BoardState bs)
        {
            Response res = new Response();
            res.CurrentBoardState = bs;
            if (bs.RedDiscsOnBoard == 0)
            {
                res.CurrentBoardState.IsRedWin = true;
                res.IsWin = true;
                return res;
            }
            else if (bs.BlackDiscsOnBoard == 0)
            {
                res.CurrentBoardState.IsRedWin = false;
                res.IsWin = true;
                return res;
            }
            res.IsWin = false;
            return res;
        }
    }
}