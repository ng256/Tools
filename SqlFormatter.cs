using System;
using System.Text.RegularExpressions;
using System.Text;

class SqlFormatter
{
    // Regular expression for tokenizing SQL queries
    private static readonly string TokenRegex = @"
        (?<Keyword>\b(SELECT|FROM|WHERE|GROUP BY|ORDER BY|INSERT INTO|UPDATE|DELETE|VALUES|INNER JOIN|LEFT JOIN|RIGHT JOIN|ON|AS|AND|OR|NOT|IN|IS|NULL|LIKE|DISTINCT|LIMIT|OFFSET)\b)
        |(?<String>'(?:''|[^'])*')   // Matches SQL string literals
        |(?<Number>\b\d+(\.\d+)?\b)   // Matches numeric values
        |(?<Identifier>\b[a-zA-Z_][a-zA-Z0-9_]*\b)   // Matches table or column identifiers
        |(?<Operator>[<>!=]+|[+\-*/%])   // Matches operators
        |(?<Punctuation>[(),.])   // Matches punctuation marks (brackets and commas)
        |(?<Whitespace>\s+)   // Matches whitespace characters
    ";

    // Enum for SQL keywords
    public enum SqlKeyWord
    {
        Select,
        From,
        Where,
        GroupBy,
        OrderBy,
        InsertInto,
        Update,
        Delete,
        Values,
        InnerJoin,
        LeftJoin,
        RightJoin,
        On,
        As,
        And,
        Or,
        Not,
        In,
        Is,
        Null,
        Like,
        Distinct,
        Limit,
        Offset
    }

    // Settings structure for controlling the pretty output format
    public struct PrettyOutputSettings
    {
        public int IndentSize;  // Number of spaces for indentation
        public bool NewLineBeforeKeywords;  // Whether to start a new line before each keyword
        public bool UppercaseKeywords;  // Whether to convert keywords to uppercase
        public SqlKeyWord[] KeywordsOnNewLine;  // Keywords that should appear on a new line
        public SqlKeyWord[] IndentAfterKeywords;  // Keywords that should increase the indentation level after them
    }

    // Method to format SQL query with specified settings
    public static string PrettyOutput(string sql, PrettyOutputSettings settings)
    {
        // Create regex object using the tokenization regex
        var regex = new Regex(TokenRegex, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        var matches = regex.Matches(sql);

        var sb = new StringBuilder();  // StringBuilder for efficient string concatenation
        int indentLevel = 0;  // Keeps track of the current indentation level
        string indent = new string(' ', settings.IndentSize);  // Indentation string based on specified size

        // Iterate over all matches from the regular expression
        foreach (Match match in matches)
        {
            if (match.Groups["Keyword"].Success)
            {
                string keyword = match.Groups["Keyword"].Value;
                string keywordUpperCase = keyword.ToUpper();  // Convert keyword to uppercase if needed
                string keywordEnumName = keywordUpperCase.Replace(" ", "").Replace("_", "");

                // Try to parse the keyword as an enum
                if (Enum.TryParse(keywordEnumName, ignoreCase: true, result: out SqlKeyWord keywordEnum))
                {
                    // If uppercase is enabled, make keyword uppercase
                    if (settings.UppercaseKeywords)
                    {
                        keyword = keywordUpperCase;
                    }

                    // If the keyword is in the "KeywordsOnNewLine" list, move it to a new line
                    if (Array.Exists(settings.KeywordsOnNewLine, kw => kw == keywordEnum))
                    {
                        if (settings.NewLineBeforeKeywords)
                        {
                            sb.AppendLine();  // Start a new line before the keyword
                        }
                        sb.Append(new string(' ', indentLevel * settings.IndentSize));  // Add indentation
                    }

                    sb.Append(keyword);  // Append the keyword

                    // If the keyword is in the "IndentAfterKeywords" list, increase the indentation level
                    if (Array.Exists(settings.IndentAfterKeywords, kw => kw == keywordEnum))
                    {
                        sb.AppendLine();  // Add a line break after the keyword
                        indentLevel++;  // Increase the indentation level
                        sb.Append(new string(' ', indentLevel * settings.IndentSize));  // Add new indentation
                    }
                }
                else
                {
                    sb.Append(keyword);  // If it's not an enum, just append the keyword as it is
                }
            }
            // Handle other token types such as string, number, identifier, operator, punctuation, and whitespace
            else if (match.Groups["String"].Success)
            {
                sb.Append(" ").Append(match.Groups["String"].Value);  // Append string literal
            }
            else if (match.Groups["Number"].Success)
            {
                sb.Append(" ").Append(match.Groups["Number"].Value);  // Append number
            }
            else if (match.Groups["Identifier"].Success)
            {
                sb.Append(" ").Append(match.Groups["Identifier"].Value);  // Append identifier (table/column name)
            }
            else if (match.Groups["Operator"].Success)
            {
                sb.Append(" ").Append(match.Groups["Operator"].Value);  // Append operator
            }
            else if (match.Groups["Punctuation"].Success)
            {
                string punctuation = match.Groups["Punctuation"].Value;
                sb.Append(punctuation);  // Append punctuation (e.g., parentheses, commas)

                // Handle indentation around punctuation
                if (punctuation == "(")
                {
                    indentLevel++;  // Increase indentation after opening parenthesis
                }
                else if (punctuation == ")")
                {
                    indentLevel = Math.Max(0, indentLevel - 1);  // Decrease indentation after closing parenthesis
                    sb.AppendLine();  // Add a line break after closing parenthesis
                    sb.Append(new string(' ', indentLevel * settings.IndentSize));  // Apply new indentation
                }
            }
            else if (match.Groups["Whitespace"].Success)
            {
                // Ignore whitespace in the tokenized output (handled implicitly by the regex)
            }
        }

        return sb.ToString().Trim();  // Remove any leading/trailing whitespace
    }

    // Example usage of the PrettyOutput method
    static void Main()
    {
        // A complex SQL query for testing the pretty output functionality
        string sql = @"
            SELECT orders.id, customers.name, SUM(orders.amount) AS total_amount 
            FROM orders 
            INNER JOIN customers ON orders.customer_id = customers.id 
            WHERE orders.date >= '2023-01-01' 
            GROUP BY orders.id, customers.name 
            HAVING total_amount > 1000 
            ORDER BY total_amount DESC";

        // Configure settings for pretty output formatting
        var settings = new PrettyOutputSettings
        {
            IndentSize = 4,  // Indentation size of 4 spaces
            NewLineBeforeKeywords = true,  // Start a new line before each keyword
            UppercaseKeywords = true,  // Convert keywords to uppercase
            KeywordsOnNewLine = new[]
            {
                SqlKeyWord.Select,
                SqlKeyWord.From,
                SqlKeyWord.Where,
                SqlKeyWord.GroupBy,
                SqlKeyWord.Having,
                SqlKeyWord.OrderBy
            },
            IndentAfterKeywords = new[]
            {
                SqlKeyWord.Select,
                SqlKeyWord.Where,
                SqlKeyWord.GroupBy,
                SqlKeyWord.Having
            }
        };

        // Format the SQL query using the specified settings
        string formattedSql = PrettyOutput(sql, settings);

        // Output the formatted SQL
        Console.WriteLine(formattedSql);
    }
}


/****************************************************************************************************
Output:

SELECT
    orders.id, customers.name, SUM(orders.amount) AS total_amount
FROM
    orders
    INNER JOIN customers ON orders.customer_id = customers.id
WHERE
    orders.date >= '2023-01-01'
GROUP BY
    orders.id, customers.name
HAVING
    total_amount > 1000
ORDER BY
    total_amount DESC
*****************************************************************************************************/
