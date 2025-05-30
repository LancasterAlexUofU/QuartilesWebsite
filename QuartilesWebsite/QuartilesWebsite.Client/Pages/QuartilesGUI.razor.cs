using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Quartiles;
using Chunks;
using Paths;
using Updater;

namespace QuartilesWebsite.Client.Pages
{
    public partial class QuartilesGUI : ComponentBase
    {
        public int MaxChunkLength { get; set; } = 4;
        public int Rows { get; set; } = 5;
        public int Columns { get; set; } = 4;

        private QuartilesCracker solver;
        //private DictionaryUpdater updater;
        //private QuartilePaths paths;
        protected List<Chunk> chunkList = [];

        public HashSet<string> Solutions { get; set; } = [];
        public Dictionary<string, List<string>> SolutionChunkMapping { get; set; } = [];

        public HashSet<string> KnownValidWords { get; private set; }


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
            //updater = new DictionaryUpdater();
            //paths = new QuartilePaths(filesToBeModified: false);

            //string validWordsPath = Path.Combine(paths.DictionaryUpdaterListsFolder, "kmown_valid_words.txt");
            //KnownValidWords = new HashSet<string>(File.ReadAllLines(validWordsPath));
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
            Dictionary<int, HashSet<string>> ChunkSizeSolutions = [];

            foreach (var solMapping in SolutionChunkMapping)
            {
                string solution = solMapping.Key;
                int chunkSize = solMapping.Value.Count;
                ChunkSizeSolutions.TryAdd(chunkSize, new HashSet<string>());
                ChunkSizeSolutions[chunkSize].Add(solution);
            }

            return ChunkSizeSolutions;
        }
    }
}