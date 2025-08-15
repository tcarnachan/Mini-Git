using System.Text;

namespace GitObjects
{
    class Commit : GitObject
    {
        public const string MAIN_PATH = "refs/heads/main";
        private static string mainPath
        {
            get => Path.Join(Directory.GetCurrentDirectory(), ".git/" + MAIN_PATH);
        }

        public Commit(string hash) : base(hash)
        {
            VerifyCommit();
        }

        public Commit(byte[] header, byte[] contents) : base(header, contents)
        {
            VerifyCommit();
        }

        private void VerifyCommit()
        {
            if (header.type != ObjectType.COMMIT) throw new InvalidOperationException($"Not a commit: {hash}");
        }

        /*
            Commit format:
            commit <size>\0tree <hash>
            [parent <hash>]
            author <author> <unix time> <time offset>
            committer <committer> <unix time> <time offset>

            <commit message>
        */
        public static Commit FromCurrent(string message)
        {
            StringBuilder sb = new StringBuilder();

            // tree
            Tree tree = Tree.FromDirectory(Directory.GetCurrentDirectory());
            sb.AppendFormat("tree {0}\n", tree.hash);

            // parent
            if (File.Exists(mainPath))
            {
                sb.AppendFormat("parent {0}\n", File.ReadAllText(mainPath).Trim());
            }

            // author
            long time = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            TimeSpan delta = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
            string timeOffset = $"{(delta.TotalMinutes >= 0 ? '+' : '-')}{delta.Hours:D2}{delta.Minutes:D2}";
            // hard coded for now
            sb.AppendFormat("author Klein Moretti thefool@sefirahcastle.com {0} {1}\n", time, timeOffset);
            sb.AppendFormat("committer Klein Moretti thefool@sefirahcastle.com {0} {1}\n\n", time, timeOffset);

            // commit message
            sb.Append(message.TrimEnd());

            // Get bytes
            byte[] content = Encoding.UTF8.GetBytes(sb.ToString());
            byte[] header = Encoding.UTF8.GetBytes($"commit {content.Length}\0");

            return new Commit(header, content);
        }

        public override void Write()
        {
            base.Write();
            // Update head
            Directory.CreateDirectory(Path.GetDirectoryName(mainPath) ?? "");
            File.WriteAllText(mainPath, hash);
        }
    }
}