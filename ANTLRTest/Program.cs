using Antlr4.Runtime;
using System;
using System.IO;
using System.Text;

namespace ANTLRTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var fileName = "../../../input.vex";
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: Input file not found at '{filePath}'");
                return;
            }

            // 1. Read the file content
            var fileContent = File.ReadAllText(filePath);

            // 2. Create the input stream
            var inputStream = new AntlrInputStream(fileContent);

            // 3. Create the lexer
            var lexer = new ANTLRTest.CompilerLexer(inputStream);

            // 4. Create the token stream
            var commonTokenStream = new CommonTokenStream(lexer);

            // 5. Create the parser
            var parser = new CompilerParser(commonTokenStream);

            // 6. Start parsing from the 'program' rule (the root rule in your grammar)
            var context = parser.program();

            // 7. Visit the parse tree with EvalVisitor
            var visitor = new EvalVisitor();
            visitor.Evaluate(context);

            Console.WriteLine("Parsing completed.");
            // Console.WriteLine(context.ToStringTree(parser)); // Print the LISP-style tree

            Console.WriteLine("\nParse Tree:");
            Console.WriteLine(PrettyPrintTree(context.ToStringTree(parser)));


            // 8. Print evaluated variables
            Console.WriteLine("\nVariables:");
            foreach (var kvp in visitor.Variables)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value}");
            }
        }

        /// <summary>
        /// Formats a LISP-style tree string with proper indentation.
        /// </summary>
        private static string PrettyPrintTree(string tree)
        {
            var sb = new StringBuilder();
            int indentLevel = 0;
            const string indent = "  ";

            for (int i = 0; i < tree.Length; i++)
            {
                char c = tree[i];

                if (c == '(')
                {
                    if (i > 0 && tree[i - 1] != '(' && tree[i - 1] != ' ')
                    {
                        sb.AppendLine();
                        sb.Append(string.Concat(Enumerable.Repeat(indent, indentLevel)));
                    }
                    sb.Append(c);
                    indentLevel++;
                    sb.AppendLine();
                    sb.Append(string.Concat(Enumerable.Repeat(indent, indentLevel)));
                }
                else if (c == ')')
                {
                    indentLevel--;
                    sb.AppendLine();
                    sb.Append(string.Concat(Enumerable.Repeat(indent, indentLevel)));
                    sb.Append(c);
                }
                else if (c == ' ' && i + 1 < tree.Length && tree[i + 1] == '(')
                {
                    // Skip space before opening paren
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}