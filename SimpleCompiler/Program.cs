using System;

namespace SimpleCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var lexer = new SimpleLexer("1 + 2 * 3 + 4;");
            var parser = new Parser( lexer, new ConsoleOutput() );
            parser.Statement();
        }

        private class ConsoleOutput : ICodeOutputDelegate
        {
            public void Output( string code )
            {
                Console.WriteLine(code);
            }
        }
    }

    
}

