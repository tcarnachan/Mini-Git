using System.Text;

namespace GitObjects
{
    class Commit : GitObject
    {
        // private static string headPath = ".git/HEAD";

        public Commit(string hash) : base(hash)
        {
            ParseCommit();
        }

        /*
            Commit format:
            commit <size>\0tree <hash>
            [parent <hash>]
            author <author> <unix time> <time offset>
            committer <committer> <unix time> <time offset>

            <commit message>
        */
        private void ParseCommit()
        {
            if (header.type != ObjectType.COMMIT) throw new InvalidOperationException($"Not a commit: {hash}");

            
        }

        // public static Commit FromCurrent()
        // {
        //     string filepath = File.ReadAllText(headPath).Trim();
        //     string expectedStart = "ref: ";
        //     string parentPath = filepath.StartsWith(expectedStart) ?
        //             ".git/" + filepath[expectedStart.Length..] : "";
        //     string parent = File.ReadAllText(parentPath).Trim();

        //     Tree tree = Tree.FromDirectory(Directory.GetCurrentDirectory());

        //     long time = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        //     TimeSpan delta = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        //     Console.WriteLine($"{(delta.TotalMinutes >= 0 ? '+' : '-')}{delta.Hours:D2}{delta.Minutes:D2}");

        //     string res = $"tree {tree.hash}\0"

        //     return new Commit();
        // }
    }
}