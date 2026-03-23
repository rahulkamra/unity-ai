namespace LlmTornado.Code.DiffMatchPatch
{
    internal readonly struct HalfMatchResult
    {
        public string Prefix1 { get; }
        public string Suffix1 { get; }
        public string Prefix2 { get; }
        public string Suffix2 { get; }
        public string CommonMiddle { get; }

        public HalfMatchResult(string prefix1, string suffix1, string prefix2, string suffix2, string commonMiddle)
        {
            Prefix1 = prefix1;
            Suffix1 = suffix1;
            Prefix2 = prefix2;
            Suffix2 = suffix2;
            CommonMiddle = commonMiddle;
        }

        public bool IsEmpty => string.IsNullOrEmpty(CommonMiddle);

        public static readonly HalfMatchResult Empty = new HalfMatchResult(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        public static bool operator >(HalfMatchResult left, HalfMatchResult right) => left.CommonMiddle.Length > right.CommonMiddle.Length;

        public static bool operator <(HalfMatchResult left, HalfMatchResult right) => left.CommonMiddle.Length < right.CommonMiddle.Length;
        public static HalfMatchResult operator -(HalfMatchResult item) => new HalfMatchResult(item.Prefix2, item.Suffix2, item.Prefix1, item.Suffix1, item.CommonMiddle);
        public override string ToString() => $"[{Prefix1}/{Prefix2}] - {CommonMiddle} - [{Suffix1}/{Suffix2}]";
    }
}
