
A test system to develop grok pattern matching strings (note: this has nothing to do with grok the educational tool).

There is no interface. The pattern string and custom patterns are read from a file and matched against a data file controlled by the switches. Output is to the console (governed by the -v switch) and an output file.

This systems uses the [Grok.Net](https://github.com/Marusyk/grok.net) library by Roman Marusyk. I've made one change to ensure that the regex call returns in a reasonable amount of time. Until this change is incorporated into the main branch you need to download Grok.Net and replace the parse(sting) method with:

```
public GrokResult Parse(string text, long ticks = -1)
{
    if (_compiledRegex == null)
    {
        ParseGrokString();
    }

    var grokItems = new List<GrokItem>();


    Thread worker = new Thread(() => { var matches = _compiledRegex.Matches(text);
                                        foreach (Match match in matches)
                                        {
                                            foreach (string groupName in _groupNames)
                                            {
                                                if (groupName != "0")
                                                {
                                                    grokItems.Add(_typeMaps.ContainsKey(groupName)
                                                        ? new GrokItem(groupName, MapType(_typeMaps[groupName], match.Groups[groupName].Value))
                                                        : new GrokItem(groupName, match.Groups[groupName].Value));
                                                }
                                            }
                                        }
                                    });
    worker.Start();
    worker.Join(new TimeSpan((ticks == -1) ? 864000000000 : ticks));

    return new GrokResult(grokItems);
}
```
If you use the ticks argument the execution time will be limited to TimeSpan(ticks) (one second is 10000000 ticks).

Switches:

-f \<dataFile\> (default: ./grokTest)

-p \<top level pattern file\> (default: ./matchPattern.txt)

-c \<custom pattern file\> (default: ./grokCustom.txt)

-o \<output file\> (default: ./outputFile.txt)

-t \<maximum execution time in ticks\> (default: 10000000 (i.e. one second))


Verbosity to the Console can be controlled using the -vx switch:

-v0: output goes to the output file only

-v1: all output goes to the output file and only errors to the console

-v2: all output goes to both output file and console

Warning: If there are \\{ \\} \\: or \\% characters in the top level pattern this will screw up the output.

Note: there is no error checking on the -d and -f switches