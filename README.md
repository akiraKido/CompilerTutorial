なんだかんだで勉強会もだれてきたので（大体教科書のせい）簡単なコンパイラの作り方を書いて、今回の勉強会はいったん終了にしたいと思います。

# コンパイラへの導入

コンパイラとは「特定の言語を指定した目的語へと変換する装置」です。今後コンパイラについて勉強するとき、あまりにも大きくて複雑に見えることで怖気づいてしまうかもしれませんが、概要をいったん理解してしまえば、あとは流れがつかめるかと思います。

## 大まかな流れ

コンパイラの基本的な流れは以下になります。

```
特定の言語の入力
　　　↓
字句解析（Lexing）
　　　↓
構文解析（Parsing）
　　　↓
目的語の出力
```

また、字句解析や構文解析の途中に最適化が行われるケースが多くあります。最適化には様々な種類がありますが、今回最適化については省略します。

上に表した通り、基本的に難しいパーツは字句解析器（Lexer）と構文解析器（Parser)の二つしかありません。ただコードファーストでばばっと書いてしまうと途中でわけがわからなくなります（経験済み）。なので、設計から実装まで全部カバーします。

## 作ってみよう！設計編

今回作るコンパイラでは以下のような結果を目指します。

```
// 入力
1 + 2 * 3 + 4;

// 出力
t0 = 1
t1 = 2
t2 = 3
t1 = t1 * t2
t0 = t0 + t1
t1 = 4
t0 = t0 + t1
```

出力は疑似的なアセンブラと考えてください。`t0` や `t1` はレジスタ、みたいな感じです。

まず設計する上でBNF記法について知っておいてください。

