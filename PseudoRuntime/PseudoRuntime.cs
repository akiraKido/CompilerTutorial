using System;
using System.IO;

namespace Runtime
{
    public static class PseudoRuntime
    {
        private static readonly float[] Registers = new float[10];

        public static void Run( string s )
        {
            using ( var reader = new StringReader( s ) )
            {
                string line;
                while ( (line = reader.ReadLine()) != null )
                {
                    var components = line.Split( new []{' '}, StringSplitOptions.RemoveEmptyEntries );
                    switch ( components.Length )
                    {
                        case 3:
                            var register = GetRegisterNum( components[0] );
                            Registers[register] = float.Parse( components[2] );
                            continue;
                        case 5:
                            var inputRegister = GetRegisterNum( components[0] );
                            var register1 = GetRegisterNum( components[2] );
                            var register2 = GetRegisterNum( components[4] );
                            switch ( components[3] )
                            {
                                case "+":
                                    Registers[inputRegister] = Registers[register1] + Registers[register2];
                                    break;
                                case "*":
                                    Registers[inputRegister] = Registers[register1] * Registers[register2];
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            continue;
                    }
                    throw new NotImplementedException();
                }
            }
            Console.WriteLine(Registers[0]);
        }

        private static int GetRegisterNum(string s)
        {
            return (int) char.GetNumericValue( s[1] );
        }
    }
}
