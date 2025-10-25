namespace MiniTemplateEngine
{
    public interface IHtmlTemplateRenderer
    {
        /// <summary>Рендерит шаблон из строки с использованием модели данных.</summary>
        string RenderFromString(string htmlTemplate, object dataModel);

        /// <summary>Рендерит шаблон, считанный из файла, с использованием модели данных.</summary>
        string RenderFromFile(string filePath, object dataModel);

        /// <summary>Рендерит шаблон из файла и сохраняет результат в выходной файл.</summary>
        void RenderToFile(string inputFilePath, string outputFilePath, object dataModel);
    }
}
