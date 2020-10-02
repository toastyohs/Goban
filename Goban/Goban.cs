using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Linq.Expressions;

namespace Goban
{

    enum Color { None, Black, White };
    enum Markup { None, Arrow, Circle, Text, Line, X, Selected, Square, Triangle, Dim, BlackTerritory, WhiteTerritory }

    enum MoveQuality { GoodForBlack, GoodForWhite, Even, Hotspot, BadMove, GoodMove, Interesting, Unclear, Doubtful}

    enum ByoYomiType { None, Japanese, Canadian}


    class Section
    {
        List<BoardNode> ThisSection = new List<BoardNode>();

        List<Section> Children = new List<Section>();

        public void AddNodeToSection(BoardNode board)
        {
            if (ThisSection.Count == 0)
            {
                board.SetId(1);
            }
            else
            {
                board.SetId(ThisSection[ThisSection.Count - 1].Id + 1 ?? 1);
            }
        }

        public void AddChild(Section s, int ParentId)
        {
            foreach(BoardNode node in s.ThisSection)
            {
                node.SetParent(ParentId);
            }
            Children.Add(s);
        }

        public void FillChildIds(Section s)
        {
            for(int i = 0; i <= s.ThisSection.Count; i++)
            {
                s.ThisSection[i].SetChildId(i + 1); //weird to have an id of '0' so whatever lol
            }
        }
    }
    class Stone
    {
        
        public Color Color { get; private set; } = Color.None;
        public Markup Annotation { get; private set; }

        public string MarkupText { get; private set; }

        public Stone()
        {
            Color = Color.None;
        }

        public Stone(Color thisColor)
        {
            Color = thisColor;
        }
        public Stone(Color thisColor, Markup thisMarkup)
        {
            Color = thisColor;
            Annotation = thisMarkup;
        }
        public Stone(Color thisColor, Markup thisMarkup, string text)
        {
            Color = thisColor;
            Annotation = thisMarkup;
            MarkupText = text;
        }

        public void SetMarkup(Markup thisMarkup, string text = null)
        {
            Annotation = thisMarkup;
            if(!string.IsNullOrWhiteSpace(text))
            {
                MarkupText = text;
            }
        }

        public void SetColor(Color c)
        {
            Color = c;
        }
    }
    class Coordinates
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coordinates(char x, char y)
        {
            X = (int)x - 97;
            Y = (int)y - 97;
        }

        public Coordinates(int x, int y)
        {
            X = x;
            Y = y;
        }

        //to deal with when an SGF has double coordinates, signifying a box
        public static Coordinates[] Split(string coordinates)
        {
            string[] coords = coordinates.Split(':');

            //string cleanup, leaving only the raw letters
            for(int i = 0; i < coords.Length; i++)
            {
                coords[i] = coords[i].Replace("[", String.Empty);
                coords[i] = coords[i].Replace("]", String.Empty);

            }

            //now that it's clean, make a list of Coordinates based off of the char constructor
            List<Coordinates> final = new List<Coordinates>();

            foreach(string s in coords)
            {
                final.Add(new Coordinates(s[0], s[1]));
            }

            return final.ToArray();
        }

