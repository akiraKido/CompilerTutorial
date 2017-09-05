using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleCompiler
{
    public enum TokenType
    {
        None,
        Add,
        Multiply,
        SemiColon,
        Number,
        EndOfInput
    }

    public interface ILexer
    {
        string Lexeme { get; }
        bool Match( TokenType token );
        void Advance();
    }

    internal class SimpleLexer : ILexer
    {
        private readonly Regex _numbeRegex = new Regex( @"\G[0-9]+(\.[0-9]+)?", RegexOptions.Compiled );

        private readonly string _text;
        private int _offset;
        private int _length;
        public string Lexeme => _text.Substring( _offset, _length );

        private TokenType _lookAhead = TokenType.None;

        internal SimpleLexer( string text )
        {
            _text = text;
        }

        public bool Match( TokenType token )
        {
            if ( _lookAhead == TokenType.None ) _lookAhead = Lex();
            return token == _lookAhead;
        }

        public void Advance()
        {
            _lookAhead = Lex();
        }

        private TokenType Lex()
        {
            _offset += _length;
            while ( _offset < _text.Length && char.IsWhiteSpace( _text[_offset] ) )
            {
                _offset++;
            }

            if(_offset >= _text.Length) return TokenType.EndOfInput;

            _length = 1;
            switch ( _text[_offset] )
            {
                case '+':
                    return TokenType.Add;
                case '*':
                    return TokenType.Multiply;
                case ';':
                    return TokenType.SemiColon;
                default:
                    var match = _numbeRegex.Match( _text, _offset );
                    if(match.Length <= 0) throw new Exception($"Unexpected token at {_offset}: {_text[_offset]}");
                    _length = match.Length;
                    return TokenType.Number;
            }
        }
    }
}
