using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Quartiles;
using Chunks;
using Paths;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;



namespace QuartilesWebsite.Client.Pages
{
    public partial class QuartilesGUI : ComponentBase
    {

        private QuartilesCracker solver;
        private QuartilePaths paths;

        /// <summary>
        /// Maximum number of characters for a chunk
        /// </summary>
        public int MaxChunkLength { get; set; } = 4;

        /// <summary>
        /// Number of rows in a quartiles grid
        /// </summary>
        public int Rows { get; set; } = 5;

        /// <summary>
        /// Number of columns in a quartiles grid
        /// </summary>
        public int Columns { get; set; } = 4;



        /// <summary>
        /// List containing chunks entered by user. Chunk letters initialized to empty in OnInitialized
        /// </summary>
        protected List<Chunk> chunkList = [];

        private List<string> ocrChunks = [];

        /// <summary>
        /// Set of solutions found by QuartilesSolver
        /// </summary>
        public HashSet<string> Solutions { get; set; } = [];

        /// <summary>
        /// Dictionary containing solutions and their associated chunks
        /// </summary>
        public Dictionary<string, List<string>> SolutionChunkMapping = [];

        /// <summary>
        /// Set containing words found in previous quartiles solutions
        /// </summary>
        private HashSet<string> KnownValidWords;

        /// <summary>
        /// Set containing words from SCOWL (Spell Checker Oriented Word Lists), a reliable source to tell if a word is valid
        /// </summary>
        public HashSet<string> SCOWLDict;


        /// <summary>
        /// Dictionary mapping ranking values to their "color" class value
        /// </summary>
        public Dictionary<int, string> RankingToColor = new()
        {
            { KNOWN_VALID_SOLUTION, "KNOWN_VALID_SOLUTION" },
            { LIKELY_VALID_SOLUTION, "LIKELY_VALID_SOLUTION" },
            { WEAK_SOLUTION, "WEAK_SOLUTION" },
        };

        // Constants representing solution confidence levels
        public const int KNOWN_VALID_SOLUTION = 0;
        public const int LIKELY_VALID_SOLUTION = 1;
        public const int WEAK_SOLUTION = 2;

        private const int MAX_FILESIZE = 5000 * 1024; // 5 MB


        [Inject]
        private IJSRuntime JS { get; set; } = default!;

        [Inject]
        private HttpClient Http { get; set; }

        /// <summary>
        /// Initialized cells to empty and reads in dictionaries to sets
        /// </summary>
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

        /// <summary>
        /// Given a row and column, returns a Chunk with the same row and column
        /// </summary>
        /// <param name="row">Row of the chunk, 0 indexed</param>
        /// <param name="column">Column of the chunk, 0 indexed</param>
        /// <returns>A Chunk object with the same row and column</returns>
        protected Chunk GetChunk(int row, int column)
        {
            return chunkList.FirstOrDefault(c => c.Row == row && c.Column == column);
        }

        /// <summary>
        /// Checks if any tiles are missing before submitting
        /// </summary>
        /// <returns></returns>
        protected async Task SubmitGrid()
        {
            if (chunkList.Any(c => string.IsNullOrWhiteSpace(c.Letters)))
            {
                await JS.InvokeVoidAsync("alert", "Please fill in all available cells in the table.");
            }

            else
            {
               FindSolutions();
            }
        }

        /// <summary>
        /// Finds the solutions and stores them in Solutions and SolutionChunkMapping
        /// </summary>
        protected void FindSolutions()
        {
            List<string> chunkLetters = new();
            foreach (Chunk cell in chunkList)
            {
                chunkLetters.Add(cell.Letters);
            }

            (Solutions, SolutionChunkMapping) = solver.QuartileSolverWithMapping(chunkLetters);
        }

        /// <summary>
        /// After a SolutionChunkMapping has been found, this method assigns a rank for each solution based on the likeliness of it being a valid solution.
        /// 
        /// <para>
        /// This method sorts each chunk solution size by rank, with the highest probability solutions appearing first
        /// </para>
        /// </summary>
        /// <returns>A Dictionary, with keys being the amount of chunks that make up a solution, and values being a tuple of solutions with their rank, in order</returns>
        protected Dictionary<int, HashSet<(int ranking, string solution)>> GetBestSolutionOrdering()
        {
            Dictionary<int, HashSet<(int ranking, string solution)>> bestSolutionOrdering = [];

            // Goes through each solution and checks if it is in more known dictionaries
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

            // Reorders set based on rank for each chunk solution size
            foreach (var bestMapping in bestSolutionOrdering)
            {
                int chunkSize = bestMapping.Key;

                HashSet<(int ranking, string solution)> orderedSet = bestSolutionOrdering[chunkSize].OrderBy(x => x.ranking).ToHashSet();
                bestSolutionOrdering[chunkSize] = orderedSet;
            }

            return bestSolutionOrdering;
        }

        /// <summary>
        /// Handles the image upload event, checks the file type, and sends the file to the API for OCR processing.
        /// </summary>
        /// <param name="e">The file event contain file information</param>
        /// <returns></returns>
        protected async Task HandleImageUpload(InputFileChangeEventArgs e)
        {
            // File type check
            var file = e.File;
            var allowedTypes = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif" };

            string extension = Path.GetExtension(file.Name).ToLowerInvariant();
            if (!allowedTypes.Contains(extension))
            {
                Console.WriteLine("Unsupported file type.");
                return;
            }

            // File stream
            using var content = new MultipartFormDataContent();
            Stream stream = file.OpenReadStream(MAX_FILESIZE);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            content.Add(fileContent, "image", file.Name);

            await GetApiDataOcr(content);
        }

        /// <summary>
        /// Sends the MultipartFormDataContent to the API for OCR processing and retrieves the recognized text chunks.
        /// </summary>
        /// <param name="content">Image to be processed</param>
        /// <returns></returns>
        private async Task GetApiDataOcr(MultipartFormDataContent content)
        {
            try
            {
                var response = await Http.PostAsync("https://localhost:7293/api/upload/upload-image", content);

                if (response.IsSuccessStatusCode)
                {
                    ocrChunks = await response.Content.ReadFromJsonAsync<List<string>>();

                    if (ocrChunks != null)
                    {
                        FillGridFromOCR(ocrChunks);
                        StateHasChanged(); // force UI to refresh after setting the letters
                    }
                }

                else
                {
                    Console.WriteLine("Upload failed.");
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Fills the grid with letters from the OCR result.
        /// </summary>
        /// <param name="ocrChunks">List of chunks scanned in from OCR</param>
        private void FillGridFromOCR(List<string> ocrChunks)
        {
            if (ocrChunks == null || ocrChunks.Count != Rows * Columns)
            {
                Console.WriteLine("OCR result is null or does not match expected grid size.");
                return;
            }

            for (int i = 0; i < ocrChunks.Count; i++)
            {
                chunkList[i].Letters = ocrChunks[i];
            }
        }
    }
}