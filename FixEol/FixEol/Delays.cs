using System.Security.Cryptography;
using System.Threading.Tasks;

namespace FixEol
{
    public sealed class Delays
    {
        /// <summary>
        ///     We don't need crypto, but it is thread safe and we use it for delays so the extra CPU
        ///     time doesn't hurt.
        /// </summary>
        static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static Task ShortDelay()
        {
            var b = new byte[1];

            Rng.GetBytes(b);

            return Task.Delay(10 + b[0] & 0x3f);
        }
    }
}