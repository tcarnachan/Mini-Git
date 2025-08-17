namespace DiffChecker
{
    class DiffPrinter
    {
        private static ConsoleColor defaultColour;
        private const int LINE_NO_WIDTH = 4;

        private Dictionary<DiffType, char> tags = new Dictionary<DiffType, char>()
        {
            { DiffType.Change, ' ' },
            { DiffType.Unchanged, ' ' },
            { DiffType.Creation, '+' },
            { DiffType.Deletion, '-' }
        };

        private Dictionary<DiffType, ConsoleColor> colours = new Dictionary<DiffType, ConsoleColor>()
        {
            { DiffType.Change, defaultColour },
            { DiffType.Unchanged, defaultColour },
            { DiffType.Creation, ConsoleColor.Green },
            { DiffType.Deletion, ConsoleColor.Red }
        };

        // Message next to filename when printing summary
        private Dictionary<DiffType, string> msg = new Dictionary<DiffType, string>()
        {
            { DiffType.Change, "" },
            { DiffType.Creation, " (new)" },
            { DiffType.Deletion, " (gone)" }
        };

        public DiffPrinter()
        {
            defaultColour = Console.ForegroundColor;
        }

        // Print the names of each changed file
        public void PrintNames(DirDiffChecker dirDiffs)
        {
            foreach (FileDiff diff in dirDiffs.diffs)
            {
                Console.ForegroundColor = colours[diff.diffType];
                Console.WriteLine(diff.name);
            }
            Console.ResetColor();
        }

        // Print summary as: <name> | <lines changed> <+ for each addition> <- for each deletion>
        // e.g. main.py | 3 ++-
        public void PrintSummary(DirDiffChecker dirDiffs)
        {
            int namePadding = dirDiffs.diffs.Max(fd =>
                fd.name.Length + fd.diffType switch
                {
                    DiffType.Deletion => 7, // <name> (gone)
                    DiffType.Creation => 6, // <name> (new)
                    _ => 0
                });
            foreach (FileDiff diff in dirDiffs.diffs)
            {
                (int ins, int del) = diff.checker.GetSummary();
                string name = diff.name + msg[diff.diffType];
                Console.Write(name.PadRight(namePadding) +
                                " |" + $"{ins + del}".PadLeft(3) + " ");
                Console.ForegroundColor = colours[DiffType.Creation];
                Console.Write(new string(tags[DiffType.Creation], ins));
                Console.ForegroundColor = colours[DiffType.Deletion];
                Console.WriteLine(new string(tags[DiffType.Deletion], del));
                Console.ResetColor();
            }
        }

        // Print the changed lines in a file, including a
        // buffer line before and after
        public void _PrintFile(FileDiff fileDiff)
        {
            Console.WriteLine($"\x1b[1m{fileDiff.name}\x1b[0m");
            var diffs = fileDiff.checker.diffs;
            LineDiff prevLine = diffs[0];
            if (prevLine.diffType != DiffType.Unchanged)
            {
                PrintLine(prevLine);
            }
            for (int i = 1; i < diffs.Count; i++)
            {
                LineDiff currLine = diffs[i];
                // This line is changed
                if (currLine.diffType != DiffType.Unchanged)
                {
                    // Buffer line before
                    if (prevLine.diffType == DiffType.Unchanged)
                    {
                        PrintLine(prevLine);
                    }
                    // Current line
                    PrintLine(currLine);
                }
                // Previous line was changed (this line is buffer after)
                else if (prevLine.diffType != DiffType.Unchanged)
                {
                    PrintLine(currLine);
                }
                prevLine = currLine;
            }
        }

        public void PrintFile(FileDiff fileDiff)
        {
            Console.WriteLine($"\x1b[1m{fileDiff.name}\x1b[0m");
            List<LineDiff> diffs = fileDiff.checker.diffs;
            var changedLines = Enumerable.Range(0, diffs.Count)
                    .Where(i => diffs[i].diffType != DiffType.Unchanged);

            HashSet<int> toPrint = new HashSet<int>();
            foreach (int i in changedLines)
            {
                for (int delta = -3; delta <= 3; delta++)
                {
                    if (i + delta >= 0 && i + delta < diffs.Count)
                        toPrint.Add(i + delta);
                }
            }

            foreach (int i in toPrint.Order())
            {
                PrintLine(diffs[i]);
            }
        }

        private void PrintLine(LineDiff line)
        {
            DiffType type = line.diffType;
            Console.ForegroundColor = colours[type];
            string oldLineNo = (line.oldLineNo == -1 ? "" :
                line.oldLineNo.ToString()).PadRight(LINE_NO_WIDTH);
            string newLineNo = (line.newLineNo == -1 ? "" :
                line.newLineNo.ToString()).PadRight(LINE_NO_WIDTH);
            string text = line.oldLine == "" ? line.newLine : line.oldLine;
            Console.WriteLine($"{tags[type]} {oldLineNo} {newLineNo}    {text}");
            Console.ResetColor();
        }
    }
}