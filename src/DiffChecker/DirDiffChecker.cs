using GitObjects;

namespace DiffChecker
{
    // Stores changes of files in a directory
    class DirDiffChecker
    {
        private Tree prev, curr;
        public List<FileDiff> diffs { get; private set; }

        public DirDiffChecker(Tree prev, Tree curr)
        {
            this.prev = prev;
            this.curr = curr;
            diffs = new List<FileDiff>();
            GetDiff();
        }

        private void GetDiff()
        {
            HashSet<TreeEntry> prevSet = prev.Entries().ToHashSet();
            var prevLookup = prev.Entries().ToDictionary(e => e.name, e => e);
            HashSet<TreeEntry> currSet = curr.Entries().ToHashSet();
            var currLookup = curr.Entries().ToDictionary(e => e.name, e => e);

            HashSet<string> onlyInPrev = prevSet.Except(currSet).Select(e => e.name).ToHashSet();
            HashSet<string> onlyInCurr = currSet.Except(prevSet).Select(e => e.name).ToHashSet();

            foreach (string s in onlyInPrev)
            {
                TreeEntry prevEntry = prevLookup[s];
                if (onlyInCurr.Contains(s))
                {
                    TreeEntry currEntry = currLookup[s];
                    if (prevEntry.mode == Tree.Mode.FILE)
                    {
                        FileDiffChecker fileDiff = new FileDiffChecker(
                            new Blob(prevEntry.hash), new Blob(currEntry.hash));
                        diffs.Add(new FileDiff(DiffType.Change, s, fileDiff));
                    }
                    else
                    {
                        DirDiffChecker dirDiff = new DirDiffChecker(
                            new Tree(prevEntry.hash), new Tree(currEntry.hash));
                        foreach (FileDiff diffEntry in dirDiff.diffs)
                        {
                            string path = Path.Join(s, diffEntry.name);
                            diffs.Add(new FileDiff(diffEntry.diffType, path, diffEntry.checker));
                        }
                    }
                    onlyInCurr.Remove(s);
                }
                else
                {
                    FileDiffChecker fileDiff = new FileDiffChecker(
                            new Blob(prevEntry.hash), DiffType.Deletion);
                    diffs.Add(new FileDiff(DiffType.Deletion, s, fileDiff));
                }
            }

            foreach (string s in onlyInCurr)
            {
                FileDiffChecker fileDiff = new FileDiffChecker(
                            new Blob(currLookup[s].hash), DiffType.Creation);
                diffs.Add(new FileDiff(DiffType.Creation, s, fileDiff));
            }
        }
    }
}