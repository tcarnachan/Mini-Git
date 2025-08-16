namespace DiffChecker
{
    class DiffPrinter
    {
        private static ConsoleColor defaultColour;

        private Dictionary<DiffType, char> tags = new Dictionary<DiffType, char>()
        {
            { DiffType.Change, ' ' },
            { DiffType.Creation, '+' },
            { DiffType.Deletion, '-' }
        };

        private Dictionary<DiffType, ConsoleColor> colours = new Dictionary<DiffType, ConsoleColor>()
        {
            { DiffType.Change, defaultColour },
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

        public void PrintNames(DirDiffChecker dirDiffs)
        {
            foreach (FileDiff diff in dirDiffs.diffs)
            {
                Console.ForegroundColor = colours[diff.diffType];
                Console.WriteLine(diff.name);
            }
            Console.ResetColor();
        }

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
                                " |" + $"{ins + del}".PadLeft(3));
                Console.ForegroundColor = colours[DiffType.Creation];
                Console.Write(new string(tags[DiffType.Creation], ins));
                Console.ForegroundColor = colours[DiffType.Deletion];
                Console.WriteLine(new string(tags[DiffType.Deletion], del));
                Console.ResetColor();
            }
        }
    }
}