using System.Text;

namespace GitObjects
{
    class Tree : GitObject
    {
        private int size;
        private List<TreeEntry> entries = new List<TreeEntry>();

        public Tree(string hash) : base(hash)
        {
            ParseTree();
        }

        public Tree(byte[] content) : base(content)
        {
            ParseTree();
        }

        // Tree format: tree <size>\0<mode> <name>\0<20_byte_sha><mode> <name>\0<20_byte_sha>
        private void ParseTree()
        {
            int split = Array.IndexOf(content, (byte)0);
            string[] treeSize = Encoding.UTF8.GetString(content[..split]).Split();
            if (treeSize[0] != "tree") throw new InvalidOperationException($"Not a tree: {hash}");
            // Get the size
            int tmp;
            if (!int.TryParse(treeSize[1], out tmp)) throw new InvalidFormatException($"Invalid size {treeSize[1]} for {hash}");
            size = tmp;

            byte[] entries = content[(split+1)..];
            while (entries.Length > 0)
            {
                // Get until next null byte
                split = Array.IndexOf(entries, (byte)0);
                string[] modeName = Encoding.UTF8.GetString(entries[..split]).Split();
                entries = entries[(split + 1)..]; // +1 to skip the null byte

                // Get hash
                string hash = Convert.ToHexStringLower(entries[..20]);
                entries = entries[20..];

                // Add to list
                this.entries.Add(new TreeEntry(modeName[0], modeName[1], hash));
            }
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