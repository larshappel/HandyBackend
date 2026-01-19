using System.IO;
using System.Text;
using Serilog.Sinks.File;

namespace HandyBackend.Logging
{
    /// <summary>
    /// A Serilog file lifecycle hook that writes a CSV header to a log file
    /// only when the file is first created.
    /// </summary>
    public class CsvHeaderHooks : FileLifecycleHooks
    {
        /// <summary>
        /// Overrides the OnFileOpened event to write a header if the file is new.
        /// </summary>
        /// <param name="path">The full path to the log file.</param>
        /// <param name="underlyingStream">The underlying file stream.</param>
        /// <param name="encoding">The encoding used by the sink.</param>
        /// <returns>The stream Serilog should write to.</returns>
        public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
        {
            // If the file is newly created (i.e., empty), write the CSV header.
            if (underlyingStream.Length == 0)
            {
                // Use a StreamWriter to write the header, ensuring we use the correct encoding
                // and leave the underlying stream open for Serilog to write to.
                using (
                    var writer = new StreamWriter(underlyingStream, encoding, 1024, leaveOpen: true)
                )
                {
                    writer.WriteLine("Timestamp, Product_ID, Amount, Individual_ID, Message");
                }
            }

            // Return the original stream for Serilog to use.
            return underlyingStream;
        }
    }
}
