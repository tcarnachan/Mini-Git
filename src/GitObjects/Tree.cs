using System.Text;

namespace GitObjects
{
    class Tree : GitObject
    {
        private List<TreeEntry> entries = new List<TreeEntry>();
        private List<Tree> subTrees = new List<Tree>();

        public Tree(string hash) : base(hash)
        {
            ParseTree();
        }

        public Tree(byte[] header, byte[] content, List<Tree> subTrees) : base(header, content)
        {
            ParseTree();
            this.subTrees = subTrees;
        }

        // Tree format: tree <size>\0<mode> <name>\0<20_byte_sha><mode> <name>\0<20_byte_sha>
        // Mode: 40000 for directories, 100644 for text files
        private void ParseTree()
        {
            if (header.type != "tree") throw new InvalidOperationException($"Not a tree: {hash}");

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
            List<Tree> subTrees = new List<Tree>();
            int skip = directory.Length + 1;
            Dictionary<string, byte[]> tree = new Dictionary<string, byte[]>();
            int treeSize = 0;

            foreach (string dir in Directory.EnumerateDirectories(directory))
            {
                if (dir.Split('/').LastOrDefault("") != ".git") // Skip the .git/ folder
                {
                    Tree subTree = FromDirectory(dir);
                    subTrees.Add(subTree);
                    byte[] entry = [.. Encoding.UTF8.GetBytes($"40000 {dir[skip..]}\0"),
                                    .. Convert.FromHexString(subTree.hash)];
                    tree[dir[skip..]] = entry;
                    treeSize += entry.Length;
                }
            }
            foreach (string file in Directory.EnumerateFiles(directory))
            {
                byte[] entry = [.. Encoding.UTF8.GetBytes($"100644 {file[skip..]}\0"),
                                .. Convert.FromHexString(Blob.FromFile(file).hash)];
                tree[file[skip..]] = entry;
                treeSize += entry.Length;
            }

            byte[] header = Encoding.UTF8.GetBytes($"tree {treeSize}\0");
            byte[] content = new byte[treeSize];
            int startIx = 0;
            foreach (string entryKey in tree.Keys.OrderBy(k => k))
            {
                byte[] entryBytes = tree[entryKey];
                Array.Copy(entryBytes, 0, content, startIx, entryBytes.Length);
                startIx += entryBytes.Length;
            }

            return new Tree(header, content, subTrees);
        }

        public override void Write()
        {
            foreach (Tree subTree in subTrees)
            {
                subTree.Write();
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

    struct TreeEntry
    {
        public string mode { get; }
        public string name {get; }
        public string hash { get; }

        public TreeEntry(string mode, string name, string hash)
        {
            this.mode = mode;
            this.name = name;
            this.hash = hash;
        }
    }
}