using System;
using System.Diagnostics;

namespace SimpleCompiler
{
    public interface ICodeOutputDelegate
    {
        void Output( string code );
    }

    /// <summary>
    /// statement   ::= sum ';' [ (sum ';')* | END_OF_INPUT ]
    /// sum         ::= factor ( '+' factor)*
    /// factor      ::= number ( '*' number)*
    /// number      ::= [0-9]+(\.[0-9]*)?
    /// </summary>
    internal class Parser
    {
        private readonly ILexer _lexer;
        private readonly ICodeOutputDelegate _outputDelegate;
        private readonly RegisterManager _register = RegisterManager.Instance;

        internal Parser( ILexer lexer, ICodeOutputDelegate outputDelegate )
        {
            _lexer = lexer;
            _outputDelegate = outputDelegate;
        }

        /// <summary>
        /// statement ::= sum ';' (sum ';')*
        /// </summary>
        internal void Statement()
        {
            while ( !_lexer.Match( TokenType.EndOfInput ) )
            {
                Sum();
                if ( _lexer.Match( TokenType.SemiColon ) )
                {
                    _lexer.Advance();
                    continue;
                }
                throw new Exception( "Expected semicolon." );
            }
        }

        /// <summary>
        /// sum ::= factor ( '+' factor)*
        /// </summary>
        private void Sum()
        {
            var register1 = Factor();
            while ( _lexer.Match( TokenType.Add ) )
            {
                _lexer.Advance();
                var register2 = Factor();
                _outputDelegate.Output( $"{register1} = {register1} + {register2}" );
                _register.Push( register2 );
            }
            _register.Push( register1 );
        }

        /// <summary>
        /// factor ::= number ( '*' number)*
        /// </summary>
        private string Factor()
        {
            var register1 = Number();
            while ( _lexer.Match( TokenType.Multiply ) )
            {
                _lexer.Advance();
                var register2 = Number();
                _outputDelegate.Output( $"{register1} = {register1} * {register2}" );
                _register.Push( register2 );
            }
            return register1;
        }

        /// <summary>
        /// number ::= [0-9]+(\.[0-9]*)?
        /// </summary>
        private string Number()
        {
            if ( _lexer.Match( TokenType.Number ) )
            {
                var register = _register.Pop();
                _outputDelegate.Output( $"{register} = {_lexer.Lexeme}" );
                _lexer.Advance();
                return register;
            }
            throw new Exception($"Number token expected: {_lexer.Lexeme}");
        }
    }
}
