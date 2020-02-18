using System.Collections.Generic;
using System.IO;

namespace FixedWidthFileUtils
{
    /// <summary>
    /// BufferedStreamReader that allows peeking at the next line from a StreamReader without 'consuming' it.
    /// </summary>
    public class BufferedStreamReader : StreamReader
    {
        private Queue<string> Buffer = new Queue<string>();
        public int CurrentLine { get; private set; } = 0;
        /// <summary>
        /// Creates a new BufferedStreamReader from a Stream
        /// </summary>
        /// <param name="s">Stream to use</param>
        public BufferedStreamReader(Stream s) : base(s) { }
        /// <summary>
        /// Peeks the next line without 'consuming' it
        /// </summary>
        /// <returns>Next line in stream</returns>
        public string PeekLine()
        {
            if (Buffer.Count > 0)
                return Buffer.Peek();

            string line = base.ReadLine();
            if (line == null) return null;

            Buffer.Enqueue(line);
            return line;
        }
        /// <summary>
        /// If we consumed a line and need to reverse that, requeue it.
        /// </summary>
        /// <param name="line"></param>
        public void RequeueLine(string line)
        {
            CurrentLine--;
            Buffer.Enqueue(line);
        }
        /// <summary>
        /// Reads the next line from the Stream
        /// </summary>
        /// <returns>Next line</returns>
        public override string ReadLine()
        {
            CurrentLine++;
            if (Buffer.Count > 0)
                return Buffer.Dequeue();
            return base.ReadLine();
        }
        /// <summary>
        /// Is the Stream at the end?
        /// </summary>
        public new bool EndOfStream => Buffer.Count == 0 && base.EndOfStream;
    }
}
