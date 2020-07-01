using System.Collections.Generic;
using System.Data.SqlClient;
using static BlogCreator.QueryHelper;


namespace BlogCreator
{
    class TargetObjectFinder
    {
        public TargetObjectFinder(string query, string dataSourceName)
        {
            this.query = query;
            this.dataSourceName = dataSourceName;
        }

        private string query;
        private string dataSourceName;

        public List<RelatedTargetObject> ReadDataFromDBAndCreateRelatedTargetObjects()
        {
            List<RelatedTargetObject> targetObjectFields = new List<RelatedTargetObject>();
            if (query != null)
            {
                var connectionString = "Data Source="
                                       + dataSourceName
                                       + "\\SQLEXPRESS; Initial Catalog=Dnn7Upgrade;"
                                       + "Integrated Security=SSPI";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var targetObject = CreateRelatedTargetObject(reader);
                                targetObjectFields.Add(targetObject);
                            }
                            reader.Close();
                        }
                    }
                }
            }
            return targetObjectFields;
        }

        private static RelatedTargetObject CreateRelatedTargetObject(SqlDataReader reader)
        {
            var targetObject = new RelatedTargetObject
            {
                id = FillFieldsWithValue(reader, "ID"),
                imagePath = FillFieldsWithValue(reader, "ImagePath"),
                tag = FillFieldsWithValue(reader, "SubCategoryIdent"),
                caregory = FillFieldsWithValue(reader, "CategoryIdent"),
                content = ConvertHtmlToMarkdown(FillFieldsWithValue(reader, "Description"))
            };

            if (!ValueExists(targetObject.id))
                targetObject.ident = FillFieldsWithValue(reader, "Ident");

            return targetObject;
        }

        public class RelatedTargetObject
        {
            public string id { get; set; }
            public string tag { get; set; }
            public string ident { get; set; }
            public string content { get; set; }
            public string caregory { get; set; }
            public string imagePath { get; set; }
        }
    }
}
