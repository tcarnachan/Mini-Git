using GitObjects;
using System.CommandLine;
using System.CommandLine.Parsing;

void ValidateExactlyOne(CommandResult res, params Option[] opts)
{
    if (opts.Count(opt => res.GetResult(opt) is not null) != 1)
    {
        string list = string.Join(", ", opts.Select(o => o.Name));
        res.AddError($"Exactly one of {list} is required");
    }
}

void ValidateAtMostOne(CommandResult res, params Option[] opts)
{
    if (opts.Count(opt => res.GetResult(opt) is not null) > 1)
    {
        string list = string.Join(", ", opts.Select(o => o.Name));
        res.AddError($"At most one of {list} can be selected");
    }
}

RootCommand root = new("Mini-Git");

// init command
Command initCommand = new("init", "Initialise a new repository");
initCommand.SetAction(pr =>
{
    Directory.CreateDirectory(".git");
    Directory.CreateDirectory(".git/objects");
    Directory.CreateDirectory(".git/refs");
    File.WriteAllText(".git/HEAD", $"ref: {Commit.MAIN_PATH}\n");
    Console.WriteLine("Initialized git directory");
});
root.Add(initCommand);

// cat-file command
var pCatOpt = new Option<bool>("-p", "--pretty-print") { Description = "Pretty-print object content" };
var tCatOpt = new Option<bool>("-t", "-type") { Description = "Only show object type" };
var sCarOpt = new Option<bool>("-s", "-size") { Description = "Only show object size" };
var catFileArg = new Argument<string>("object")
    { Arity = ArgumentArity.ExactlyOne, Description = "Object hash" };
Command catFileCommand = new("cat-file",
    "Provide contents or details of repository objects")
    { pCatOpt, tCatOpt, sCarOpt, catFileArg };
catFileCommand.Validators.Add(res => ValidateExactlyOne(res, pCatOpt, tCatOpt, sCarOpt));
catFileCommand.SetAction(pr =>
{
    GitObject gitObject = new GitObject(pr.GetValue(catFileArg) ?? "");
    if (pr.GetValue(pCatOpt))
    {
        if (gitObject.header.type == ObjectType.TREE)
        {
            Tree tree = new Tree(gitObject);
            foreach (TreeEntry entry in tree.Entries())
            {
                Console.WriteLine($"{int.Parse(entry.mode):D6} {entry.hash} {entry.name}");
            }
        }
        else
        {
            Console.WriteLine(gitObject.GetContentString());
        }
    }
    else if (pr.GetValue(tCatOpt)) Console.WriteLine(gitObject.header.type);
    else Console.WriteLine(gitObject.header.size);
});
root.Add(catFileCommand);

// hash-object command
var tHashOpt = new Option<string>("-t", "-type")
{
    Description = "Type of object to hash",
    DefaultValueFactory = pr => "blob",
};
tHashOpt.AcceptOnlyFromAmong(ObjectType.BLOB, ObjectType.TREE);
var wHashOpt = new Option<bool>("-w", "-write") { Description = "Write the object into the object database" };
var hashObjArg = new Argument<string>("file")
    { Arity = ArgumentArity.ExactlyOne, Description = "Directory or Filename" };
Command hashObjCommand = new("hash-object",
    "Compute object hash and optionally create an object from a file")
    { tHashOpt, wHashOpt, hashObjArg };
hashObjCommand.SetAction(pr =>
{
    GitObject go;
    string filename = pr.GetValue(hashObjArg) ?? "";
    switch (pr.GetValue(tHashOpt))
    {
        case ObjectType.BLOB:
            go = Blob.FromFile(filename);
            break;
        default:
            go = Tree.FromDirectory(Path.Join(Directory.GetCurrentDirectory(), filename));
            break;
    }
    Console.WriteLine(go.hash);
    if (pr.GetValue(wHashOpt)) go.Write();
});
root.Add(hashObjCommand);

