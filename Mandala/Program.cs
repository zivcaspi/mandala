using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandala
{
    class Program
    {
        static object s_lock = new object();
        static int s_errors = 0;

        // TODO: Why this doesn't work at the moment:
        // 1. If the app intermixes writing to stderr and stdout, we have no way to order them
        //    (nslookup does this, for example)
        // 2. If we create a single pipe for both stderr and stdout, we have no way to tell which
        //    of them produced the info
        // 3. There seem to be a bug in .NET WRT newlines -- they are elided
        static int Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return Usage();
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = args[0];
            psi.Arguments = string.Join(" ", args.Skip(1).Select(arg => '"' + arg + '"'));
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            try
            {
                using (var process = Process.Start(psi))
                {
                    process.OutputDataReceived += OnStdOut;
                    process.ErrorDataReceived += OnStdErr;
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();
                    if (process.ExitCode > 0)
                    {
                        return process.ExitCode;
                    }
                }
            }
            catch (Exception ex)
            {
                return ErrorLine(ex.Message);
            }

            return Math.Min(s_errors, 100); // If error count exceeds 100, don't write it as some apps can't handle > 127
        }

        static void OnStdOut(object sender, DataReceivedEventArgs e)
        {
            Info(e.Data);
        }

        static void OnStdErr(object sender, DataReceivedEventArgs e)
        {
            Error(e.Data);
        }

        static int Usage()
        {
            Console.WriteLine(@"
Mandala:

  Run a console app and color its output.

Synopsis:

  Mandala.exe [<Options>] <CommandLineToRun>

");
            return 0;
        }

        static void Info(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                return;
            }

            if (msg.IndexOf("Err", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // So that we will write to stderr
                Error(msg);
                return;
            }

            var color = ConsoleColor.White;
            if (msg.IndexOf("Wrn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                msg.IndexOf("Warn", StringComparison.OrdinalIgnoreCase) >= 0 )
            {
                color = ConsoleColor.Yellow;
            }

            lock (s_lock)
            {
                Console.ForegroundColor = color;
                Console.Write(msg);
            }
        }

        static int Error(string msg, int code = 123)
        {
            if (string.IsNullOrEmpty(msg))
            {
                return 0;
            }

            lock (s_lock)
            {
                s_errors++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write(msg);
            }

            return code;
        }

        static int ErrorLine(string msg, int code = 123)
        {
            if (string.IsNullOrEmpty(msg))
            {
                return 0;
            }

            lock (s_lock)
            {
                s_errors++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(msg);
            }

            return code;
        }
    }
}
