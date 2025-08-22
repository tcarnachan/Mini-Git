using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GitObjects
{
    class Commit : GitObject
    {
        // hard coded for now
        private const string AUTHOR = "Klein Moretti <thefool@sefirahcastle.com>";

        public const string MAIN_PATH = "refs/heads/main";
        public static string mainPath
        {
            get => Path.Join(Directory.GetCurrentDirectory(), ".git/" + MAIN_PATH);
        }

        private Tree? _tree;
        public string treeHash { get; private set; }
        public Tree tree
        {
            get
            {
                if (_tree == null) _tree = new Tree(treeHash);
                return _tree;
            }
        }
        private string author, message, timeOffset;
        public string parent { get; private set; }
        private DateTime time;

        public Commit(string hash) : base(hash)
        {
            VerifyCommit();
            PopulateCommit();
        }

        public Commit(byte[] header, byte[] content) : base(header, content)
        {
            VerifyCommit();
            PopulateCommit();
        }

        public Commit(byte[] header, byte[] contents,
            Tree tree, string parent, string author,
            DateTime time, string timeOffset, string message) : base(header, contents)
        {
            VerifyCommit();

            this._tree = tree;
            this.treeHash = tree.hash;
            this.parent = parent;
            this.author = author;
            this.parent = parent;
            this.time = time;
            this.timeOffset = timeOffset;
            this.message = message;
        }

        private void VerifyCommit()
        {
            if (header.type != ObjectType.COMMIT) throw new InvalidOperationException($"Not a commit: {hash}");
        }

        [MemberNotNull("message", "treeHash", "parent", "timeOffset", "time", "author")]
        private void PopulateCommit()
        {
            // Last lines, may contain several lines
            string[] content = GetContentString().Split("\n\n");
            message = content[1];

            // Get tree
            content = content[0].Split('\n');
            treeHash = content[0].Split()[1];

            // Get parent, if it exists
            if (content[1].StartsWith("parent")) parent = content[1].Split()[1];
            else parent = "";

            // committer <author> <unix time seconds> <offset>
            string[] authorLine = content.Last().Split(" ");
            timeOffset = authorLine.Last();
            string date = authorLine[authorLine.Length - 2];
            time = DateTime.UnixEpoch.AddSeconds(long.Parse(date));

            string[] authorInfo = authorLine[1..(authorLine.Length - 2)];
            author = string.Join(" ", authorInfo);
        }

        /*
            Commit format:
            commit <size>\0tree <hash>
            [parent <hash>]
            author <author> <unix time> <time offset>
            committer <committer> <unix time> <time offset>

            <commit message>
        */
        public static bool TryFromCurrent(string message, out Commit commit)
        {
            StringBuilder sb = new StringBuilder();

            // tree
            Tree tree = Tree.FromDirectory(Directory.GetCurrentDirectory());
            sb.AppendFormat("tree {0}\n", tree.hash);

            // parent
            string parentHash = "";
            if (File.Exists(mainPath))
            {
                parentHash = File.ReadAllText(mainPath).Trim();
                Commit parentCommit = new Commit(parentHash);
                if (parentCommit.tree.hash == tree.hash)
                {
                    Console.WriteLine("Nothing to commit");
                    commit = parentCommit;
                    return false;
                }
                sb.AppendFormat("parent {0}\n", parentHash);
            }

            // author
            DateTime now = DateTime.UtcNow;
            long time = ((DateTimeOffset)now).ToUnixTimeSeconds();
            TimeSpan delta = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            string timeOffset = $"{(delta.TotalMinutes >= 0 ? '+' : '-')}{delta.Hours:D2}{delta.Minutes:D2}";
            sb.AppendFormat("author {0} {1} {2}\n", AUTHOR, time, timeOffset);
            sb.AppendFormat("committer {0} {1} {2}\n\n", AUTHOR, time, timeOffset);

            // commit message
            sb.Append(message.TrimEnd());

            // Get bytes
            byte[] content = Encoding.UTF8.GetBytes(sb.ToString());
            byte[] header = Encoding.UTF8.GetBytes($"{ObjectType.COMMIT} {content.Length}\0");

            commit = new Commit(header, content, tree, parentHash,
                AUTHOR, now, timeOffset, message.TrimEnd() + "\n");
            return true;
        }

        public static Commit FromContent(byte[] content)
        {
            byte[] header = Encoding.UTF8.GetBytes($"{ObjectType.COMMIT} {content.Length}\0");
            return new Commit(header, content);
        }

        public static string MainCommitHash()
        {
            if (File.Exists(mainPath))
            {
                return File.ReadAllText(mainPath).Trim();
            }
            return "";
        }

        protected override void WriteSubfiles(string filepath = "")
        {
            // Write the current directory
            tree.Write();
            // Update head
            Directory.CreateDirectory(Path.GetDirectoryName(mainPath) ?? "");
            File.WriteAllText(mainPath, hash);
        }

        public void Print()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"commit {hash}");
            Console.ResetColor();
            Console.WriteLine($"Author: {author}");
            string date = time.ToLongDateString().Replace(",", "");
            Console.WriteLine($"Date: {date} {timeOffset}");
            var msgLines = message.Split('\n').Select(l => "\t" + l);
            Console.WriteLine($"\n{string.Join('\n', msgLines)}\n");
        }
    }
}