// ls-tree command
var treeOnlyOpt = new Option<bool>("-t", "--tree-only") { Description = "Only show trees" };
var nameOnlyOpt = new Option<bool>("-n", "--name-only") { Description = "Only list names" };
var hashOnlyOpt = new Option<bool>("-o", "--hash-only") { Description = "Only list hashes" }; //-h is used for help
var lsTreeArg = new Argument<string>("hash")
    { Arity = ArgumentArity.ExactlyOne, Description = "Tree object hash" };
Command lsTreeCommand = new("ls-tree", "List the contents of a tree object")
    { treeOnlyOpt, nameOnlyOpt, hashOnlyOpt, lsTreeArg };
lsTreeCommand.Validators.Add(res => ValidateAtMostOne(res, nameOnlyOpt, hashOnlyOpt));
lsTreeCommand.SetAction(pr =>
{
    string hash = pr.GetValue(lsTreeArg) ?? "";
    bool treeOnly = pr.GetValue(treeOnlyOpt);
    bool nameOnly = pr.GetValue(nameOnlyOpt);
    bool hashOnly = pr.GetValue(hashOnlyOpt);
    foreach (var entry in new Tree(hash).Entries())
    {
        if (!treeOnly || entry.mode == Tree.Mode.DIR)
        {
            Console.WriteLine(nameOnly ? entry.name : hashOnly ? entry.hash :
                $"{int.Parse(entry.mode):D6} {entry.hash} {entry.name}");
        }
    }
});
root.Add(lsTreeCommand);

// write-tree command
var prefixOpt = new Option<string>("--prefix")
    { Description = "Create a tree object from prefix directory" };
Command writeTreeCommand = new Command("write-tree",
    "Create a tree object from the current directory") { prefixOpt };
writeTreeCommand.SetAction(pr =>
{
    Tree tree = Tree.FromDirectory(Path.Join(Directory.GetCurrentDirectory(), pr.GetValue(prefixOpt) ?? ""));
    tree.Write();
    Console.WriteLine(tree.hash);
});
root.Add(writeTreeCommand);

// commit command
var msgOpt = new Option<string>("-m")
    { Description = "Commit message", Required = true };
Command commitCommand = new("commit",
    "Commit current directory to main") { msgOpt };
commitCommand.SetAction(pr =>
{
    Commit commit;
    if (Commit.TryFromCurrent(pr.GetValue(msgOpt) ?? "", out commit))
    {
        commit.Write();
        Console.WriteLine(commit.hash);
    }
});
root.Add(commitCommand);

// log command
Command logCommand = new("log", "Show commit logs");
logCommand.SetAction(pr =>
{
    string commitHash = Commit.MainCommitHash();
    if (commitHash == "")
    {
        Console.WriteLine("Fatal: no commits found");
        return;
    }
    while (commitHash != "")
    {
        Console.WriteLine("-----");
        Commit commit = new Commit(commitHash);
        Console.WriteLine(commit);
        commitHash = commit.parent;
    }
});
root.Add(logCommand);

// diff command
var commitOpt = new Argument<string>("commit")
{
    Arity = ArgumentArity.ZeroOrOne,
    DefaultValueFactory = pr => Commit.MainCommitHash()
};
var nameCommitOpt = new Option<bool>("--name-only")
    { Description = "Only show the names of the changed files" };
Command diffCommand = new Command("diff", "Show changes between commits")
                        { commitOpt, nameCommitOpt };
diffCommand.SetAction(pr =>
{
    Tree currTree = Tree.FromDirectory(Directory.GetCurrentDirectory());
    currTree.Write();
    Tree pastTree = new Commit(pr.GetValue(commitOpt) ?? "").tree;

    if (currTree.hash == pastTree.hash)
    {
        Console.WriteLine("No changes found");
        return;
    }

    DiffChecker diffChecker = new DiffChecker();
    foreach (DiffEntry diff in diffChecker.GetDiff(pastTree, currTree))
    {
        switch (diff.diffType)
        {
            case DiffEntry.DiffType.Change:
                Console.ResetColor();
                break;
            case DiffEntry.DiffType.Creation:
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            default:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
        }
        Console.WriteLine(diff.name);
    }
    Console.ResetColor();
});
root.Add(diffCommand);

int status = root.Parse(args).Invoke();