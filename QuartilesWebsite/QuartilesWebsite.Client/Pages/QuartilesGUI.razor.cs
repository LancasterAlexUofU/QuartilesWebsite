using Microsoft.AspNetCore.Components;
using Quartiles;

namespace QuartilesWebsite.Client.Pages
{
    public partial class QuartilesGUI : ComponentBase
    {
        public int MAX_CHUNK_LENGTH;
        public int rows;
        public int columns;

        public List<string> GridValues;
        public QuartilesCracker solver;
        public QuartilesGUI()
        {
            MAX_CHUNK_LENGTH = 5;
            rows = 5;
            columns = 4;

            GridValues = new();
            solver = new();
        }


        protected override void OnInitialized()
        {
            for (int row = 0; row < rows; row++)
            {

                for (int col = 0; col < columns; col++)
                {
                    GridValues.Add(string.Empty);
                }
            }
        }

        private async Task SubmitGrid()
        {
            if (GridValues.Contains(string.Empty))
            {

            }
        }
    }
}