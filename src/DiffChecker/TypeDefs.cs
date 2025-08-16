namespace DiffChecker
{
    enum DiffType { Creation, Deletion, Change, Unchanged };

    // Stores a file's changes
    struct FileDiff
    {
        public DiffType diffType;
        public string name;
        internal FileDiffChecker checker;

        public FileDiff(DiffType diffType, string name, FileDiffChecker checker)
        {
            this.name = name;
            this.diffType = diffType;
            this.checker = checker;
        }
    }

    // Stores a line's changes
    struct LineDiff
    {
        public DiffType diffType;
        public string oldLine;
        public string newLine;
        public int oldLineNo;
        public int newLineNo;

        public LineDiff(DiffType diffType, string oldLine, string newLine, int oldLineNo, int newLineNo)
        {
            this.diffType = diffType;
            this.oldLine = oldLine;
            this.newLine = newLine;
            this.oldLineNo = oldLineNo;
            this.newLineNo = newLineNo;
        }
    }
}