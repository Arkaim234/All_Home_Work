using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine
{
    /// <summary>
    /// Упрощённый HTML-шаблонизатор с базовой поддержкой переменных, условий и циклов.
    /// </summary>
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        // Минимальные регулярки:

        private static readonly Regex VariableRegex = new(@"\$\{([^}]+)\}",
            RegexOptions.Compiled | RegexOptions.Singleline);

        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            return ProcessTemplate(htmlTemplate, dataModel, new Dictionary<string, object?>()).Trim();
        }

        public string RenderFromFile(string filePath, object dataModel)
        {
            string htmlTemplate = File.ReadAllText(filePath);
            return RenderFromString(htmlTemplate, dataModel);
        }

        public void RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
        {
            string result = RenderFromFile(inputFilePath, dataModel);
            File.WriteAllText(outputFilePath, result);
        }

        private string ProcessTemplate(string template, object dataModel, Dictionary<string, object?> context)
        {
            string result = ProcessForeach(template, dataModel, context);
            result = ProcessIfElse(result, dataModel, context);
            result = ProcessVariables(result, dataModel, context);
            return CleanSpaces(result);
        }

        private string CleanSpaces(string input)
        {
            // Заменяем множественные пробелы на один, но сохраняем структуру
            return Regex.Replace(input, @"\s+", " ");
        }
        private string ProcessForeach(string template, object dataModel, Dictionary<string, object?> context)
        {
            var sb = new StringBuilder();
            int index = 0;
            while (true)
            {
                int foreachPos = template.IndexOf("$foreach(", index, StringComparison.Ordinal);
                if (foreachPos < 0)
                {
                    // нет больше foreach — приписываем остаток и выходим
                    sb.Append(template.AsSpan(index));
                    break;
                }

                // добавляем всё до foreach
                sb.Append(template.AsSpan(index, foreachPos - index));

                // находим конец заголовка "…)"
                int headerEnd = template.IndexOf(')', foreachPos);
                string header = template.Substring(foreachPos + 9, headerEnd - (foreachPos + 9));
                string[] headerParts = header.Split(new[] { "in" }, StringSplitOptions.RemoveEmptyEntries);
                string itemName = headerParts[0].Trim().Substring(3).Trim(); // after "var"
                string collectionPath = headerParts[1].Trim();

                // ищем соответствующий $endfor с учётом вложенности
                int bodyStart = headerEnd + 1;
                int pos = bodyStart;
                int depth = 1;
                while (depth > 0 && pos < template.Length)
                {
                    int nextFor = template.IndexOf("$foreach", pos, StringComparison.Ordinal);
                    int nextEnd = template.IndexOf("$endfor", pos, StringComparison.Ordinal);
                    if (nextEnd >= 0 && (nextFor < 0 || nextEnd < nextFor))
                    {
                        depth--;
                        pos = nextEnd + 7; // длина "$endfor"
                    }
                    else if (nextFor >= 0)
                    {
                        depth++;
                        pos = nextFor + 1;
                    }
                    else
                    {
                        // несбалансированная конструкция
                        break;
                    }
                }
                int bodyEnd = pos - 7;
                string body = template.Substring(bodyStart, bodyEnd - bodyStart);

                // рекурсивно обрабатываем тело цикла
                var collection = GetValueByPath(dataModel, collectionPath, context) as IEnumerable;
                if (collection != null)
                {
                    foreach (var item in collection)
                    {
                        var localContext = new Dictionary<string, object?>(context) { [itemName] = item };
                        sb.Append(ProcessTemplate(body, item, localContext));
                    }
                }

                // переходим к следующему фрагменту после $endfor
                index = pos;
            }

            return sb.ToString();
        }

        private string ProcessIfElse(string template, object dataModel, Dictionary<string, object?> context)
        {
            var sb = new StringBuilder();
            int index = 0;

            while (true)
            {
                int start = template.IndexOf("$if(", index, StringComparison.Ordinal);
                if (start < 0)
                {
                    sb.Append(template.AsSpan(index));
                    break;
                }

                sb.Append(template.AsSpan(index, start - index));

                int condEnd = template.IndexOf(')', start);
                if (condEnd == -1) break;
                string conditionPath = template.Substring(start + 4, condEnd - (start + 4)).Trim();

                int scan = condEnd + 1;
                int depth = 1;
                int elsePos = -1;

                while (depth > 0 && scan < template.Length)
                {
                    int nextIf = template.IndexOf("$if(", scan, StringComparison.Ordinal);
                    int nextElse = template.IndexOf("$else", scan, StringComparison.Ordinal);
                    int nextEnd = template.IndexOf("$endif", scan, StringComparison.Ordinal);

                    if (nextEnd == -1) break;

                    if (nextIf >= 0 && nextIf < nextEnd)
                    {
                        depth++;
                        scan = nextIf + 4;
                    }
                    else if (nextElse >= 0 && nextElse < nextEnd && depth == 1)
                    {
                        elsePos = nextElse;
                        scan = nextElse + 5;
                    }
                    else
                    {
                        depth--;
                        scan = nextEnd + 6;
                    }
                }

                int endifPos = scan - 6;
                string ifBody, elseBody;

                if (elsePos >= 0)
                {
                    ifBody = template.Substring(condEnd + 1, elsePos - (condEnd + 1));
                    elseBody = template.Substring(elsePos + 5, endifPos - (elsePos + 5));
                }
                else
                {
                    ifBody = template.Substring(condEnd + 1, endifPos - (condEnd + 1));
                    elseBody = string.Empty;
                }

                bool condition = EvaluateCondition(GetValueByPath(dataModel, conditionPath, context));
                sb.Append(ProcessTemplate(condition ? ifBody : elseBody, dataModel, context));
                index = scan;
            }

            return sb.ToString();
        }
        private bool EvaluateCondition(object? value)
        {
            if (value is bool b) return b;
            if (value == null) return false;
            if (value is string s)
            {
                if (bool.TryParse(s, out var parsed)) return parsed;
                return !string.IsNullOrWhiteSpace(s);
            }
            if (value is IEnumerable e)
            {
                var en = e.GetEnumerator();
                try { return en.MoveNext(); }
                finally { if (en is IDisposable d) d.Dispose(); }
            }
            return true;
        }

        private string ProcessVariables(string template, object dataModel, Dictionary<string, object?> context)
        {
            return VariableRegex.Replace(template, match =>
            {
                string propertyPath = match.Groups[1].Value.Trim();
                object? value = GetValueByPath(dataModel, propertyPath, context);
                return value?.ToString() ?? string.Empty;
            });
        }

        private static object? GetValueByPath(object obj, string propertyPath, Dictionary<string, object?> context)
        {
            if (obj == null) return null;

            string trimmedPath = propertyPath.Trim();
            if (string.IsNullOrEmpty(trimmedPath))
                return obj;

            var parts = trimmedPath.Split('.');
            object? current = obj;

            foreach (var part in parts)
            {
                if (current == null) return null;

                var type = current.GetType();
                var prop = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop != null)
                {
                    current = prop.GetValue(current);
                    continue;
                }

                var field = type.GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    current = field.GetValue(current);
                    continue;
                }

                if (context.TryGetValue(part, out var ctxVal))
                {
                    current = ctxVal;
                    continue;
                }

                return null;
            }

            return current;
        }
    }
}