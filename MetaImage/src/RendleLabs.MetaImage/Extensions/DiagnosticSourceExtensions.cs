using System.Diagnostics;

namespace RendleLabs.MetaImage.Extensions
{
    public static class DiagnosticSourceExtensions
    {
        public static DiagnosticSource IfEnabled(this DiagnosticSource source, string name)
            => source.IsEnabled(name) ? source : null;
    }
}