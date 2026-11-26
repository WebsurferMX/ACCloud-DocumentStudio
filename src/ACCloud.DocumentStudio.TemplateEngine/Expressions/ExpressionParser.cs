using System.Globalization;
using System.Text;
using ACCloud.DocumentStudio.Renderer;

namespace ACCloud.DocumentStudio.TemplateEngine.Expressions;

internal abstract record Expr;

internal sealed record LiteralExpr(object? Value) : Expr;

internal sealed record PathExpr(IReadOnlyList<PathSegment> Segments) : Expr;

internal sealed record CallExpr(string Name, IReadOnlyList<Expr> Arguments) : Expr;

/// <summary>Un tramo de ruta: nombre de propiedad o índice de arreglo.</summary>
internal readonly record struct PathSegment(string? Name, int? Index);

/// <summary>
/// Parser de expresiones (lo que va dentro de <c>{{ }}</c>).
/// <code>
/// expr  := call | path | string | number | true | false | null
/// call  := ident '(' [expr (',' expr)*] ')'
/// path  := ident ('.' ident | '[' int ']')*
/// </code>
/// </summary>
internal static class ExpressionParser
{
    public static Expr Parse(string text)
    {
        var parser = new Parser(text);
        var expression = parser.ParseExpression();

        parser.SkipWhitespace();

        if (!parser.AtEnd)
        {
            throw parser.Error("contenido inesperado después de la expresión");
        }

        return expression;
    }

    private sealed class Parser
    {
        private readonly string _text;
        private int _position;

        public Parser(string text)
        {
            _text = text;
        }

        public bool AtEnd => _position >= _text.Length;

        private char Current => _text[_position];

        public void SkipWhitespace()
        {
            while (!AtEnd && char.IsWhiteSpace(Current))
            {
                _position++;
            }
        }

        public DocumentValidationException Error(string message)
        {
            return new DocumentValidationException(
                $"Expresión inválida '{_text}': {message} (posición {_position + 1}).");
        }

        public Expr ParseExpression()
        {
            SkipWhitespace();

            if (AtEnd)
            {
                throw Error("se esperaba un valor");
            }

            var c = Current;

            if (c is '"' or '\'')
            {
                return new LiteralExpr(ParseString(c));
            }

            if (char.IsDigit(c)
                || (c == '-' && _position + 1 < _text.Length && char.IsDigit(_text[_position + 1])))
            {
                return new LiteralExpr(ParseNumber());
            }

            if (!IsIdentifierStart(c))
            {
                throw Error($"carácter inesperado '{c}'");
            }

            var name = ParseIdentifier();

            SkipWhitespace();

            if (!AtEnd && Current == '(')
            {
                _position++;

                return new CallExpr(name, ParseArguments());
            }

            return name switch
            {
                "true" => new LiteralExpr(true),
                "false" => new LiteralExpr(false),
                "null" => new LiteralExpr(null),
                _ => ParsePath(name)
            };
        }

        private List<Expr> ParseArguments()
        {
            var arguments = new List<Expr>();

            SkipWhitespace();

            if (!AtEnd && Current == ')')
            {
                _position++;

                return arguments;
            }

            while (true)
            {
                arguments.Add(ParseExpression());

                SkipWhitespace();

                if (AtEnd)
                {
                    throw Error("falta ')'");
                }

                if (Current == ',')
                {
                    _position++;
                    continue;
                }

                if (Current == ')')
                {
                    _position++;

                    return arguments;
                }

                throw Error($"se esperaba ',' o ')' y se encontró '{Current}'");
            }
        }

        private PathExpr ParsePath(string first)
        {
            var segments = new List<PathSegment> { new(first, null) };

            while (!AtEnd)
            {
                if (Current == '.')
                {
                    _position++;

                    if (AtEnd || !IsIdentifierStart(Current))
                    {
                        throw Error("se esperaba un nombre después de '.'");
                    }

                    segments.Add(new PathSegment(ParseIdentifier(), null));
                }
                else if (Current == '[')
                {
                    _position++;

                    var start = _position;

                    while (!AtEnd && char.IsDigit(Current))
                    {
                        _position++;
                    }

                    if (start == _position || AtEnd || Current != ']')
                    {
                        throw Error("índice inválido, se esperaba [número]");
                    }

                    var index = int.Parse(_text.AsSpan(start, _position - start), CultureInfo.InvariantCulture);
                    _position++;

                    segments.Add(new PathSegment(null, index));
                }
                else
                {
                    break;
                }
            }

            return new PathExpr(segments);
        }

        private string ParseIdentifier()
        {
            var start = _position;

            while (!AtEnd && IsIdentifierPart(Current))
            {
                _position++;
            }

            return _text[start.._position];
        }

        private string ParseString(char quote)
        {
            _position++;

            var builder = new StringBuilder();

            while (!AtEnd)
            {
                var c = Current;
                _position++;

                if (c == '\\' && !AtEnd)
                {
                    builder.Append(Current);
                    _position++;
                }
                else if (c == quote)
                {
                    return builder.ToString();
                }
                else
                {
                    builder.Append(c);
                }
            }

            throw Error("cadena sin cerrar");
        }

        private decimal ParseNumber()
        {
            var start = _position;

            if (Current == '-')
            {
                _position++;
            }

            while (!AtEnd && (char.IsDigit(Current) || Current == '.'))
            {
                _position++;
            }

            if (!decimal.TryParse(
                    _text.AsSpan(start, _position - start),
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                throw Error("número inválido");
            }

            return value;
        }

        private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

        private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';
    }
}
