using Antlr4.Runtime;
using System;
using System.IO;
using System.Linq;
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

            var fileContent = File.ReadAllText(filePath);
            var inputStream = new AntlrInputStream(fileContent);
            var lexer = new ANTLRTest.CompilerLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);

            SaveTokensToFile(commonTokenStream, "../../../tokens.txt");
            commonTokenStream.Seek(0);

            var parser = new CompilerParser(commonTokenStream);
            var context = parser.program();

            var visitor = new EvalVisitor();
            visitor.Evaluate(context);

            SaveGlobalVariablesToFile(visitor, "../../../global_variables.txt");

            Console.WriteLine("Parsing completed.");
            Console.WriteLine("\nParse Tree:");
            Console.WriteLine(PrettyPrintTree(context.ToStringTree(parser)));

            Console.WriteLine("\nVariables:");
            var variables = visitor.Variables;
            foreach (var kvp in variables)
            {
                Console.WriteLine($"  {kvp.Key} = {kvp.Value.Value} (Type: {kvp.Value.Type}, Scope: {kvp.Value.Scope})");
            }
        }

        private static void SaveGlobalVariablesToFile(EvalVisitor visitor, string outputPath)
        {
            var globalVars = visitor.Variables
                .Where(kvp => kvp.Value.Scope == "global")
                .Select(kvp => $"Variable: {kvp.Key}, Type: {kvp.Value.Type}, Initial Value: {kvp.Value.Value}");

            File.WriteAllLines(outputPath, globalVars);
            Console.WriteLine($"Global variables saved to '{outputPath}'");
        }

        private static void SaveTokensToFile(CommonTokenStream tokens, string outputPath)
        {
            tokens.Fill();
            var tokenTuples = tokens.GetTokens().Select(t =>
            {
                string tokenName = CompilerLexer.DefaultVocabulary.GetSymbolicName(t.Type) ?? "EOF";
                string lexem = t.Text.Replace("\r", "\\r").Replace("\n", "\\n");
                int lineIndex = t.Line;
                return $"<{tokenName}, {lexem}, {lineIndex}>";
            });
            File.WriteAllLines(outputPath, tokenTuples);
            Console.WriteLine($"Tokens saved to '{outputPath}'");
        }

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