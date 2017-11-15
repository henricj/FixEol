using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ude;

namespace FixEol
{
    public sealed class EncodingInformation
    {
        static readonly Dictionary<string, Encoding> Encodings = CreateEncodings();

        public Encoding Encoding { get; private set; }
        public bool BomDetected { get; private set; }

        static Dictionary<string, Encoding> CreateEncodings()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return new Dictionary<string, Encoding>(StringComparer.InvariantCultureIgnoreCase)
            {
                // http://sourceforge.net/projects/streaman/files/Useful_Tools/UniversalCharDetCS/
                { "UTF-8", Encoding.UTF8 },
                { "UTF-16LE", Encoding.Unicode },
                { "UTF-16BE", Encoding.BigEndianUnicode },
                { "UTF-32LE", Encoding.UTF32 },
                { "UTF-32BE", Encoding.GetEncoding(12001) },
                { "X-ISO-10646-UCS-4-2143", Encoding.UTF32 },
                { "X-ISO-10646-UCS-4-3412", Encoding.GetEncoding(12001) },
                { "ISO-8859-5", Encoding.GetEncoding(28595) },
                { "windows-1251", Encoding.GetEncoding(1251) },
                { "Big-5", Encoding.GetEncoding(950) },
                { "GB18030", Encoding.GetEncoding(54936) },
                { "HZ-GB-2312", Encoding.GetEncoding(52936) },
                { "ISO-2022-CN", Encoding.GetEncoding(50227) },
                { "x-cp50227", Encoding.GetEncoding(50227) },
                { "x-euc-tw", Encoding.GetEncoding(51936) },
                { "EUC-TW", Encoding.GetEncoding(51936) },
                { "EUC-CN", Encoding.GetEncoding(51936) },
                { "ISO-8859-7", Encoding.GetEncoding(28597) },
                { "windows-1253", Encoding.GetEncoding(1253) },
                { "ISO-8859-8", Encoding.GetEncoding(28598) },
                { "windows-1255", Encoding.GetEncoding(1255) },
                { "EUC-JP", Encoding.GetEncoding(51932) },
                { "ISO-2022-JP", Encoding.GetEncoding(50222) },
                { "csISO2022JP", Encoding.GetEncoding(50222) },
                { "Shift_JIS", Encoding.GetEncoding(932) },
                { "EUC-KR", Encoding.GetEncoding(51949) },
                { "ISO-2022-KR", Encoding.GetEncoding(50225) },
                { "IBM855", Encoding.GetEncoding(855) },
                { "IBM866", Encoding.GetEncoding(866) },
                { "KOI8-R", Encoding.GetEncoding(20866) },
                { "x-mac-cyrillic", Encoding.GetEncoding(10007) },
                { "TIS-620", Encoding.GetEncoding(874) },
                { "ISO 8859-11", Encoding.GetEncoding(874) },
                { "ASCII", Encoding.ASCII },
                { "us-ascii", Encoding.ASCII },
                { "windows-1252", Encoding.GetEncoding(1252) }
            };
        }

        public static async Task<EncodingInformation> DetectEncodingAsync(Stream stream)
        {
            var cdet = new CharsetDetector();

            await cdet.FeedAsync(stream).ConfigureAwait(false);

            cdet.DataEnd();

            if (cdet.Charset != null)
            {
                if (Encodings.TryGetValue(cdet.Charset, out var encoding))
                {
                    return new EncodingInformation
                    {
                        Encoding = encoding,
                        BomDetected = cdet.BomDetected
                    };
                }

                try
                {
                    return new EncodingInformation
                    {
                        Encoding = Encoding.GetEncoding(cdet.Charset),
                        BomDetected = cdet.BomDetected
                    };
                }
                catch (ArgumentException ex)
                {
                    Debug.WriteLine("Encoding {0} not found: {1}", cdet.Charset, ex.Message);
                }

                Console.WriteLine("Unknown encoding for " + cdet.Charset);
            }
            else
            {
                Console.WriteLine("Detection failed.");
            }

            return null;
        }
    }
}
