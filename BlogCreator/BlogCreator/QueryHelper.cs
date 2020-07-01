using Html2Markdown;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace BlogCreator
{
    class QueryHelper
    {
        private static Converter converter = new Converter();

        private static DateTime start = new DateTime(2008, 1, 1);

        public static string FillFieldsWithValue(SqlDataReader reader, string columnName)
        {
            if (ColumnExists(reader, columnName))
            {
                return reader[columnName].ToString().Trim();
            }
            return null;
        }

        public static bool ColumnExists(IDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ValueExists(string field)
        {
            return field != null && field != "";
        }

        public static string ConvertHtmlToMarkdown(string html)
        {
            if (ValueExists(html))
            {
                do
                {
                    html = converter.Convert(html);
                } while (html.Contains("p&gt;") || html.Contains("<p>"));
            }
            return html;
        }

        public static string CreateDate()
        {
            start = start.AddDays(14);
            return start.ToString("yyyy-MM-dd");
        }

        //sucht nach zugehorigen Felder in der targetFields und wenn gefunden feugt die zu targetObject hinzu, gibt dann das targetObject zurueck
        public static TargetObject FindRelatedTargetObjectIfExists(TargetObject targetObject, List<TargetObjectFinder.RelatedTargetObject> targetFields)
        {
            if (targetFields != null)
            {
                targetObject.tags = targetFields.Where(f => f.id == targetObject.id && ValueExists(f.tag))
                    .Select(i => i.tag).Distinct().ToList();

                targetObject.gallery = targetFields.Where(f => f.id == targetObject.id && (ValueExists(f.content) || ValueExists(f.imagePath)))
                    .Select(i => new TargetObjectGallery { image = i.imagePath, description = i.content }).Distinct().ToList();

                targetObject.categories = targetFields.Where(f => f.id == targetObject.id && ValueExists(f.caregory))
                    .Select(i => i.caregory).Distinct().ToList();
            }

            return targetObject;
        }

        public class TargetObject
        {
            public string id { get; set; }
            public string date { get; set; }
            public string title { get; set; }
            public string summary { get; set; }
            public string content { get; set; }
            public string subTitle { get; set; }
            public string fileName { get; set; }
            public string imagePath { get; set; }
            public List<string> tags { get; set; }
            public List<string> categories { get; set; }
            public List<TargetObjectGallery> gallery { get; set; }
        }

        public class TargetObjectGallery
        {
            public string image { get; set; }
            public string description { get; set; }
        }
    }
}
