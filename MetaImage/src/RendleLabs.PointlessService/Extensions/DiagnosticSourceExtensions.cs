using System.Diagnostics;

namespace RendleLabs.PointlessService.Extensions
{
    public static class DiagnosticSourceExtensions
    {
        public static MaybeDiagnosticSource? IfEnabled(this DiagnosticSource source, string name)
            => source.IsEnabled(name) ? new MaybeDiagnosticSource(name, source) : (MaybeDiagnosticSource?)null;

        public readonly struct MaybeDiagnosticSource
        {
            private readonly string _name;
            private readonly DiagnosticSource _source;

            public MaybeDiagnosticSource(string name, DiagnosticSource source)
            {
                _name = name;
                _source = source;
            }

            public void Write(object args)
            {
                _source.Write(_name, args);
            }
        }
    }
}