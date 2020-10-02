using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Goban
{
    class Program
    {
        static void Main(string[] args)
        {
            GameRecord rec = new GameRecord();
            int thisBoard = 0;

            while (true)
            {

                for (int i = 0; i < rec.Record[thisBoard].BoardSize; i++)
                {
                    string thisLine = string.Empty;
                    for (int j = 0; j < rec.Record[thisBoard].BoardSize; j++)
                    {
                        switch (rec.Record[thisBoard].BoardState[j, i].Color)
                        {
                            case Color.Black:
                                thisLine += "B ";
                                break;
                            case Color.White:
                                thisLine += "W ";
                                break;
                            case Color.None:
                                thisLine += "+ ";
                                break;
                        }

                    }
                    Console.WriteLine(thisLine);
                }

                var key = Console.ReadKey();

                switch(key.Key)
                {
                    case ConsoleKey.LeftArrow:
                        if(thisBoard != 0)
                        {
                            thisBoard--;
                        }
                        break;
                    case ConsoleKey.RightArrow:
                        if(thisBoard != rec.Record.Count - 1)
                        {
                            thisBoard++;
                        }
                        break;
                    case ConsoleKey.Enter:
                        goto End;
                    default:
                        break;

                }
                Console.Clear();
            }
            End:

            Console.Read();



        }
    }
}
