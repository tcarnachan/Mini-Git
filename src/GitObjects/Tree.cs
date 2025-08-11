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
            string[] split = GetString().Split('\0');
            if (split.Length > 1)
            {
                // TODO: mode + hash
                entries.Add(new TreeEntry("", split[1].Split()[1], ""));
                for (int i = 2; i < split.Length; i++)
                {
                    string[] remain = string.Join("", split[i].Skip(20).ToArray()).Split(); // remain[0] is mode, remain[1] is name
                    if (remain.Length > 1) entries.Add(new TreeEntry("", remain[1], ""));
                }
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