using BusinessLogicLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BusinessLogicLayer.Services
{
    public class TextPreprocessService : ITextPreprocessService
    {
        public Task<string> PreProcessText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(string.Empty);

            // 1. Normalize line endings
            text = text.Replace("\r\n", "\n");

            var lines = text.Split('\n');
            var cleanedLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // 2. Drop digit walls (80% digits, long lines)
                int digitCount = trimmed.Count(char.IsDigit);
                if (trimmed.Length > 50 &&
                    digitCount > trimmed.Length * 0.8)
                {
                    continue;
                }

                // 3. Drop table headers
                if (TableHeaders.IsMatch(trimmed))
                    continue;

                // 4. Remove long digit sequences inside text
                trimmed = LongDigitSequence.Replace(trimmed, " ");

                // 5. Keep only lines that look like sentences
                if (!LooksLikeSentence(trimmed))
                    continue;

                cleanedLines.Add(trimmed);
            }

            // 6. Rebuild text
            var result = string.Join("\n", cleanedLines);

            // 7. Normalize whitespace
            result = MultipleSpaces.Replace(result, " ");
            result = MultipleNewlines.Replace(result, "\n\n");

            return Task.FromResult(result.Trim());
        }

        private static readonly Regex LongDigitSequence =
          new(@"\b\d{12,}\b", RegexOptions.Compiled);

        private static readonly Regex TableHeaders =
            new(@"(Level\s*\d+|Pay\s*Band|Grade\s*Pay)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex MultipleSpaces =
            new(@"\s{2,}", RegexOptions.Compiled);

        private static readonly Regex MultipleNewlines =
            new(@"\n{3,}", RegexOptions.Compiled);
        private static bool LooksLikeSentence(string line)
        {
            // Heuristic: contains verbs or punctuation
            return Regex.IsMatch(
                line,
                @"\b(is|are|was|were|shall|must|should|will|may|can|apply|provide|require)\b",
                RegexOptions.IgnoreCase)
                || line.Contains('.');
        }

    }
}
