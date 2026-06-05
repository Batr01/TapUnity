using System;

namespace TapBrawl.Utils
{
    /// <summary>Сравнение версий Major.Minor.Patch.</summary>
    public readonly struct SemVer : IComparable<SemVer>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public SemVer(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public static bool TryParse(string? value, out SemVer version)
        {
            version = default;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var parts = value.Trim().Split('.');
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out var major))
                return false;
            if (!int.TryParse(parts[1], out var minor))
                return false;
            if (!int.TryParse(parts[2], out var patch))
                return false;

            version = new SemVer(major, minor, patch);
            return true;
        }

        public static SemVer Parse(string value)
        {
            if (!TryParse(value, out var version))
                throw new FormatException($"Некорректная версия: {value}");
            return version;
        }

        public int CompareTo(SemVer other)
        {
            var major = Major.CompareTo(other.Major);
            if (major != 0)
                return major;

            var minor = Minor.CompareTo(other.Minor);
            if (minor != 0)
                return minor;

            return Patch.CompareTo(other.Patch);
        }

        public static bool operator <(SemVer left, SemVer right) => left.CompareTo(right) < 0;
        public static bool operator >(SemVer left, SemVer right) => left.CompareTo(right) > 0;
        public static bool operator <=(SemVer left, SemVer right) => left.CompareTo(right) <= 0;
        public static bool operator >=(SemVer left, SemVer right) => left.CompareTo(right) >= 0;

        public override string ToString() => $"{Major}.{Minor}.{Patch}";
    }
}
