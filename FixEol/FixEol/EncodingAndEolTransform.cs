using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FixEol
{
    public sealed class EncodingAndEolTransform
    {
        #region BomPolicy enum

        public enum BomPolicy
        {
            Never,
            Force,

            /// <summary>
            ///     Output BOM iff the source file had a BOM.
            /// </summary>
            CopySource,

            /// <summary>
            ///     Output BOM unless the source encoding was UTF-8 without a BOM.
            /// </summary>
            CopyUtf8OrForce
        }

        #endregion

        static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false, true);
        static readonly Encoding Utf8WithBom = Encoding.UTF8;

        public EncodingAndEolTransform()
        {
            OutputBomPolicy = BomPolicy.Force;
        }

        /// <summary>
        ///     Force a particular output encoding.  This takes precedence over all other options.
        /// </summary>
        public Encoding OutputEncoding { get; init; }

        public BomPolicy OutputBomPolicy { get; init; }

        public bool TrimLines { get; init; } = false;

        public async Task<bool> TransformFileAsync(EncodingInformation encoding, Stream inputStream, Stream outputStream)
        {
            using var inputHash = SHA256.Create();
            using var outputHash = SHA256.Create();
            await using (var inputFilter = new CryptoStream(inputStream, inputHash, CryptoStreamMode.Read))
            {
                await using var outputFilter = new CryptoStream(outputStream, outputHash, CryptoStreamMode.Write);
                using var tr = new StreamReader(inputFilter, encoding.Encoding);

                var outputEncoding = GetOutputEncoding(encoding);

                await using var sw = new StreamWriter(outputFilter, outputEncoding, 4096, true);
                await TransformCoreAsync(tr, sw).ConfigureAwait(false);
            }

            // Only return "true" if the files are the same (at least, they have the same SHA-256).
            return !inputHash.Hash.SequenceEqual(outputHash.Hash);
        }

        Encoding GetOutputEncoding(EncodingInformation sourceEncodingInformation)
        {
            var outputEncoding = OutputEncoding;

            if (null != outputEncoding)
                return outputEncoding;

            switch (OutputBomPolicy)
            {
                case BomPolicy.Never:
                    return Utf8WithoutBom;
                case BomPolicy.CopySource:
                    return sourceEncodingInformation.BomDetected ? Utf8WithBom : Utf8WithoutBom;
                case BomPolicy.CopyUtf8OrForce:
                    if (sourceEncodingInformation.Encoding.CodePage == Encoding.UTF8.CodePage && !sourceEncodingInformation.BomDetected)
                        return Utf8WithoutBom;

                    break;
                case BomPolicy.Force:
                default:
                    break;
            }

            return Utf8WithBom;
        }

        async Task TransformCoreAsync(TextReader reader, TextWriter writer)
        {
            var writerBlock = new ActionBlock<string>(writer.WriteLineAsync);

            for (; ; )
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (null == line)
                    break;

                line = line.Normalize(NormalizationForm.FormC);

                if (TrimLines)
                    line = line.TrimEnd();

                writerBlock.Post(line);
            }

            writerBlock.Complete();

            await writerBlock.Completion.ConfigureAwait(false);
        }
    }
}
