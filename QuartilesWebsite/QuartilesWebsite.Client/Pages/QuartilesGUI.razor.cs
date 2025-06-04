using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Quartiles;
using Chunks;
using Paths;


namespace QuartilesWebsite.Client.Pages
{
    public partial class QuartilesGUI : ComponentBase
    {
        public int MaxChunkLength { get; set; } = 4;
        public int Rows { get; set; } = 5;
        public int Columns { get; set; } = 4;

        private QuartilesCracker solver;
        private QuartilePaths paths;
        protected List<Chunk> chunkList = [];

        public HashSet<string> Solutions { get; set; } = [];
        public Dictionary<string, List<string>> SolutionChunkMapping = [];

        public HashSet<string> KnownValidWords;
        public HashSet<string> SCOWLDict;

        public Dictionary<int, string> RankingToColor = new()
        {
            { KNOWN_VALID_SOLUTION, "KNOWN_VALID_SOLUTION" },
            { LIKELY_VALID_SOLUTION, "LIKELY_VALID_SOLUTION" },
            { WEAK_SOLUTION, "WEAK_SOLUTION" },
        };

        public const int KNOWN_VALID_SOLUTION = 0;
        public const int LIKELY_VALID_SOLUTION = 1;
        public const int WEAK_SOLUTION = 2;


        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        protected override void OnInitialized()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    Chunk cell = new Chunk(row, col);
                    chunkList.Add(cell);
                }
            }

            solver = new QuartilesCracker();
            paths = new QuartilePaths(filesToBeModified: false);

            string validWordsPath = Path.Combine(paths.QuartilesCrackerOtherDictFolder, "known_valid_words.txt");
            string scowlWordsPath = Path.Combine(paths.QuartilesCrackerOtherDictFolder, "2of12.txt");
            KnownValidWords = new HashSet<string>(File.ReadAllLines(validWordsPath));
            SCOWLDict = new HashSet<string>(File.ReadLines(scowlWordsPath));
        }

        protected Chunk GetChunk(int row, int column)
        {
            return chunkList.FirstOrDefault(c => c.Row == row && c.Column == column);
        }

        protected async Task SubmitGrid()
        {
            if (chunkList.Any(c => string.IsNullOrWhiteSpace(c.Letters)))
            {
                await JS.InvokeVoidAsync("alert", "Please fill in all available cells in the table.");
            }

            else
            {
               DisplaySolutions();
            }
        }

        protected void DisplaySolutions()
        {
            List<string> chunkLetters = new();
            foreach (Chunk cell in chunkList)
            {
                chunkLetters.Add(cell.Letters);
            }

            (Solutions, SolutionChunkMapping) = solver.QuartileSolverWithMapping(chunkLetters);
        }

        protected Dictionary<int, HashSet<string>> GetSeparateChunkSizeSolutions()
        {
            Dictionary<int, HashSet<string>> chunkSizeSolutions = [];

            foreach (var solMapping in SolutionChunkMapping)
            {
                string solution = solMapping.Key;
                int chunkSize = solMapping.Value.Count;
                chunkSizeSolutions.TryAdd(chunkSize, new HashSet<string>());
                chunkSizeSolutions[chunkSize].Add(solution);
            }

            return chunkSizeSolutions;
        }

        protected Dictionary<int, HashSet<(int ranking, string solution)>> GetBestSolutionOrdering()
        {
            Dictionary<int, HashSet<(int ranking, string solution)>> bestSolutionOrdering = [];

            foreach (var solMapping in SolutionChunkMapping)
            {
                int chunkSize = solMapping.Value.Count;
                string solution = solMapping.Key;

                bestSolutionOrdering.TryAdd(chunkSize, new HashSet<(int ranking, string solution)>());

                if (KnownValidWords.Contains(solution))
                {
                    bestSolutionOrdering[chunkSize].Add((KNOWN_VALID_SOLUTION, solution));
                }

                else if (SCOWLDict.Contains(solution))
                {
                    bestSolutionOrdering[chunkSize].Add((LIKELY_VALID_SOLUTION, solution));
                }

                else
                {
                    bestSolutionOrdering[chunkSize].Add((WEAK_SOLUTION, solution));
                }
            }

            foreach (var bestMapping in bestSolutionOrdering)
            {
                int chunkSize = bestMapping.Key;

                HashSet<(int ranking, string solution)> orderedSet = bestSolutionOrdering[chunkSize].OrderBy(x => x.ranking).ToHashSet();
                bestSolutionOrdering[chunkSize] = orderedSet;
            }

            return bestSolutionOrdering;
        }
    }
}