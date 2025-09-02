using System.Diagnostics;
using GitObjects;
using ZLibDotNet;

namespace Requests
{
    class Packfile
    {
        Dictionary<string, GitObject> lookup = new Dictionary<string, GitObject>();
        Dictionary<string, PackObject> typeLookup = new Dictionary<string, PackObject>();

        public Packfile(byte[] contentBytes, int numObjects)
        {
            List<DeltaObject> deltas = new List<DeltaObject>();
            using MemoryStream contentStream = new MemoryStream(contentBytes);
            for (int i = 0; i < numObjects; i++)
            {
                // Get object header
                int b = contentStream.ReadByte();
                PackObject type = (PackObject)((b >> 4) & 7);
                int size = (b & 0xF) + ((b & 0x80) == 0 ? 0 : ReadVarLenInt(contentStream)) << 4;

                // Get content
                byte[] baseObj = new byte[20];
                switch (type)
                {
                    case PackObject.REF_DELTA:
                        contentStream.Read(baseObj);
                        break;
                    case PackObject.OFS_DELTA:
                        // Don't advertise support for this
                        throw new NotImplementedException(
                            "Offset delta object not implemented!");
                }

                byte[] bytes = new byte[size];

                ZStream zStream = new ZStream()
                {
                    Input = contentBytes[(int)contentStream.Position..],
                    Output = bytes,
                };

                ZLib zlib = new();
                int _ = zlib.InflateInit(ref zStream);
                _ = zlib.Inflate(ref zStream, ZLib.Z_SYNC_FLUSH);

                contentStream.Seek(zStream.NextIn, SeekOrigin.Current);

                // Ignore trailing null bytes
                int end;
                for (end = bytes.Length; end > 0 && bytes[end - 1] == 0; end--) { }
                if(end == 0) continue;
                bytes = bytes[..end];

                switch (type)
                {
                    case PackObject.BLOB:
                    case PackObject.TREE:
                    case PackObject.COMMIT:
                        GitObject obj = FromContent(type, bytes);
                        lookup[obj.hash] = obj;
                        typeLookup[obj.hash] = type;
                        break;
                    case PackObject.REF_DELTA:
                        string baseHash = Convert.ToHexStringLower(baseObj);
                        deltas.Add(new DeltaObject(baseHash, bytes));
                        break;
                }
            }

            ReadDeltas(deltas);
        }

        private void ReadDeltas(List<DeltaObject> deltas)
        {
            while (deltas.Count > 0)
            {
                // Some deltas reference other deltas
                // so first do the ones that don't
                int decoded = 0;
                for (int i = 0; i < deltas.Count; i++)
                {
                    if (lookup.ContainsKey(deltas[i].baseHash))
                    {
                        ReadDelta(deltas[i]);
                        decoded++;
                        deltas.RemoveAt(i);
                        i--;
                    }
                }
                // Don't infinitely loop
                if (decoded == 0)
                {
                    throw new Exception("Error decoding deltafied objects");
                }
            }
        }

        /*
            Format:
            <variable length base size><variable length result size><copy or add instructions>
            Copy instructions: <1xxxxxxx><offset (<= 4 bytes)><size (<= 3 bytes)>
            Add instructions: <0xxxxxxx><data>
        */
        private void ReadDelta(DeltaObject delta)
        {
            using MemoryStream mStream = new MemoryStream(delta.content);
            int baseSize = ReadVarLenInt(mStream);
            int resSize = ReadVarLenInt(mStream);

            byte[] baseBytes = lookup[delta.baseHash].content;
            Debug.Assert(baseSize == baseBytes.Length);
            byte[] resBytes = new byte[resSize];

            int index = 0, instr;
            while ((instr = mStream.ReadByte()) != -1)
            {
                // Copy instruction
                if ((instr & 0x80) > 0)
                {
                    int offset = 0, size = 0, shift;
                    for (shift = 0; shift < 4; shift++)
                    {
                        if ((instr & (1 << shift)) != 0)
                        {
                            offset |= mStream.ReadByte() << (shift * 8);
                        }
                    }
                    for (; shift < 7; shift++)
                    {
                        if ((instr & (1 << shift)) != 0)
                        {
                            size |= mStream.ReadByte() << ((shift - 4) * 8);
                        }
                    }
                    if (size == 0) size = 0x10000;
                    Array.Copy(baseBytes, offset, resBytes, index, size);
                    index += size;
                }
                // Add instruction
                else
                {
                    int toAdd = instr & 0x7F;
                    mStream.ReadExactly(resBytes, index, toAdd);
                    index += toAdd;
                }
            }

            PackObject type = typeLookup[delta.baseHash];
            GitObject obj = FromContent(type, resBytes);
            lookup[obj.hash] = obj;
            typeLookup[obj.hash] = type;
        }

        public void Write(string mainHash)
        {
            // Commit tree should be current directory
            Commit mainCommit = (Commit)lookup[mainHash];
            Tree currDir = (Tree)lookup[mainCommit.treeHash];
            // Write files to current directory and .git/objects
            WriteDir(currDir);
            foreach ((string hash, PackObject type) in typeLookup)
            {
                if (type == PackObject.COMMIT)
                {
                    lookup[hash].Write(writeSubfiles: false);
                }
            }
            // Update head
            Directory.CreateDirectory(Path.GetDirectoryName(Commit.mainPath) ?? "");
            File.WriteAllText(Commit.mainPath, mainCommit.hash);
        }

        private void WriteDir(Tree tree, string dir = "")
        {
            tree.Write(writeSubfiles: false);
            foreach (TreeEntry entry in tree.Entries())
            {
                GitObject go = lookup[entry.hash];
                go.Write(writeSubfiles: false);
                if (entry.mode == Tree.Mode.FILE)
                {
                    string content = go.GetContentString();
                    File.WriteAllText(Path.Join(dir, entry.name), content);
                }
                else if (entry.mode == Tree.Mode.DIR)
                {
                    string newDir = Path.Join(dir, entry.name);
                    Directory.CreateDirectory(newDir);
                    WriteDir((Tree)go, newDir);
                }
            }
        }

        private GitObject FromContent(PackObject type, byte[] bytes)
            => type switch
            {
                PackObject.BLOB => Blob.FromContent(bytes),
                PackObject.TREE => Tree.FromContent(bytes),
                _ => Commit.FromContent(bytes)
            };

        private int ReadVarLenInt(MemoryStream stream)
        {
            int res = 0, shift = 0;
            while (true)
            {
                int b = stream.ReadByte();
                res |= (b & 0x7F) << shift;
                shift += 7;

                if ((b & 0x80) == 0) return res;
            }
        }

        enum PackObject
        {
            COMMIT = 1,
            TREE = 2,
            BLOB = 3,
            OFS_DELTA = 6,
            REF_DELTA = 7
        };
    }

    struct DeltaObject
    {
        public string baseHash;
        public byte[] content;

        public DeltaObject(string baseHash, byte[] content)
        {
            this.baseHash = baseHash;
            this.content = content;
        }
    }
}