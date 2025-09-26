namespace PackageScanner
{
    public class DependantAssembly
    {
        public string Name { get; set; }
        public string Token { get; set; }
        public string Culture { get; set; }
        public string OldVersion { get; set; }
        public string NewVersion { get; set; }

        public int UsageCount { get; set; }
    }
}