        public bool IsInBounds(int BoardSize)
        {
            return X >= 0 && X < BoardSize && Y >= 0 && Y < BoardSize;
        }

    }
    class BoardNode
    {
        #region variables
        public int? Id { get; private set; } 
        public int? ParentId { get; private set; }
        public int? ChildId { get; private set; }
        public int BoardSize { get; private set; } = 19; //default to the big boy board
        public Stone[,] BoardState { get; set; }
        public Color NextColor { get; private set; } = Color.Black;

        public List<BoardNode[]> children = new List<BoardNode[]>();
        public MoveQuality? Quality { get; set; }
        public int MoveQualitySeverity { get; set; } //VERY good for white, black, etc. likely not used but still trying to be in spec with SGF 
        public string NodeName { get; set; }
        public string Comment { get; set; }
        public string BlackName { get; set; }
        public string WhiteName { get; set; }
        public string BlackTeamName { get; set; } 
        public string WhiteTeamName { get; set; }
        public string BlackRank { get; set; }
        public string WhiteRank { get; set; }
        public string AnnotatorName { get; set; }
        public string PlacePlayed { get; set; }
        public string Result { get; set; }
        public int Handicap { get; set; }
        public int Value { get; set; }
        public TimeSpan BlackTimeLeft { get; set; } = new TimeSpan();
        public TimeSpan WhiteTimeLeft { get; set; } = new TimeSpan();
        public double Komi { get; set; } = 7.5; //Default to AGA komi until there's a reason to change the default
        public string Source { get; set; } //source of file
        public string RuleSet { get; set; } = "AGA"; //again, default to AGA ruleset
        public string TimeSettings { get; set; }
        public int BlackByoYomiPeriods { get; set; } //likely going to use this for both Canadian OT moves and Japanese OT periods
        public int WhiteByoYomiPeriods { get; set; }
        public TimeSpan BlackByoYomiTime { get; set; }
        public TimeSpan WhiteByoYomiTime { get; set; }
        public string GameRecorderName { get; set; }
        public string GameName { get; set; }
        public string ExtraGameInfo { get; set; }
        public ByoYomiType ByoYomi { get; set; } = ByoYomiType.Japanese; //Let's face it, no one uses Canadian. Except IGS
        public string ByoYomiSettings { get; set; }
        public string OpeningPlayed { get; set; }
        public string AppUsed { get; set; } = "Ricky's unnamed go app, that will 100% not be some weeabo name, version 0.01";
        public string CharsetUsed { get; set; } //this changing is unlikely unless my shit goes global, and in that case this was a good use of time anyway lmao
        public int SgfVersion { get; set; } = 4; //I don't see me using old shit, but I might? studying backwards compatibility is a thing reserved for apps that function first lol
        public int GameType { get; set; } = 1; //1 = go. this is 100% not going to change. If I were going to make a different SGF reading program for those games, I'd likely put them in their own project.
        public string DatePlayed { get; set; }
        public int MoveNumber { get; set; } = 1;
        public string CopyrightInfo { get; set; } = "I'll stomp your bitch ass if you try to claim my shit as your own. All rights reserved.";
        public string EventName { get; set; }
        public int MarkupStyle { get; set; } = 2; //the default style for problems - also not really implemented until I do it, but is something that may come up in someone's SGF
        public string Round { get; set; }


        #endregion

        #region methods
        public BoardNode()
        {
            BoardState = new Stone[BoardSize, BoardSize];

            for (int i = 0; i < BoardSize; i++)
            {
                for(int j = 0; j < BoardSize; j++)
                {
                    BoardState[i, j] = new Stone();
                }
            }
        }

        public void SetParent(int id)
        {
            ParentId = id;
        }

        public void SetId(int number)
        {
            Id = number;
        }

        public void SetChildId(int number)
        {
            ChildId = number;
        }


        public BoardNode(Stone[,] currentBoard)
        {
            BoardState = (Stone[,])currentBoard.Clone();
        }

        public BoardNode Place(Stone stone, Coordinates c)
        {
            Stone[,] TempBoard = CloneBoard().BoardState;

            TempBoard[c.X, c.Y] = stone;

            return new BoardNode
            {
                BoardSize = BoardSize,
                BoardState = TempBoard
            };
        }

        public BoardNode Place(Coordinates c)
        {

            if(BoardState[c.X, c.Y].Color != Color.None)
            {
                return null;
            }
            
            bool isKill = false;
           
            BoardState[c.X, c.Y].SetColor(GetNextColor());

            var Group = GetGroup(c);


            var AdjacentGroups = GetAdjacentGroups(Group);


            foreach (var ThisGroup in AdjacentGroups)
            {
                if (IsDead(ThisGroup))
                {
                    RemoveGroup(ThisGroup);
                    isKill = true; //mark that the placed stone that didn't have any liberties still killed something
                }
            }

            
            if (LibertiesLeft(Group) == 0 && !isKill)
            {
                BoardState[c.X, c.Y].SetColor(Color.None);
                return null;
            }

            return new BoardNode
            {
                BoardState = CloneBoard().BoardState,
                NextColor = NextColor

            };
            
        }

        //determine which parts of the array are part of a contiguous group of stones
        public Coordinates[] GetGroup(Coordinates OriginStone)
        {
            List<Coordinates> GroupStones = new List<Coordinates>();

            GroupStones.Add(OriginStone);

            var StoneColor = BoardState[OriginStone.X, OriginStone.Y];

            while (true)
            {
                var TempStones = GroupStones.ToList();
                
                foreach( Coordinates c in TempStones)
                {
                    if (c.Y + 1 < BoardSize && BoardState[c.X, c.Y + 1] == StoneColor && !GroupStones.Where(x => x.X == c.X && x.Y == c.Y + 1).Any())
                        GroupStones.Add(new Coordinates (c.X, c.Y + 1));
                    if (c.Y - 1 >= 0 && BoardState[c.X, c.Y - 1] == StoneColor && !GroupStones.Where(x => x.X == c.X && x.Y == c.Y - 1).Any())
                        GroupStones.Add(new Coordinates(c.X, c.Y - 1));
                    if (c.X + 1 < BoardSize && BoardState[c.X + 1, c.Y] == StoneColor && !GroupStones.Where(x => x.X == c.X + 1 && x.Y == c.Y).Any())
                        GroupStones.Add(new Coordinates(c.X + 1, c.Y));
                    if (c.X - 1 >= 0 && BoardState[c.X - 1, c.Y] == StoneColor && !GroupStones.Where(x => x.X == c.X - 1 && x.Y == c.Y).Any())
                        GroupStones.Add(new Coordinates(c.X - 1, c.Y));
                }

                if (GroupStones.Count == TempStones.Count)
                    break;
                
            }

            return GroupStones.ToArray();
        }
        public void RemoveGroup(Coordinates[] group)
        {
            foreach(Coordinates co in group)
            {
                BoardState[co.X, co.Y].SetColor(Color.None);
            }

        }
        public bool IsDead(Coordinates[] Group)
        {
            return LibertiesLeft(Group) == 0;
        }
        //take a group of stones that are part of the current coordinates and determine if liberties are left
        public int LibertiesLeft(Coordinates[] Group)
        {

            int liberties = 0;

            var CheckedCoordinates = new List<Coordinates>();

            foreach (Coordinates co in Group)
            {
                if (co.Y + 1 < BoardSize && BoardState[co.X, co.Y + 1].Color == Color.None && !Group.Where(x => x.X == co.X && x.Y == co.Y + 1 ).Any() && !CheckedCoordinates.Where(x => x.X == co.X && x.Y == co.Y + 1).Any())
                {
                    CheckedCoordinates.Add(new Coordinates(co.X, co.Y + 1));
                    liberties++;
                }

                if (co.Y - 1 >= 0 && BoardState[co.X, co.Y - 1].Color == Color.None && !Group.Where(x => x.X == co.X && x.Y == co.Y - 1).Any() && !CheckedCoordinates.Where(x => x.X == co.X && x.Y == co.Y - 1).Any())
                {
                    CheckedCoordinates.Add(new Coordinates(co.X, co.Y - 1));
                    liberties++;
                }
                if (co.X + 1 < BoardSize && BoardState[co.X + 1, co.Y].Color == Color.None && !Group.Where(x => x.X == co.X + 1 && x.Y == co.Y).Any() && !CheckedCoordinates.Where(x => x.X == co.X + 1 && x.Y == co.Y).Any())
                {
                    CheckedCoordinates.Add(new Coordinates(co.X + 1, co.Y));
                    liberties++;
                }
                if (co.X - 1 >= 0 && BoardState[co.X - 1, co.Y].Color == Color.None && !Group.Where(x => x.X == co.X - 1 && x.Y == co.Y).Any() && !CheckedCoordinates.Where(x => x.X == co.X - 1 && x.Y == co.Y).Any())
                {
                    CheckedCoordinates.Add(new Coordinates(co.X - 1, co.Y));
                    liberties++;
                }
            }

            return liberties;
        }

        public void SetBoardSize(int size)
        {
            BoardSize = size;
        }
        public BoardNode CloneBoard()
        {

            BoardNode tempNode = new BoardNode
            {
                BoardState = new Stone[BoardSize, BoardSize],
                OpeningPlayed = OpeningPlayed,
                AnnotatorName = AnnotatorName,
                MarkupStyle = MarkupStyle,
                CopyrightInfo = CopyrightInfo,
                BlackName = BlackName,
                BlackTeamName = BlackTeamName,
                BlackRank = BlackRank,
                WhiteName = WhiteName,
                WhiteRank = WhiteRank,
                WhiteTeamName = WhiteTeamName,
                AppUsed = AppUsed,
                BoardSize = BoardSize,
                Handicap = Handicap,
                CharsetUsed = CharsetUsed,
                Source = Source,
                SgfVersion = SgfVersion,
                RuleSet = RuleSet,
                ByoYomiSettings = ByoYomiSettings,
                ByoYomi = ByoYomi,
                EventName = EventName,
                DatePlayed = DatePlayed,
                ExtraGameInfo = ExtraGameInfo,
                GameName = GameName,
                Result = Result,
                Komi = Komi,
                Round = Round,
                GameRecorderName = GameRecorderName,
                PlacePlayed = PlacePlayed,
                TimeSettings = TimeSettings,
                NextColor = NextColor,
                BlackByoYomiPeriods = BlackByoYomiPeriods,
                WhiteByoYomiPeriods = WhiteByoYomiPeriods,
                WhiteTimeLeft = WhiteTimeLeft,
                WhiteByoYomiTime = WhiteByoYomiTime,
                BlackByoYomiTime = BlackByoYomiTime,
                BlackTimeLeft = BlackTimeLeft,
                MoveQualitySeverity = MoveQualitySeverity
            };

            for(int i = 0; i < BoardSize; i++)
            {
                for(int j = 0; j < BoardSize; j++)
                {
                    tempNode.BoardState[i, j] = new Stone(BoardState[i, j].Color);
                }
            }

            return tempNode;
        }

        //public bool IsKo()
        //{
        //    if
        //}

        public List<Coordinates[]> GetAdjacentGroups(Coordinates[] c)
        {
            List<Coordinates[]> AdjacentGroups = new List<Coordinates[]>();

            Color GroupColor = BoardState[c[0].X, c[0].Y].Color;

            foreach (Coordinates co in c)
            {
                Coordinates[] Cardinals =
                {
                    new Coordinates(co.X, co.Y + 1),
                    new Coordinates(co.X, co.Y - 1),
                    new Coordinates(co.X + 1, co.Y),
                    new Coordinates(co.X - 1, co.Y)
                };

                foreach (Coordinates x in Cardinals)
                {
                    //if it isn't an enemy group, it's technically part of the original group,
                    if (x.IsInBounds(BoardSize) && IsEnemyStone(x, GroupColor) && !c.Contains(x))
                    {
                        AdjacentGroups.Add(GetGroup(x));
                    }
                }

            }

            return AdjacentGroups.Distinct().ToList();
        }

        public bool IsEnemyStone(Coordinates c, Color rock)
        {
            if (rock == Color.Black)
                return BoardState[c.X, c.Y].Color == Color.White;
            else if(rock == Color.White)
                return BoardState[c.X, c.Y].Color == Color.Black;

            return false;
        }
        //public void PlaceChild(Stone stone, int x, int y)
        //{
        //    Stone[,] TempBoard = BoardState;

        //    TempBoard[x, y] = stone;


        //    children.Add(new BoardNode[]{ new BoardNode
        //    {
        //        Parent = this,
        //        BoardSize = BoardSize,
        //        BoardState = TempBoard
        //    }});
        //}

        public void RemoveStones(List<Coordinates> group)
        {
            foreach(Coordinates c in group)
            {
                BoardState[c.X, c.Y].SetColor(Color.None);
            }
        }
        public BoardNode Place(int x, int y)
        {
            Stone[,] TempBoard = (Stone[,])BoardState.Clone();

            TempBoard[x, y].SetColor(GetNextColor());

            return new BoardNode
            {
                BoardSize = BoardSize,
                BoardState = TempBoard
            };
        }

        //returns the current turn's stone, then changes to the opposite on the board
        public Color GetNextColor()
        {
            Color thisStone = NextColor;

            NextColor = NextColor == Color.Black ? Color.White : Color.Black;

            return thisStone;
        }
        public void SetNextColor(Color thisColor)
        {
            NextColor = thisColor;
        }

        #endregion
    }

    class GameRecord
    {
        Section Record = new Section();

        public GameRecord()
        {
            string fileText = File.ReadAllText(@"C:\Users\rickr\Downloads\Mei-1995-1.sgf");
            var sgf = new Sgf();

            Record = sgf.Parse(fileText);
        }

    }

    class Game
    {
        public BoardNode CurrentState = new BoardNode();

        public List<BoardNode> GameRecord { get; private set; } = new List<BoardNode>();


    }
}
