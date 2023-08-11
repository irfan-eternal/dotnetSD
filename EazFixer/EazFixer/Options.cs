using CommandLine;

namespace EazFixer
{
    class Options
    {
        [Option("file",
            Default = false,
            Required = true)]
        public string InFile { get; set; }

        [Option("out")]
        public string OutFile { get; set; }

        [Option("keep-types")]
        public bool KeepTypes { get; set; }

        [Option("virt-fix")]
        public bool VirtFix { get; set; }

        [Option("type",
            Default = false,
            Required = true)]
        public string type { get; set; }

        [Option("method",
            Default = false,
            Required = true)]
        public string method { get; set; }
    }
}
