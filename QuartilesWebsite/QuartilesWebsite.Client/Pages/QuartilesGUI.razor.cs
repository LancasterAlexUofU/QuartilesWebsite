using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Quartiles;
using Chunks;

namespace QuartilesWebsite.Client.Pages
{
    public partial class QuartilesGUI : ComponentBase
    {
        public int MaxChunkLength { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }

        private QuartilesCracker solver = new();
        protected List<Chunk> chunkList = new();

        public List<string> Results { get; set; } = new();
        public List<KeyValuePair<string, List<string>>> SolutionChunkMapping { get; set; } = new();

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        public QuartilesGUI(int maxChunkLength = 5, int rows = 5, int columns = 4)
        {
            MaxChunkLength = maxChunkLength;
            Rows = rows;
            Columns = columns;
        }

        public QuartilesGUI() 
        {
            MaxChunkLength = 5;
            Rows = 5;
            Columns = 4;
        }

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

            DisplaySolutions();
        }

        protected async Task DisplaySolutions()
        {
            List<string> chunkLetters = new();
            foreach (Chunk cell in chunkList)
            {
                chunkLetters.Add(cell.Letters);
            }

            var (results, solutionChunkMapping) = solver.QuartileSolver(chunkLetters);
            this.Results = results;
            this.SolutionChunkMapping = solutionChunkMapping;
        }
    }
}