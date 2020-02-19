using System.Collections.Generic;
using System.IO;

namespace FixedWidthFileUtils.Utilities
{
    /// <summary>
    /// BufferedStreamReader that allows peeking at the next line from a StreamReader without 'consuming' it.
    /// </summary>
    public class BufferedStreamReader : StreamReader
    {
        private readonly Queue<string> _buffer = new Queue<string>();
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
            if (_buffer.Count > 0)
                return _buffer.Peek();

            var line = base.ReadLine();
            if (line == null) return null;

            _buffer.Enqueue(line);
            return line;
        }
        /// <summary>
        /// If we consumed a line and need to reverse that, requeue it.
        /// </summary>
        /// <param name="line"></param>
        public void RequeueLine(string line)
        {
            _buffer.Enqueue(line);
        }
        /// <summary>
        /// Reads the next line from the Stream
        /// </summary>
        /// <returns>Next line</returns>
        public override string ReadLine()
        {
            return _buffer.Count > 0 ? _buffer.Dequeue() : base.ReadLine();
        }
        /// <summary>
        /// Is the Stream at the end?
        /// </summary>
        public new bool EndOfStream => _buffer.Count == 0 && base.EndOfStream;
    }
}
