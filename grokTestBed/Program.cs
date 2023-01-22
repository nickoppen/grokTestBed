using GrokNet;
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

class Program
{
    private struct cliArgs
    {
        public string customPatternFile;
        public string patternFile;
        public string dataFile;
        public string outputFile;
        public long maxExecutionTimeInTicks;
        public int verbosity;
    }

    private cliArgs readCliArgs(string[] strArgs)
    {
        var result = new cliArgs
        {
            customPatternFile = @"./grokCustom.txt",
            patternFile= @"./matchPattern.txt",
            dataFile= @"./grokTest.txt",
            outputFile = @"./outputFile.txt",
            maxExecutionTimeInTicks = 10000000,     // one second
            verbosity = 2
        };

        for (int i = 0; i < strArgs.Length; i++)
        {
            switch (strArgs[i])
            {
                case "-p":
                    result.patternFile = strArgs[++i];
                    break;
                case "-f":
                    result.dataFile = strArgs[++i];
                    break;
                case "-c":
                    result.customPatternFile = strArgs[++i];
                    break;
                case "-o":
                    result.outputFile = strArgs[++i];
                    break;
                case "-t":
                    result.maxExecutionTimeInTicks = long.Parse(strArgs[++i]);
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
    private Dictionary<string, int> splitPatternIntoElements(string grokPattern)
    {
        int i = 0;
        var result = new Dictionary<string, int>();
        string[] patternColonNames = grokPattern.Split('%');        /// Problem: \% could be part of the string
        foreach (string patternColonName in patternColonNames)
        {
            string[] patternsAndNames = patternColonName.Split(":");    /// Ditto: \:
            if (patternsAndNames.Length >= 2)
            {
                int closePos = patternsAndNames[1].IndexOf('}');
                string name = patternsAndNames[1].Remove(closePos);
                if (!result.ContainsKey(name))
                    result.Add(name, i++);
            }
        }

        foreach (var key in result.Keys)
        {
            Console.WriteLine("Key:"+ key + " is at " + result[key]);
        }

        return result;
    }
    
    private void reportToConsole(string message, GrokResult grkLogRes, Dictionary<string, int> grokPatternElements)
    {
        Console.WriteLine(message);
        foreach (string elementName in grokPatternElements.Keys)
        {
            Console.WriteLine(elementName + ":\t" + grkLogRes[grokPatternElements[elementName]].Value.ToString());
        }
        Console.WriteLine();

    }

    private void reportToErrorLog(string message, StreamWriter errorLogWriter, GrokResult grkLogRes, Dictionary<string, int> grokPatternElements)
    {
        errorLogWriter.WriteLine(message);
        foreach (string elementName in grokPatternElements.Keys)
        {
            errorLogWriter.WriteLine(elementName + ":\t" + grkLogRes[grokPatternElements[elementName]].Value.ToString());
        }
        errorLogWriter.WriteLine();

    }
    
    private int readLogFile(FileInfo fileInformation, StreamWriter errorLogWriter, cliArgs args, Program p)
    {
        int insertCount = 0;
        var parseTime = new Stopwatch();

        if (File.Exists(args.customPatternFile) && File.Exists(args.patternFile))
        {
            var grokPattern = File.ReadAllText(args.patternFile);
            var grokPatternElements = p.splitPatternIntoElements(grokPattern);
            var customPatterns = System.IO.File.OpenRead(args.customPatternFile);
            
            StreamReader logReader = fileInformation.OpenText();
            var grkLogLine = new Grok(grokPattern, customPatterns); 

            GrokResult grkLogRes;
            string logLine;

            while ((logLine = logReader.ReadLine()) != null)
            {
                parseTime.Start();
                grkLogRes = grkLogLine.Parse(logLine, args.maxExecutionTimeInTicks);    
                parseTime.Stop();

                if (grkLogRes.Any())
                {
                    string message = "Parse time:" + parseTime.Elapsed.ToString() + " --- " + logLine;
                    if (args.verbosity >= 2)
                    {
                        reportToConsole(message, grkLogRes, grokPatternElements);
                    }
                    reportToErrorLog(message, errorLogWriter, grkLogRes, grokPatternElements);
                }
                else
                {
                    string reasonForError = "";
                    if (parseTime.Elapsed > new TimeSpan(args.maxExecutionTimeInTicks))
                        reasonForError = "Timeout Error:\t";
                    else
                        reasonForError = "No Match Error:\t";

                    errorLogWriter.WriteLine(reasonForError + logLine);
                    errorLogWriter.WriteLine();
                    if (args.verbosity >= 1)
                    {
                        Console.WriteLine(reasonForError + logLine);
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
                var fileInformation = new FileInfo(args.dataFile);
                insertCount = p.readLogFile(fileInformation, errorLogWriter, args, p);
            }
        }
        Console.WriteLine("lines: " + insertCount.ToString());

        Environment.Exit(0);
    }
}
