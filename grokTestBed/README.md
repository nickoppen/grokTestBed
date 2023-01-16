
Switches now control input files and the output destination:

-f <dataFile> (default: ./grokTest)
-p <top level pattern file> (default: ./matchPattern.txt)
-c <custom pattern file> (default: ./grokCustom.txt)
-o <output file> (default: ./outputFile.txt)

Verbosity to the Console can be controlled using the -vx switch:
-v0: output goes to the output file only
-v1: all output goes to the output file and only errors to the console
-v2: all output goes to both output file and console

Warning: If there are \{ \} \: or \% characters in the top level pattern this will screw up the output.