using GitObjects;

class DiffChecker
{
    // Returns the difference going from prev to curr
    public List<DiffEntry> GetDiff(Tree prev, Tree curr)
    {
        List<DiffEntry> diff = new List<DiffEntry>();

        HashSet<TreeEntry> prevSet = prev.Entries().ToHashSet();
        var prevLookup = prev.Entries().ToDictionary(e => e.name, e => e);
        HashSet<TreeEntry> currSet = curr.Entries().ToHashSet();
        var currLookup = curr.Entries().ToDictionary(e => e.name, e => e);

        HashSet<string> onlyInPrev = prevSet.Except(currSet).Select(e => e.name).ToHashSet();
        HashSet<string> onlyInCurr = currSet.Except(prevSet).Select(e => e.name).ToHashSet();

        foreach (string s in onlyInPrev)
        {
            if (onlyInCurr.Contains(s))
            {
                TreeEntry thisEntry = prevLookup[s];
                if (thisEntry.mode == Tree.Mode.FILE)
                {
                    diff.Add(new DiffEntry(s, DiffEntry.DiffType.Change));
                }
                else
                {
                    TreeEntry otherEntry = currLookup[s];
                    List<DiffEntry> dirDiff = GetDiff(
                        new Tree(thisEntry.hash), new Tree(otherEntry.hash));
                    foreach (DiffEntry diffEntry in dirDiff)
                    {
                        string path = Path.Join(s, diffEntry.name);
                        diff.Add(new DiffEntry(path, diffEntry.diffType));
                    }
                }
                onlyInCurr.Remove(s);
            }
            else
            {
                diff.Add(new DiffEntry(s, DiffEntry.DiffType.Deletion));
            }
        }

        foreach (string s in onlyInCurr)
        {
            diff.Add(new DiffEntry(s, DiffEntry.DiffType.Creation));
        }


        return diff;
    }
}

public struct DiffEntry
{
    public enum DiffType { Creation, Deletion, Change };

    public string name;
    public DiffType diffType;

    public DiffEntry(string name, DiffType diffType)
    {
        this.name = name;
        this.diffType = diffType;
    }
}