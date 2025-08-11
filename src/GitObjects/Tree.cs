namespace GitObjects
{
    /*
    Tree file format:
        tree <size>\0<mode> <name>\0<20_byte_sha><mode> <name>\0<20_byte_sha>
    */

    class Tree : GitObject
    {
        public Tree(string hash) : base(hash) { }

        public Tree(byte[] content) : base(content) { }
    }
}