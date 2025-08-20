using System.Diagnostics;
using System.Text;

namespace Requests
{
    class GitRequest
    {
        private HttpClient client;

        public GitRequest(string repo_url)
        {
            client = new HttpClient()
            {
                BaseAddress = new Uri(repo_url + "/")
            };
        }

        public async Task<string> GetMainHash()
        {
            string target = "info/refs?service=git-upload-pack";
            using HttpResponseMessage response = await client.GetAsync(target);
            response.EnsureSuccessStatusCode();

            /*
                Format (<size> is 4-digit hex):
                001e# service=git-upload-pack
                0000<size><hash> HEAD\0<server capabilities>
                <size><hash> <branch name>
                ...
                <size><hash> <branch name>
                0000
            */
            string responseBody = await response.Content.ReadAsStringAsync();
            string[] lines = responseBody.Split('\n');

            Debug.Assert(lines[0] == "001e# service=git-upload-pack");
            Debug.Assert(lines.Last() == "0000");

            return lines[1].Split()[0][8..];
        }

        private byte[] FormatLine(string line)
        {
            byte[] lineBytes = Encoding.UTF8.GetBytes(line + "\n");
            // +4 because size includes itself
            string size = (lineBytes.Length + 4).ToString("X4");
            return [.. Encoding.UTF8.GetBytes(size), .. lineBytes];
        }

        public async Task<(byte[], int)> GetPackContent(string hash)
        {
            string target = "git-upload-pack";

            HttpContent content = new ByteArrayContent([
                .. FormatLine($"want {hash}"),
                .. Encoding.UTF8.GetBytes("0000"),
                .. FormatLine("done")
            ]);
            content.Headers.Add("Content-Type", "application/x-git-upload-pack-request");

            /*
                Format:
                0008NAK
                <header><content><checksum>

                header is PACK<version><number of objects>
            */
            var response = await client.PostAsync(target, content);
            byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();

            int ix = Array.IndexOf(responseBytes, (byte)'\n');
            byte[] packfileBytes = responseBytes[(ix + 1)..];

            // Header (first 12 bytes)
            string magic = Encoding.UTF8.GetString(packfileBytes[..4]);
            Debug.Assert(magic == "PACK");
            int version = FromHexBytes(packfileBytes[4..8]);
            int numObjects = FromHexBytes(packfileBytes[8..12]);
            Console.WriteLine($"Header: v{version} #{numObjects}");

            return (packfileBytes[12..], numObjects);
        }

        private int FromHexBytes(byte[] hex)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(hex);
            }
            return BitConverter.ToInt32(hex);
        }
    }
}