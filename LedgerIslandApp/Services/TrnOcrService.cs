using IronOcr;
using System.Text.RegularExpressions;

namespace LedgerIslandApp.Services
{
    public class TrnOcrService
    {
        private static readonly Regex TrnRegex = new(@"\b\d{9}\b", RegexOptions.Compiled);


        public async Task<(string? value, float confidence)> ExtractTrnAsync(Stream imageStream)
        {
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            string? bestVal = null;
            double bestConf = 0;

            foreach (var angle in new[] { 0, 90, 180, 270 })
            {
                var input = new OcrInput();
                input.AddImage(bytes);
                if (angle != 0) input.Rotate(angle);
                input.Deskew();
                input.DeNoise();
                input.Binarize();
                input.EnhanceResolution();
                input.Contrast();

                var ocr = new IronTesseract { Language = OcrLanguage.English };
                ocr.Configuration.ReadBarCodes = false;
                ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.Auto;
                // whitelist via tesseract variable (works across versions)
                ocr.Configuration.TesseractVariables["tessedit_char_whitelist"] = "0123456789TRNtrn- ";

                var result = ocr.Read(input);
                if (string.IsNullOrWhiteSpace(result.Text)) continue;

                // 1) Prefer lines containing TRN; look on same and nearby lines
                IronOcr.OcrResult.Line[] lines = result.Lines ?? Array.Empty<IronOcr.OcrResult.Line>();
                for (int i = 0; i < lines.Length; i++)
                {
                    var lineText = (lines[i].Text ?? string.Empty).Replace(" ", "");
                    if (Regex.IsMatch(lineText, @"T\.?R\.?N\.?", RegexOptions.IgnoreCase))
                    {
                        // same line first
                        var mSame = Regex.Match(lineText, @"\d{9}");
                        if (mSame.Success && result.Confidence > bestConf)
                        {
                            bestVal = mSame.Value; bestConf = result.Confidence;
                            break;
                        }

                        // concatenate this and next 2 lines
                        var sb = new System.Text.StringBuilder(lineText);
                        if (i + 1 < lines.Length) sb.Append((lines[i + 1].Text ?? "").Replace(" ", ""));
                        if (i + 2 < lines.Length) sb.Append((lines[i + 2].Text ?? "").Replace(" ", ""));
                        var mNear = Regex.Match(sb.ToString(), @"\d{9}");
                        if (mNear.Success && result.Confidence > bestConf)
                        {
                            bestVal = mNear.Value; bestConf = result.Confidence;
                            break;
                        }
                    }
                }

                // 2) Fallback: anywhere in the page text
                if (bestVal is null)
                {
                    var all = (result.Text ?? string.Empty).Replace(" ", "");
                    var m = Regex.Match(all, @"\d{9}");
                    if (m.Success && result.Confidence > bestConf)
                    {
                        bestVal = m.Value; bestConf = result.Confidence;
                    }
                }

                if (bestVal != null) break; // stop rotation loop once found
            }

            return bestVal is null ? (null, 0f) : (bestVal, (float)(bestConf / 100.0));
        }






    }
}
