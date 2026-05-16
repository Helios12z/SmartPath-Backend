using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SmartPathBackend.Utils
{
    /// <summary>
    /// Helper class to detect if an NVIDIA RTX graphics card exists on this machine.
    /// </summary>
    public static class GpuDetector
    {
        private static bool? _hasRtx = null;

        /// <summary>
        /// Checks if the machine contains at least one video controller whose name includes "RTX".
        /// The result is cached for subsequent calls.
        /// </summary>
        public static bool HasRtxCard()
        {
            if (_hasRtx.HasValue) return _hasRtx.Value;

            // Only perform Windows-specific check.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Default to true on other OS environments to avoid blocking legitimate
                // setups (like Linux containers or cloud environments) without local diagnostics.
                _hasRtx = true;
                return true;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-NoProfile -Command \"Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(TimeSpan.FromSeconds(5));

                    if (output.Contains("RTX", StringComparison.OrdinalIgnoreCase))
                    {
                        _hasRtx = true;
                        return true;
                    }
                }
            }
            catch
            {
                // If powershell query fails, try alternative fallback using cmd wmic
                try
                {
                    var fallbackPsi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c wmic path win32_VideoController get name",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var fallbackProcess = Process.Start(fallbackPsi);
                    if (fallbackProcess != null)
                    {
                        string output = fallbackProcess.StandardOutput.ReadToEnd();
                        fallbackProcess.WaitForExit(TimeSpan.FromSeconds(5));

                        if (output.Contains("RTX", StringComparison.OrdinalIgnoreCase))
                        {
                            _hasRtx = true;
                            return true;
                        }
                    }
                }
                catch
                {
                    // Fallback if all detection attempts fail
                }
            }

            _hasRtx = false;
            return false;
        }
    }
}
