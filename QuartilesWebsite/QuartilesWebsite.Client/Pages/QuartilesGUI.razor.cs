using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Quartiles;
using Chunks;
using Paths;

namespace QuartilesWebsite.Client.Pages
{
    public partial class QuartilesGUI : ComponentBase
    {
        public int MaxChunkLength { get; set; } = 5;
        public int Rows { get; set; } = 5;
        public int Columns { get; set; } = 4;

        private QuartilesCracker solver;
        protected List<Chunk> chunkList = [];

        public HashSet<string> Solutions { get; set; } = [];
        public Dictionary<string, List<string>> SolutionChunkMapping { get; set; } = [];

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
            solver.CurrentDictionary = "quartiles_dictionary_updated";
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

            var (solutions, solutionChunkMapping) = solver.QuartileSolverWithMapping(chunkLetters);
            this.Solutions = solutions;
            this.SolutionChunkMapping = solutionChunkMapping;
        }
    }
}