using System;
using Runtime;

namespace SimpleCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var lexer = new SimpleLexer("2 * 3 * 4;");
            //var parser = new Parser( lexer, new ConsoleOutput() );

            var output = new RuntimeOutput();
            var parser = new Parser( lexer, output );
            parser.Statement();

            PseudoRuntime.Run( output.Result );
        }

        private class ConsoleOutput : ICodeOutputDelegate
        {
            public void Output( string code )
            {
                Console.WriteLine(code);
            }
        }

        private class RuntimeOutput : ICodeOutputDelegate
        {
            internal string Result { get; private set; } = string.Empty;

            public void Output( string code )
            {
                Result += code + Environment.NewLine;
            }
        }
    }

    
}

