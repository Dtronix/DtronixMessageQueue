using System;
using System.IO;
using System.Text;

namespace DtronixMessageQueue.Tests.Performance
{
    class ConsoleCopy : IDisposable
    {
        FileStream _fileStream;
        StreamWriter _fileWriter;
        TextWriter _doubleWriter;
        TextWriter _oldOut;

        class DoubleWriter : TextWriter
        {
            TextWriter _one;
            TextWriter _two;

            public DoubleWriter(TextWriter one, TextWriter two)
            {
                _one = one;
                _two = two;
            }

            public override Encoding Encoding
            {
                get { return _one.Encoding; }
            }

            public override void Flush()
            {
                _one.Flush();
                _two.Flush();
            }

            public override void Write(char value)
            {
                _one.Write(value);
                _two.Write(value);
            }
        }

        public ConsoleCopy(string path)
        {
            _oldOut = Console.Out;

            try
            {
                _fileStream = File.Create(path);

                _fileWriter = new StreamWriter(_fileStream);
                _fileWriter.AutoFlush = true;

                _doubleWriter = new DoubleWriter(_fileWriter, _oldOut);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open file for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(_doubleWriter);
        }

        public void Dispose()
        {
            Console.SetOut(_oldOut);
            if (_fileWriter != null)
            {
                _fileWriter.Flush();
                _fileWriter.Close();
                _fileWriter = null;
            }
            if (_fileStream != null)
            {
                _fileStream.Close();
                _fileStream = null;
            }
        }
    }
}