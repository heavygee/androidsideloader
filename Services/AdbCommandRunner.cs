namespace AndroidSideloader.Services
{
    public sealed class AdbCommandRunner : IAdbCommandRunner
    {
        public AdbCommandResult Run(string command)
        {
            ProcessOutput output = ADB.RunAdbCommandToString(command, suppressLogging: true);
            return new AdbCommandResult(output.Output, output.Error);
        }
    }
}
