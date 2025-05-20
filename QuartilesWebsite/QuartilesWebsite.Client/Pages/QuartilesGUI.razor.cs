using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Quartiles;
using Chunks;

namespace QuartilesWebsite.Client.Pages
{
    public partial class QuartilesGUI : ComponentBase
    {
        public int MAX_CHUNK_LENGTH;
        public int rows;
        public int columns;

        public List<string> GridValues;
        public QuartilesCracker solver;
        public List<Chunk> chunkList;

        public List<string> Results { get; set; } = new();

        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        public QuartilesGUI()
        {
            MAX_CHUNK_LENGTH = 5;
            rows = 5;
            columns = 4;

            GridValues = new();
            solver = new();
            chunkList = new();
        }

        protected override void OnInitialized()
        {
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Chunk cell = new Chunk(row, col);
                    chunkList.Add(cell);
                }
            }
        }

        public Chunk GetChunk(int row, int column)
        {
            return chunkList.FirstOrDefault(c => c.Row == row && c.Column == column);
        }

        public async Task SubmitGrid()
        {
            if (chunkList.Any(c => string.IsNullOrWhiteSpace(c.Letters)))
            {
                await JS.InvokeVoidAsync("alert", "Please fill in all available cells in the table.");
            }

            List<string> chunkLetters = new();
            foreach (Chunk cell in chunkList)
            {
                chunkLetters.Add(cell.Letters);
            }

            Results = solver.QuartilesDriver(chunkLetters);
        }
    }
}