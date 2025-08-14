using System.Text;

namespace GitObjects
{
    class Tree : GitObject
    {
        private int size;
        private List<TreeEntry> entries = new List<TreeEntry>();
        private List<Tree> subTrees = new List<Tree>();

        public Tree(string hash) : base(hash)
        {
            ParseTree();
        }

        public Tree(byte[] content, List<Tree> subTrees) : base(content)
        {
            ParseTree();
            this.subTrees = subTrees;
        }

        // Tree format: tree <size>\0<mode> <name>\0<20_byte_sha><mode> <name>\0<20_byte_sha>
        // Mode: 40000 for directories, 100644 for text files
        private void ParseTree()
        {
            int split = Array.IndexOf(content, (byte)0);
            string[] treeSize = Encoding.UTF8.GetString(content[..split]).Split();
            if (treeSize[0] != "tree") throw new InvalidOperationException($"Not a tree: {hash}");
            // Get the size
            int tmp;
            if (!int.TryParse(treeSize[1], out tmp)) throw new InvalidFormatException($"Invalid size {treeSize[1]} for {hash}");
            size = tmp;

            byte[] entries = content[(split + 1)..];
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

        // Create a tree object from a directory
        public static Tree FromDirectory(string directory)
        {
            List<Tree> subTrees = new List<Tree>();
            int skip = directory.Length + 1;
            // StringBuilder tree = new();
            List<byte> tree = new List<byte>();

            foreach (string dir in Directory.EnumerateDirectories(directory))
            {
                if (dir.Split('/').LastOrDefault("") != ".git") // Skip the .git/ folder
                {
                    Tree subTree = FromDirectory(dir);
                    subTrees.Add(subTree);
                    tree.AddRange(Encoding.UTF8.GetBytes($"40000 {dir[skip..]}\0"));
                    tree.AddRange(Convert.FromHexString(subTree.hash));
                    // tree.AppendFormat("40000 {0}\0{1}", dir[skip..], Convert.FromHexString(subTree.hash));
                }
            }
            foreach (string file in Directory.EnumerateFiles(directory))
            {
                tree.AddRange(Encoding.UTF8.GetBytes($"100644 {file[skip..]}\0"));
                tree.AddRange(Convert.FromHexString(Blob.FromFile(file).hash));
                // tree.AppendFormat("100644 {0}\0{1}", file[skip..], Encoding.UTF8.GetBytes(Blob.FromFile(file).hash));
            }

            // byte[] content = Encoding.UTF8.GetBytes(tree.ToString());
            // byte[] header = Encoding.UTF8.GetBytes($"tree {content.Length}\0");
            byte[] header = Encoding.UTF8.GetBytes($"tree {tree.Count}\0");

            return new Tree([.. header, .. tree.ToArray()], subTrees);
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