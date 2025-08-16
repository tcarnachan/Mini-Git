using GitObjects;

namespace DiffChecker
{
    // Stores changes of lines in a file
    // Uses the Myers Difference Algorithm
    class FileDiffChecker
    {
        private string[] prev, curr;
        public List<LineDiff> diffs { get; private set; }

        // Created or deleted file
        public FileDiffChecker(Blob file, DiffType type)
        {
            // Created file
            if (type == DiffType.Creation)
            {
                prev = [];
                curr = file.GetContentString().Split('\n');
            }
            // Deleted file
            else
            {
                prev = file.GetContentString().Split('\n');
                curr = [];
            }
            diffs = new List<LineDiff>();
            GetDiff();
        }

        public FileDiffChecker(Blob prev, Blob curr)
        {
            this.prev = prev.GetContentString().Split('\n');
            this.curr = curr.GetContentString().Split('\n');
            diffs = new List<LineDiff>();
            GetDiff();
        }

        public (int ins, int del) GetSummary()
            => (diffs.Count(c => c.diffType == DiffType.Creation),
                diffs.Count(c => c.diffType == DiffType.Deletion));

        private void GetDiff()
        {
            var path = ShortestEditPath();
            string[] diffPrev = [.. prev, ""];
            string[] diffCurr = [.. curr, ""];
            foreach (var (prevX, prevY, x, y) in BackTrack(path))
            {
                string prevLine = diffPrev[prevX];
                string currLine = diffCurr[prevY];

                if (x == prevX)
                {
                    diffs.Add(new LineDiff(
                        DiffType.Creation, "", currLine, -1, prevY + 1));
                }
                else if (y == prevY)
                {
                    diffs.Add(new LineDiff(
                        DiffType.Deletion, prevLine, "", prevX + 1, -1));
                }
                else
                {
                    diffs.Add(new LineDiff(
                        DiffType.Unchanged, prevLine, currLine, prevX + 1, prevY + 1));
                }
            }
            diffs.Reverse();
        }

        private List<ShiftedArray<int>> ShortestEditPath()
        {
            int n = prev.Length, m = curr.Length;
            int max = prev.Length + curr.Length;

            ShiftedArray<int> v = new ShiftedArray<int>(-max, max);
            v[1] = 0;
            List<ShiftedArray<int>> trace = new();

            for (int d = 0; d <= max; d++)
            {
                trace.Add(new ShiftedArray<int>(v));
                for (int k = -d; k <= d; k += 2)
                {
                    int x;
                    if (k == -d || (k != d && v[k - 1] < v[k + 1]))
                    {
                        x = v[k + 1];
                    }
                    else
                    {
                        x = v[k - 1] + 1;
                    }
                    int y = x - k;
                    for (; x < n && y < m && prev[x] == curr[y]; x++, y++) { }
                    v[k] = x;

                    if (x >= n && y >= m)
                    {
                        return trace;
                    }
                }
            }

            return trace;
        }

        private IEnumerable<(int, int, int, int)> BackTrack(List<ShiftedArray<int>> trace)
        {
            int x = prev.Length, y = curr.Length;
            for (int d = trace.Count - 1; d >= 0; d--)
            {
                var v = trace[d];
                int k = x - y;
                int prevK;
                if (k == -d || (k != d && v[k - 1] < v[k + 1]))
                {
                    prevK = k + 1;
                }
                else
                {
                    prevK = k - 1;
                }
                int prevX = v[prevK];
                int prevY = prevX - prevK;
                while (x > prevX && y > prevY)
                {
                    yield return (x - 1, y - 1, x, y);
                    (x, y) = (x - 1, y - 1);
                }
                if (d > 0)
                {
                    yield return (prevX, prevY, x, y);
                }
                (x, y) = (prevX, prevY);
            }
        }

        // Array with shifted indices going from
        // min...max instead of 0...(max - min + 1)
        class ShiftedArray<T>
        {
            private T[] arr;
            private int minIndex;

            public T this[int index]
            {
                get => arr[index - minIndex];
                set => arr[index - minIndex] = value;
            }

            public ShiftedArray(int minIndex, int maxIndex)
            {
                arr = new T[maxIndex - minIndex + 1];
                this.minIndex = minIndex;
            }

            public ShiftedArray(ShiftedArray<T> shiftedArr)
            {
                arr = new T[shiftedArr.arr.Length];
                Array.Copy(shiftedArr.arr, arr, arr.Length);
                minIndex = shiftedArr.minIndex;
            }
        }
    }
}