[バッカス・ナウア記法](https://ja.wikipedia.org/wiki/%E3%83%90%E3%83%83%E3%82%AB%E3%82%B9%E3%83%BB%E3%83%8A%E3%82%A6%E3%82%A2%E8%A8%98%E6%B3%95)

以下に示すのはBNF記法を少しいじったものになりますが、感覚的に理解してもらえるかと思います。

```
statement   ::= [(sum ';')+ | ε]
sum         ::= factor [( '+' factor)+ | ε]
factor      ::= number [( '*' number)+ | ε]
number      ::= [0-9]+(\.[0-9]+)?
```

これが今回の言語の定義になります。最初の３つの定義の最後に入ってあるのは ε （イプシロン）です。ε は「何も返さない」という意味です。BNFでは結構これを書かないときがありますが、これを書かないとループしたりしたときに死にます。単純に正規表現みたいに`*`で表しちゃってもいいんですけど明確に「抜けられる」ことがわかる方がいいかな、と。

で、これを入力 `1 + 2 * 3 + 4;` に適用すると、以下のようなツリーになります。

```
       +
   ┌───┴───┐ 
   +       +
 ┌─┴─┐   ┌─┴─┐
 *   *   *   ε
┌┴┐ ┌┴┐ ┌┴┐
1 ε 2 3 4 ε
```

これでなんとなく ε の存在意義がわかりますかね？ 別に無くてもいいけれど、あるとツリーがちょっとわかりやすくなる気がします。これは ε が無いと以下のようになります。

```
     +
   ┌─┴─┐ 
   +   4
 ┌─┴─┐
 1   *
   ┌─┴─┐
   2   3
```

個人的に、これを見るとプログラムでどうやって実現するの？ `switch` 文でループするの？と不安になります。ε がどう動作してくるかは、構文解析器の実装の際に見てみましょう。

では早速、字句解析器を作って行きましょう。

## 作ってみよう！字句解析器編

字句解析器では文字列をトークンに分割していきます。文字列のままでも良いのですが、実際にはトークンの状態で最適化などを行う都合上、トークンにしているとわかりやすいです。また、設計的にも構文解析器と疎結合にすることができ、字句解析器と構文解析器を別々に実装しなおしたりといったことができます。

文字列から取得できるのは以下の４種類のトークンになります。

- 数値
- ＋
- ＊
- ；

これに、初期値を表す「None」と、入力の終了を表す「EndOfInput」を追加します。

```csharp
public enum TokenType
{
    None,
    Add,
    Multiply,
    SemiColon,
    Number,
    EndOfInput
}
```

入力してきた値を、上のEnumで返せれば OK です。

```csharp
internal class SimpleLexer : ILexer
{
    private readonly Regex _numbeRegex = new Regex( @"\G[0-9]+(\.[0-9]+)?", 
                                                    RegexOptions.Compiled );

    private readonly string _text;
    private int _offset;
    private int _length;

    internal SimpleLexer( string text )
    {
        _text = text;
    }

    private TokenType Lex()
    {
        _offset += _length;
        // 空白行を飛ばします
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
                if(match.Length <= 0) throw new Exception("Unexpected Token");
                _length = match.Length;
                return TokenType.Number;
        }
    }
}
```

ちなみに、なるべく文字列をインスタンス化しないように　`_offset` と `_length` を使って後から文字列を引っ張れるようにしてます。これについては後ほど。

基本的には１文字読み込んで、その文字次第で返すトークンを返却しています。数字かどうかに関しては、現在文字列とかも無いので、`default` に来た値をそのまま数値にパースしちゃってます。

で、トークンを取得できるようになったのはいいですが、`private` にしてます。はい、アクセサを実装します。

```csharp
        public string Lexeme => _text.Substring( _offset, _length );

        public bool Match( TokenType token )
        {
            if ( _lookAhead == TokenType.None ) _lookAhead = Lex();
            return token == _lookAhead;
        }

        public void Advance()
        {
            _lookAhead = Lex();
        }
        
        private TokenType _lookAhead = TokenType.None;
```

`Lexeme` とは Lex(字句解析) したときに取得できた文字列です。ほかは自明ですが、最初の `Advance` をせずに `Match` できるよう、最初は無条件に `Lex` を走らせています。以上で字句解析終わりです。早い。

## 作ってみよう！構文解析器編

パーサってやつと作ります。ただ、作る前に一旦図に戻ります。

```
statement   ::= [(sum ';')+ | ε]
sum         ::= factor [( '+' factor)+ | ε]
factor      ::= number [( '*' number)+ | ε]
number      ::= [0-9]+(\.[0-9]+)?

       +
   ┌───┴───┐ 
   +       +
 ┌─┴─┐   ┌─┴─┐
 *   *   *   ε
┌┴┐ ┌┴┐ ┌┴┐
1 ε 2 3 4 ε
```

これ。さっきは ε があることについて便利だと力説していましたが、まあ流れ的には以下のようになるわけです。

```
statement
   │  ↑
   ↓  └──────┐ No
  sum ──→ + ある？
   │↑        │
   ├─────────┘ Yes
   ↓└────────┐ No
 factor → * ある？
   ↓↑        │ Yes
 number ←────┘
```

この「No」の部分が ε です。これ、Noの部分ないと「* ある？」とかの条件子の後どこいきゃいいかわかりづらいですよね。分かりづらくないなら省略してもらっても大丈夫です。。

で、単純にこいつら全部関数にしてやります。No は return、 Yes はループ、または関数呼び出しです。レッツ実装。

```csharp
internal class Parser
    {
        private readonly ILexer _lexer;

        internal Parser( ILexer lexer )
        {
            _lexer = lexer;
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
            Factor();
            while ( _lexer.Match( TokenType.Add ) )
            {
                _lexer.Advance();
                Factor();
            }
        }

        /// <summary>
        /// factor ::= number ( '*' number)*
        /// </summary>
        private void Factor()
        {
            Number();
            while ( _lexer.Match( TokenType.Multiply ) )
            {
                _lexer.Advance();
                Number();
            }
        }

        /// <summary>
        /// number ::= [0-9]+(\.[0-9]*)?
        /// </summary>
        private void Number()
        {
            if ( _lexer.Match( TokenType.Number ) )
            {
                _lexer.Advance();
            }
            throw new Exception($"Number token expected: {_lexer.Lexeme}");
        }
    }
```

Matchして、期待通りだったらトークンをAdvanceして、そうでなければException。簡単ですね。

以上、パーサでした。

## 目的語の吐き出し

最後に目的語の吐き出しを行います。そのためにはスタック的なサムシングを作ってあげなければいけません。以下レジストリを都度取ってくるやつです。

```csharp
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
```

Pop すると新しいレジスタを拾います。Pushすると捨てます。基本ツリーの一番下の方の値はレジスタを拾ってすぐ捨てるはずなのでこの実装で大丈夫なはず。。普通に中Stackにして、取り出せなかったら新しく作る系の実装でも良いと思います。今回はめんどくさかったのでポインタ方式にしました。

後はこれを実装します。

```csharp
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
```

おわり。

## 実行用コード

```csharp
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
```
