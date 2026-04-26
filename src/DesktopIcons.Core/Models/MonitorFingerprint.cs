using System.Security.Cryptography;
using System.Text;
using DesktopIcons.Core.Interop;

namespace DesktopIcons.Core.Models;

public static class MonitorFingerprint
{
    public readonly record struct Result(string Fingerprint, string Setup);

    public static Result Compute()
    {
        var monitors = MonitorEnum.GetAll();
        if (monitors.Count == 0)
        {
            return new Result("FP-00000000", "0 monitors");
        }

        var ordered = monitors
            .OrderBy(m => m.Left)
            .ThenBy(m => m.Top)
            .ToList();

        var canonical = new StringBuilder();
        canonical.Append(ordered.Count).Append('|');
        for (int i = 0; i < ordered.Count; i++)
        {
            var m = ordered[i];
            if (i > 0) canonical.Append('|');
            canonical.Append(m.Width).Append('x').Append(m.Height)
                     .Append(m.Left >= 0 ? "+" : "").Append(m.Left)
                     .Append(m.Top  >= 0 ? "+" : "").Append(m.Top);
            if (m.IsPrimary) canonical.Append('P');
        }

        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(canonical.ToString()));
        var shortHash = Convert.ToHexString(hash, 0, 4).ToLowerInvariant();

        var setup = new StringBuilder();
        setup.Append(ordered.Count).Append(ordered.Count == 1 ? " monitor: " : " monitors: ");
        for (int i = 0; i < ordered.Count; i++)
        {
            var m = ordered[i];
            if (i > 0) setup.Append(", ");
            setup.Append(m.Width).Append('x').Append(m.Height)
                 .Append('@').Append('(').Append(m.Left).Append(',').Append(m.Top).Append(')');
            if (m.IsPrimary) setup.Append(" [primary]");
        }

        return new Result($"FP-{shortHash}", setup.ToString());
    }
}
