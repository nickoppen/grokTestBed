using GrokNet;
using System.Diagnostics;
using System.Globalization;

class Program
{
    private int readLogFileToDatabase(FileInfo fileInformation, StreamWriter errorLogWriter, Program p)
    {
        int insertCount = 0;

        string customPatternFile = @"./grokCustom.txt";
        if (File.Exists(customPatternFile))
        {
            using (FileStream customPatterns = System.IO.File.OpenRead(customPatternFile))
            {
                StreamReader logReader = fileInformation.OpenText();
                var grkLogLine = new Grok("%{LOGTIME:Timestamp} %{LOGPROG:Prog}: %{LOGACTION:Action} %{LOGDOMAIN:Domain} %{LOGDIRECTION:Direction} %{LOGEOL:EndOfLine}", customPatterns);
                //                                      0                 1                   2                   3                       4                   5                  
                GrokResult grkLogRes;
                string timeOfCall, action, domain, direction, endOfLine;
                int isClauseLength, toPos;
                string logLine;

                while ((logLine = logReader.ReadLine()) != null)
                {
                    grkLogRes = grkLogLine.Parse(logLine);
                    if (grkLogRes.Any())
                    {
                        timeOfCall = grkLogRes[0].Value.ToString();
                        Console.WriteLine("TimeOfCall:\t" + timeOfCall);
                        action = grkLogRes[2].Value.ToString();
                        Console.WriteLine("Action:\t\t" + action );
                        domain = grkLogRes[3].Value.ToString();
                        Console.WriteLine("Domain:\t\t" + domain);
                        direction = grkLogRes[4].Value.ToString();
                        Console.WriteLine("Direction:\t" + direction);
                        endOfLine = grkLogRes[5].Value.ToString();
                        Console.WriteLine("End Of Line:\t" + direction);
                        Console.WriteLine();
                    }
                    else
                        errorLogWriter.WriteLine("No Match Error:" + logLine);
                }
                logReader.Close();
            }
        }
        else
        {
            errorLogWriter.WriteLine("File: " + customPatternFile + " does not exist");
            errorLogWriter.Close();
            Environment.Exit(-1);
        }

        return insertCount;
    }

    static void Main(string[] strArgs)
    {
        var p = new Program();

        int insertCount;
        TimeSpan insertTime = TimeSpan.Zero;
        var runTimer = new Stopwatch();

        if (File.Exists(strArgs[0]))
        {
            using (StreamWriter errorLogWriter = new StreamWriter("./errorLog.txt", false))
            {
                FileInfo fileInformation = new FileInfo(strArgs[0]);
                insertCount = p.readLogFileToDatabase(fileInformation, errorLogWriter, p);
            }
        }


        Environment.Exit(0);
    }
}
