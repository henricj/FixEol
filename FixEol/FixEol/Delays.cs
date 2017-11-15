using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FixEol
{
    public static class Delays
    {
        /// <summary>
        ///     We don't need crypto, but it is thread safe and we use it for delays so the extra CPU
        ///     time doesn't hurt too much.
        /// </summary>
        static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static Task ShortDelay()
        {
            var b = new byte[1];

            Rng.GetBytes(b);

            return Task.Delay((10 + b[0]) & 0x3f);
        }

        public static Task LongDelay(TimeSpan baseDelay, CancellationToken cancellationToken)
        {
            var b = new byte[1];

            Rng.GetBytes(b);

            var delay = baseDelay.Add(TimeSpan.FromMilliseconds(b[0]));

            return Task.Delay(delay, cancellationToken);
        }
    }
}
