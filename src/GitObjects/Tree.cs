using System.Dynamic;
using System.Text;
using System.Collections.Generic;

namespace GitObjects
{
    class Tree : GitObject
    {
        public static class Mode
        {
            public const string DIR = "40000";
            public const string FILE = "100644";
        }
        
        private List<TreeEntry> entries = new List<TreeEntry>();

        public Tree(string hash) : base(hash)
        {
            ParseTree();
        }

        public Tree(byte[] header, byte[] content) : base(header, content)
        {
            ParseTree();
        }

        public Tree(GitObject go) : base(go)
        {
            ParseTree();
        }

        // Tree format: tree <size>\0<mode> <name>\0<20_byte_sha><mode> <name>\0<20_byte_sha>
        // Mode: 40000 for directories, 100644 for text files
        private void ParseTree()
        {
            if (header.type != ObjectType.TREE) throw new InvalidOperationException($"Not a tree: {hash}");

            byte[] entries = content;
            while (entries.Length > 0)
            {
                // Get until next null byte
                int split = Array.IndexOf(entries, (byte)0);
                string[] modeName = Encoding.UTF8.GetString(entries[..split]).Split();
                entries = entries[(split + 1)..]; // +1 to skip the null byte

                // Get hash
                string hash = Convert.ToHexStringLower(entries[..20]);
                entries = entries[20..];

                // Add to list
                this.entries.Add(new TreeEntry(modeName[0], modeName[1], hash));
            }
        }

        // Create a tree object from a directory
        public static Tree FromDirectory(string directory)
        {
            int skip = directory.Length + (directory.EndsWith("/") ? 0 : 1);
            Dictionary<string, byte[]> tree = new Dictionary<string, byte[]>();
            int treeSize = 0;

            foreach (string dir in Directory.EnumerateDirectories(directory))
            {
                if (dir.Split('/').LastOrDefault("") != ".git") // Skip the .git/ folder
                {
                    byte[] entry = [.. Encoding.UTF8.GetBytes($"{Mode.DIR} {dir[skip..]}\0"),
                                    .. Convert.FromHexString(FromDirectory(dir).hash)];
                    tree[dir[skip..]] = entry;
                    treeSize += entry.Length;
                }
            }
            foreach (string file in Directory.EnumerateFiles(directory))
            {
                byte[] entry = [.. Encoding.UTF8.GetBytes($"{Mode.FILE} {file[skip..]}\0"),
                                .. Convert.FromHexString(Blob.FromFile(file).hash)];
                tree[file[skip..]] = entry;
                treeSize += entry.Length;
            }

            byte[] header = Encoding.UTF8.GetBytes($"tree {treeSize}\0");
            byte[] content = new byte[treeSize];
            // Sort alphabetically
            int startIx = 0;
            foreach (string entryKey in tree.Keys.OrderBy(k => k))
            {
                byte[] entryBytes = tree[entryKey];
                Array.Copy(entryBytes, 0, content, startIx, entryBytes.Length);
                startIx += entryBytes.Length;
            }

            return new Tree(header, content);
        }

        // Returns the difference going from this Tree to other Tree, so a file
        // in other but not this will be Creating a file and a file in this but
        // not other will be Deleting a file
        public List<DiffEntry> GetDiff(Tree other)
        {
            List<DiffEntry> diff = new List<DiffEntry>();

            HashSet<TreeEntry> thisSet = [.. entries];
            var thisLookup = entries.ToDictionary(e => e.name, e => e);
            HashSet<TreeEntry> otherSet = [.. other.entries];
            var otherLookup = other.entries.ToDictionary(e => e.name, e => e);

            HashSet<string> inThis = thisSet.Except(otherSet).Select(e => e.name).ToHashSet();
            HashSet<string> inOther = otherSet.Except(thisSet).Select(e => e.name).ToHashSet();

            foreach (string s in inThis)
            {
                if (inOther.Contains(s))
                {
                    TreeEntry thisEntry = thisLookup[s];
                    if (thisEntry.mode == Mode.FILE)
                    {
                        diff.Add(new DiffEntry(s, DiffEntry.DiffType.Change));
                    }
                    else
                    {
                        TreeEntry otherEntry = otherLookup[s];
                        var dirDiff = new Tree(thisEntry.hash).GetDiff(new Tree(otherEntry.hash));
                        foreach (DiffEntry diffEntry in dirDiff)
                        {
                            string path = Path.Join(s, diffEntry.name);
                            diff.Add(new DiffEntry(path, diffEntry.diffType));
                        }
                    }
                    inOther.Remove(s);
                }
                else
                {
                    diff.Add(new DiffEntry(s, DiffEntry.DiffType.Deletion));
                }
            }

            foreach (string s in inOther)
            {
                diff.Add(new DiffEntry(s, DiffEntry.DiffType.Creation));
            }
            

            return diff;
        }

        public override void Write(string path = "")
        {
            foreach (TreeEntry entry in entries)
            {
                string filepath = Path.Join(path, entry.name);
                if (entry.mode == Mode.DIR) FromDirectory(filepath).Write(filepath);
                else Blob.FromFile(filepath).Write();
            }
            base.Write();
        }

        public IEnumerable<TreeEntry> Entries()
        {
            foreach (TreeEntry entry in entries)
            {
                yield return entry;
            }
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

    struct TreeEntry
    {
        public string mode { get; }
        public string name { get; }
        public string hash { get; }

        public TreeEntry(string mode, string name, string hash)
        {
            this.mode = mode;
            this.name = name;
            this.hash = hash;
        }

        public override int GetHashCode()
        {
            return Convert.ToInt32(hash[..8], 16);
        }
    }
}