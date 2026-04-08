namespace SV22T1020292.DataLayers.SQLServer
{
    internal static class SqlLike
    {
        /// <summary>
        /// Tạo pattern LIKE an toàn (escape %, _, [).
        /// </summary>
        public static string Pattern(string? search)
        {
            if (string.IsNullOrWhiteSpace(search)) return "";
            var s = search.Trim();
            s = s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
            return "%" + s + "%";
        }
    }
}
