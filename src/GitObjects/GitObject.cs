namespace GitObjects
{
    class GitObject
    {
        public string hash { get; private set; }
        public string dir { get; private set; }
        public string filename { get; private set; }
        public string filepath { get => dir + filename; }

        public GitObject(string hash)
        {
            this.hash = hash;
            dir = $".git/objects/{hash[..2]}/";
            filename = $"{hash[2..]}";
        }
    }
}