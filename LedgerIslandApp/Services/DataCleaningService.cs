using System.Text.RegularExpressions;

namespace LedgerIslandApp.Services
{
    public class DataCleaningService 
    {
        /// <summary>
        /// Apply cleaning rules to all fields in uploaded rows.
        /// </summary>
        public IEnumerable<string[]> Clean(IEnumerable<string[]> rows)
        {
            foreach (var row in rows)
            {
                var cleaned = row
                    .Select(field => CleanText(field))
                    .ToArray();

                yield return cleaned;
            }
        }

        /// <summary>
        /// Removes embedded CR/LF characters, collapses whitespace, trims.
        /// </summary>
        private static string CleanText(string? s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;

            // collapse CR/LF that sneak inside fields (not row delimiters)
            s = s.Replace('\r', ' ').Replace('\n', ' ');

            // normalize whitespace (collapse multiple spaces to one)
            s = Regex.Replace(s, @"\s{2,}", " ");

            return s.Trim();
        }
    }
}
