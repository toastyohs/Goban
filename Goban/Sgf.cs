using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Goban
{
    //all of the possible things an SGF can throw at you
    enum SgfCommand { b, w, ko, mn, ab, ae, aw, pl, c, dm, gb, gw, ho, n, uc, v,
        bm, doo, it, te, ar, cr, dd, lb, ln, ma, sl, sq, tr, ap, ca, ff, gm, st,
        sz, an, br, bt, cp, dt, ev, gv, gc, on, ot, pb, pc, pw, re, ro, ru, so, tm, us, wr,
        wt, bl, ob, ow, wl, fg, pm, vw, gn, ha, km, tb, tw }
    class Sgf
    {
        public BoardNode Board { get; private set; } = new BoardNode();

        public string GameFile { get; set; }

        public BoardNode ExecuteMove(string move)
        {
            
            move = move.Replace(";", String.Empty);

            int endOfInstruction = move.IndexOf('[');
            string instruction = move.Substring(0, endOfInstruction).ToLower(); // pull the move out
            string bracketText = move.Substring(move.IndexOf('[')).ToLower(); //pull the coordinates of the move 

            // remove the brackets from the bracket text. I guess that works? possible todo - maybe get strip to make an array of strings, so that the necessary splits are done too.
            bracketText = Strip(bracketText);


            //since 'do' can't be used as an enum value, changing instruction to 'doo' in all cases of 'do'
            instruction = instruction.Replace("do", "doo");
            

            SgfCommand command = (SgfCommand)Enum.Parse(typeof(SgfCommand), instruction);

            var TempBoard = Board.CloneBoard();

            switch (command)
            {
                //there's going to be quite a few placeholders until I get the basic SGF functionality tested.
                case SgfCommand.b:
                    if (TempBoard.NextColor == Color.Black)
                        TempBoard = TempBoard.Place(new Coordinates(bracketText[0], bracketText[1])) ?? TempBoard;
                    break;
                case SgfCommand.w:
                    if (TempBoard.NextColor == Color.White)
                        TempBoard = TempBoard.Place(new Coordinates(bracketText[0], bracketText[1])) ?? TempBoard;
                    break;
                case SgfCommand.ko:
                    //not really needed, as I should be able to place moves wherever I want, and I will make sure that ko is honored in the game tree.
                    break;
                case SgfCommand.mn:
                    TempBoard.MoveNumber = int.Parse(bracketText);
                    break;
                case SgfCommand.ab:
                    PlaceBatch(bracketText, Color.Black);
                    TempBoard.BoardState = Board.CloneBoard().BoardState; //placing the batch still works best using this object's BoardState, so cloning it to TempBoard after we're done
                    break;
                case SgfCommand.ae:
                    PlaceBatch(bracketText, Color.None);
                    TempBoard.BoardState = Board.CloneBoard().BoardState;
                    break;
                case SgfCommand.aw:
                    PlaceBatch(bracketText, Color.White);
                    TempBoard.BoardState = Board.CloneBoard().BoardState;
                    break;
                case SgfCommand.pl:
                    TempBoard.SetNextColor(bracketText.ToLower() == "b" ? Color.Black : bracketText.ToLower() == "w" ? Color.White : Board.NextColor);
                    break;
                case SgfCommand.c:
                    TempBoard.Comment = bracketText;
                    break;
                case SgfCommand.dm:
                    TempBoard.Quality = MoveQuality.Even;
                    break;
                case SgfCommand.gw:
                    TempBoard.Quality = MoveQuality.GoodForWhite;
                    break;
                case SgfCommand.ho:
                    TempBoard.Quality = MoveQuality.Hotspot;
                    break;
                case SgfCommand.n:
                    TempBoard.NodeName = bracketText;
                    break;
                case SgfCommand.uc:
                    TempBoard.Quality = MoveQuality.Unclear;
                    break;
                case SgfCommand.v:
                    TempBoard.Value = int.Parse(bracketText); //todo - type checking on bracket text, but at least here I may be able to try/catch if the type is wrong.
                    break;
                case SgfCommand.bm:
                    TempBoard.Quality = MoveQuality.BadMove;
                    break;
                case SgfCommand.doo:
                    TempBoard.Quality = MoveQuality.Doubtful; //lol, my changing to 'doo' is kind of hilarious. your doubtful move is dookie
                    break;
                case SgfCommand.it:
                    TempBoard.Quality = MoveQuality.Interesting;
                    break;
                case SgfCommand.te:
                    TempBoard.Quality = MoveQuality.GoodMove;
                    break;
                case SgfCommand.ar:
                    //arrows - may never implement this, but it does exist.
                    break;
                case SgfCommand.cr:
                    MarkupBatch(bracketText, Markup.Circle);
                    TempBoard.BoardState = Board.CloneBoard().BoardState;
                    break;
                case SgfCommand.dd:
                    MarkupBatch(bracketText, Markup.Dim);
                    TempBoard.BoardState = Board.CloneBoard().BoardState;
                    break;
                case SgfCommand.lb:
                    SetText(bracketText);
                    break;
                case SgfCommand.ln:
                    //placeholder - create a line from one point to the next. waiting until we have a real gui until we get into this
                    break;
                case SgfCommand.ma:
                    MarkupBatch(bracketText, Markup.X);
                    TempBoard.BoardState = Board.CloneBoard().BoardState;
                    break;
                case SgfCommand.sl:
                    MarkupBatch(bracketText, Markup.Selected);
                    TempBoard.BoardState = Board.CloneBoard().BoardState;
                    break;
                case SgfCommand.sq:
                    MarkupBatch(bracketText, Markup.Square);
                    break;
                case SgfCommand.tr:
                    MarkupBatch(bracketText, Markup.Triangle);
                    break;
                case SgfCommand.ap:
                    TempBoard.AppUsed = bracketText;
                    break;
                case SgfCommand.ca:
                    TempBoard.CharsetUsed = bracketText;
                    break;
                case SgfCommand.ff:
                    TempBoard.SgfVersion = int.Parse(bracketText);
                    break;
                case SgfCommand.gm:
                    //basically, not supporting other game types, so if it isn't go I'm just gonna drop out
                    if (int.Parse(bracketText) != 1)
                    {
                        throw new Exception("Non-Go game types are not supported in this app");
                    }
                    break;
                case SgfCommand.st:
                    TempBoard.MarkupStyle = int.Parse(bracketText);
                    break;
                case SgfCommand.sz:
                    TempBoard.SetBoardSize(int.Parse(bracketText));
                    break;
                case SgfCommand.an:
                    TempBoard.AnnotatorName = bracketText;
                    break;
                case SgfCommand.br:
                    TempBoard.BlackRank = bracketText;
                    break;
                case SgfCommand.bt:
                    TempBoard.BlackTeamName = bracketText;
                    break;
                case SgfCommand.cp:
                    TempBoard.CopyrightInfo = bracketText;
                    break;
                case SgfCommand.dt:
                    TempBoard.DatePlayed = bracketText;
                    break;
                case SgfCommand.ev:
                    TempBoard.EventName = bracketText;
                    break;
                case SgfCommand.gn:
                    TempBoard.GameName = bracketText;
                    break;
                case SgfCommand.gc:
                    TempBoard.ExtraGameInfo = bracketText;
                    break;
                case SgfCommand.on:
                    TempBoard.OpeningPlayed = bracketText;
                    break;
                case SgfCommand.ot:
                    TempBoard.ByoYomiSettings = bracketText; //todo, make a byo yomi parser that sets the type, periods, and time in the BoardNode object
                    break;
                case SgfCommand.pb:
                    TempBoard.BlackName = bracketText;
                    break;
                case SgfCommand.pc:
                    TempBoard.PlacePlayed = bracketText;
                    break;
                case SgfCommand.pw:
                    TempBoard.WhiteName = bracketText;
                    break;
                case SgfCommand.re:
                    TempBoard.Result = bracketText;
                    break;
                case SgfCommand.ro:
                    TempBoard.Round = bracketText;
                    break;
                case SgfCommand.ru:
                    TempBoard.RuleSet = bracketText;
                    break;
                case SgfCommand.so:
                    TempBoard.Source = bracketText;
                    break;
                case SgfCommand.tm:
                    TempBoard.TimeSettings = bracketText;
                    break;
                case SgfCommand.us:
                    TempBoard.GameRecorderName = bracketText;
                    break;
                case SgfCommand.wr:
                    TempBoard.WhiteRank = bracketText;
                    break;
                case SgfCommand.wt:
                    TempBoard.WhiteTeamName = bracketText;
                    break;
                case SgfCommand.bl:
                    TempBoard.BlackTimeLeft = TimeSpan.Parse(bracketText);
                    break;
                case SgfCommand.ob:
                    TempBoard.BlackByoYomiPeriods = int.Parse(bracketText); //sets remaining required moves as the 'periods'
                    break;
                case SgfCommand.ow:
                    TempBoard.WhiteByoYomiPeriods = int.Parse(bracketText);
                    break;
                case SgfCommand.wl:
                    TempBoard.WhiteTimeLeft = TimeSpan.Parse(bracketText);
                    break;
                case SgfCommand.fg:
                    //placeholder - divide game into figures for printing. Not in the scope of this program
                    break;
                case SgfCommand.pm:
                    //placeholder - printing stuff. Not in the scope of original program
                    break;
                case SgfCommand.vw:
                    //placeholder, set field of view. likely REALLY important for go problems. DEFINITELY in the scope lol
                    break;
                case SgfCommand.ha:
                    TempBoard.Handicap = int.Parse(bracketText);
                    break;
                case SgfCommand.km:
                    TempBoard.Komi = double.Parse(bracketText);
                    break;
                case SgfCommand.tb:
                    MarkupBatch(bracketText, Markup.BlackTerritory);
                    TempBoard.BoardState = Board.CloneBoard().BoardState;
                    break;
                case SgfCommand.tw:
                    MarkupBatch(bracketText, Markup.WhiteTerritory);
                    TempBoard.BoardState = Board.CloneBoard().BoardState;
                    break;
                default:
                    break;

            }
            Board = TempBoard;
            return TempBoard;

        }

        public List<BoardNode> Parse(string record)
        {
            List<BoardNode> GameBuilder = new List<BoardNode>();

            BoardNode FirstNode; 
            
            record = ExecuteFirstInstructions(record, out FirstNode);

            GameBuilder.Add(FirstNode.Place(new Stone(Color.None), new Coordinates(0, 0)));

            while(true)
            {
                string temp = GetNextInstruction(record);

                GameBuilder.Add(ExecuteMove(temp));

                record = record.Remove(0, temp.Length);

                if(record[0] == ')')
                {
                    break;
                }

            }

            return GameBuilder;
        }

        public string ExecuteFirstInstructions(string instructions, out BoardNode FirstBoard)
        {
            
            if(instructions[0] != '(')
            {
                FirstBoard = new BoardNode();
                return instructions;
            }


            instructions = instructions.Remove(0, 2); //removes the "(;" from the beginning of the file before starting to parse

            instructions = instructions.Replace("\n", ""); //remove page whitespace to make parsing easier.

            while(true)
            {

                int EndOfInstructionIndex = instructions.IndexOf(']');

                string NextInstruction = instructions.Substring(0, EndOfInstructionIndex + 1);

                FirstBoard = ExecuteMove(NextInstruction); //since we are not making these their own separate game moves because it's just info, we will make a temp node while we manipulate the one in the class

                
                instructions = instructions.Replace(NextInstruction, String.Empty);

                if(instructions[0] == ';')
                    break;
            }

            return instructions;

        }

        public string GetNextInstruction(string instructions)
        {
            //I believe it's safe to assume from ';' to ']' is a singular instruction
            int start = instructions.IndexOf(';');
            int length =  instructions.IndexOf(']') + 1 - start; //not certain if I need the "+1" or not for length purposes, but I'm going to leave it as is until I get to a testing point

            return instructions.Substring(start, length);
        }
        public void PlaceBatch(string coord, Color stone)
        {
            var coords = Coordinates.Split(coord);

            //only go into this if we have 2 coordinates after splitting
            if (coords.Length > 1)
            {
                for (int i = coords[0].X; i <= coords[1].X; i++)
                {
                    for (int j = coords[0].Y; j <= coords[1].Y; j++)
                    {
                        Board.BoardState[i, j].SetColor(stone);
                    }
                }
            }
            else
            {
                Board.BoardState[coords[0].X, coords[0].Y].SetColor(stone);
            }

        }
        //essentially the same method as above, but with markup
        public void MarkupBatch(string coord, Markup markup, string text = null)
        {
            var coords = Coordinates.Split(coord);

            //only go into this if we have 2 coordinates after splitting
            if (coords.Length > 1)
            {
                for (int i = coords[0].X; i <= coords[1].X; i++)
                {
                    for (int j = coords[0].Y; j <= coords[1].Y; j++)
                    {
                        Board.BoardState[i, j].SetMarkup(markup, text);
                    }
                }
            }
            //if no coordinates and it's dim, set everything to 0
            else if(markup == Markup.Dim && string.IsNullOrWhiteSpace(coord))
            {
                foreach(Stone s in Board.BoardState)
                {
                    if(s.Annotation == Markup.Dim)
                    {
                        s.SetMarkup(Markup.None);
                    }
                }
            }
            {
                Board.BoardState[coords[0].X, coords[0].Y].SetMarkup(markup, text);

            }

        }

        //text command is set up like 'coordinates:text', somewhat mimicing arrays of points. this takes that split and puts it on the board correctly
        public void SetText(string coordsAndText)
        {
            var coordsText = coordsAndText.Split(':')[0];
            var text = coordsAndText.Split(':')[1];

            var coords = new Coordinates(coordsText[0], coordsText[1]);

            Board.BoardState[coords.X, coords.Y].SetMarkup(Markup.Text, text);


        }

        public string Strip(string rawString)
        {
            rawString = rawString.Replace("[", "");
            rawString = rawString.Replace("]", "");

            return rawString;
        }


    } 
}

