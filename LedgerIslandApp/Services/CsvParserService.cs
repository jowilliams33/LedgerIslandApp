using Microsoft.AspNetCore.Components.Forms;
using System.Text;

namespace LedgerIslandApp.Services
{
    public class CsvParserService 
    {
        public async Task<(string[] headers, List<string[]> rows)> ParseAsync(
            IBrowserFile file,
            CancellationToken ct = default)
        {
            using var stream = file.OpenReadStream(long.MaxValue);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);

            var lines = new List<string[]>();
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
                lines.Add(ParseLine(line));

            if (lines.Count == 0)
                return (Array.Empty<string>(), new List<string[]>());

            var headers = lines[0];
            lines.RemoveAt(0);
            return (headers, lines);
        }

        public string[] ParseLine(string line)
        {
            var cells = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    { sb.Append('"'); i++; }
                    else { inQuotes = !inQuotes; }
                }
                else if (c == ',' && !inQuotes)
                { cells.Add(sb.ToString()); sb.Clear(); }
                else
                { sb.Append(c); }
            }

            cells.Add(sb.ToString());
            return cells.ToArray();
        }
    }
}
