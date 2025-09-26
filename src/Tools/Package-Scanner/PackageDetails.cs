namespace PackageScanner
{
    public class PackageDetails
    {
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public string PackageLicence { get; set; }
        public int UsageCount { get; set; }

        public override string ToString()
        {
            return $"|{PackageName}|{PackageVersion}|{PackageLicence}|{UsageCount}|";
        }
    }
}