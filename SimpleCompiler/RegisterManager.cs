using System;

namespace SimpleCompiler
{
    internal class RegisterManager
    {
        internal static readonly RegisterManager Instance = new RegisterManager();
        private RegisterManager() { }

        private int _index;
        private readonly string[] _registers = {"t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8", "t9"};

        internal string Pop()
        {
            if(_index + 1 > _registers.Length)
                throw new Exception("[Internal] Register stack is empty.");
            return _registers[_index++];
        }

        internal void Push(string register)
        {
            --_index;
            if(_index < 0)
                throw new Exception( "[Internal] Register stack underflow." );
            if ( _registers[_index] != register )
                throw new Exception( $"[Internal] Unexpected register push to stack: {{{register}}}." );
        }
    }
}
