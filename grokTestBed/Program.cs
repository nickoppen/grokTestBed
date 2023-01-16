using GrokNet;
using Microsoft.VisualBasic;
using System.Collections;
using System.Diagnostics;
using System.Globalization;

class Program
{
    private struct patternName
    {
        public string pattern;
        public string name;
        public int index;
    }
    private struct cliArgs
    {
        public string customPatternFile;
        public string patternFile;
        public string dataFile;
        public string outputFile;
        public int verbosity;
    }

    private cliArgs readCliArgs(string[] strArgs)
    {
        cliArgs result = new cliArgs
        {
            customPatternFile = @"./grokCustom.txt",
            patternFile= @"./matchPattern.txt",
            dataFile= @"./grokTest.txt",
            outputFile = @"./outputFile.txt",
            verbosity = 2
        };

        for (int i = 0; i < strArgs.Length; i++)
        {
            switch (strArgs[i])
            {
                case "-p":
                    result.customPatternFile = strArgs[++i];
                    break;
                case "-f":
                    result.dataFile = strArgs[++i];
                    break;
                case "-c":
                    result.customPatternFile += strArgs[++i];
                    break;
                case "-o":
                    result.outputFile += strArgs[++i];
                    break;
                case "-v0":
                    result.verbosity = 0;
                    break;
                case "-v1":
                    result.verbosity = 1;
                    break;
                case "-v2":
                    result.verbosity = 2;
                    break;
                default:
                    Console.WriteLine("-f <dataFile> (default: ./grokTest)\n-p <top level pattern file> (default: ./matchPattern.txt)\n-c <custom pattern file> (default: ./grokCustom.txt)\n-o <output file> (default: ./outputFile.txt)\n-vx : Verbosity (0 <= x <= 2)");
                    Environment.Exit(0);
                    break;
            }
        }
        return result;
    }
    private ArrayList splitPatternIntoElements(string grokPattern)
    {
        int i = 0;
        ArrayList result = new ArrayList();
        string[] patternColonNames = grokPattern.Split('%');        /// Problem: \% could be part of the string
        foreach (string patternColonName in patternColonNames)
        {
            string[] patternsAndNames = patternColonName.Split(":");    /// Ditto: \:
            if (patternsAndNames.Length >= 2 )
                result.Add(new patternName { pattern = patternsAndNames[0].TrimStart('{'), name = patternsAndNames[1].TrimEnd(' ','}'), index = i++ });
        }

        return result;
    }
    private int readLogFileToDatabase(FileInfo fileInformation, StreamWriter errorLogWriter, cliArgs args, Program p)
    {
        int insertCount = 0;
        var parseTime = new Stopwatch();

        if (File.Exists(args.customPatternFile) && File.Exists(args.patternFile))
        {
            var grokPattern = File.ReadAllText(args.patternFile);
            var grokPatternElements = p.splitPatternIntoElements(grokPattern);
            var customPatterns = System.IO.File.OpenRead(args.customPatternFile);
            
            StreamReader logReader = fileInformation.OpenText();
            var grkLogLine = new Grok(grokPattern, customPatterns); //e.g. "%{LOGTIME:Timestamp} %{LOGPROG:Prog}: %{LOGACTION:Action} %{LOGDOMAIN:Domain} %{LOGDIRECTION:Direction} %{LOGEOL:EndOfLine}"
                                           //                                           0                 1                   2                   3                       4                   5                  
            GrokResult grkLogRes;
            string logLine;

            while ((logLine = logReader.ReadLine()) != null)
            {
                parseTime.Start();
                grkLogRes = grkLogLine.Parse(logLine, 10000000);    // 10000000 is 1 sec in ticks
                parseTime.Stop();

                if (grkLogRes.Any())
                {
                    if (args.verbosity >= 2)
                    {
                        Console.WriteLine("Parse time:" + parseTime.Elapsed.ToString() + " --- " + logLine);
                        foreach (patternName element in grokPatternElements)
                        {
                            Console.WriteLine(element.name + ":\t" + grkLogRes[element.index].Value.ToString());
                        }
                    }
                    errorLogWriter.WriteLine("Parse time:" + parseTime.Elapsed.ToString() + " --- " + logLine);
                    foreach (patternName element in grokPatternElements)
                    {
                        errorLogWriter.WriteLine(element.name + ":\t" + grkLogRes[element.index].Value.ToString());
                    }
                    errorLogWriter.WriteLine();
                }
                else
                {
                    errorLogWriter.WriteLine("No Match Error:" + logLine);
                    if (args.verbosity >= 1)
                    {
                        Console.WriteLine("No Match Error:" + logLine);
                        Console.WriteLine();
                    }
                }
                parseTime.Reset();
                insertCount++;
            }
            logReader.Close();
        }
        else
        {
            Console.WriteLine("File: " + args.customPatternFile + " and/or file: " + args.patternFile + " does not exist");
            errorLogWriter.Close();
            Environment.Exit(-1);
        }

        return insertCount;
    }

    static void Main(string[] strArgs)
    {
        var p = new Program();
        var args = p.readCliArgs(strArgs);

        int insertCount = 0;

        if (File.Exists(args.dataFile))
        {
            using (var errorLogWriter = new StreamWriter(args.outputFile, false))
            {
                FileInfo fileInformation = new FileInfo(args.dataFile);
                insertCount = p.readLogFileToDatabase(fileInformation, errorLogWriter, args, p);
            }
        }
        Console.WriteLine("lines: " + insertCount.ToString());

        Environment.Exit(0);
    }
}
