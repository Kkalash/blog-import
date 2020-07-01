using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using static BlogCreator.QueryHelper;

namespace BlogCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            var targetObjects = ReadDataFromDBAndCreateTargetObjects(SWT_Item, "AVATHAR", SWT_ItemReletedQuery);
            EditTemplateAndCreateFile(targetObjects, "SWT_Item", "template_SWT.md");
        }

        //liest Query und gibt ein Liste von TargetObject zurueck 
        private static List<TargetObject> ReadDataFromDBAndCreateTargetObjects(string query, string dataSourceName, string relatedQuery = null)
        {
            var connectionString = "Data Source="
                                   + dataSourceName
                                   + "\\SQLEXPRESS; Initial Catalog=Dnn7Upgrade;"
                                   + "Integrated Security=SSPI";
            List<TargetObject> targetObjects = new List<TargetObject>();
            var relatedTabelConnection = new TargetObjectFinder(relatedQuery, dataSourceName);
            var relatedObjectFields = relatedTabelConnection.ReadDataFromDBAndCreateRelatedTargetObjects();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var targetObject = CreateTargetObject(reader, relatedObjectFields);
                            if (query == SunBlog_Entries) targetObject.categories.Add("Nachrichten");
                            targetObjects.Add(targetObject);
                        }

                        reader.Close();
                    }
                }
            }
            return targetObjects;
        }

        //weist die Werte von der Tabelle an die properties von TargetObject zu und gibt ein TargetObject zurueck
        private static TargetObject CreateTargetObject(SqlDataReader reader, List<TargetObjectFinder.RelatedTargetObject> targetFields)
        {
            var targetObject = new TargetObject
            {
                tags = new List<string>(),
                categories = new List<string>(),

                id = FillFieldsWithValue(reader, "ID"),
                title = FillFieldsWithValue(reader, "Title"),
                fileName = FillFieldsWithValue(reader, "Ident"),
                subTitle = FillFieldsWithValue(reader, "SubTitle"),

                summary = ConvertHtmlToMarkdown(FillFieldsWithValue(reader, "TeaserText")),
                content = ConvertHtmlToMarkdown(FillFieldsWithValue(reader, "Description")),
                imagePath = EditImageToImagePath(FillFieldsWithValue(reader, "ImagePath"))
            };

            var tag = FillFieldsWithValue(reader, "SubCategoryIdent");
            var groupIdent = FillFieldsWithValue(reader, "GroupIdent");
            var categoryIdent = FillFieldsWithValue(reader, "CategoryIdent");
            var date = FillFieldsWithValue(reader, "Date");
            targetObject.date = ValueExists(date) ? DateTime.Parse(date).ToString("yyyy-MM-dd") : CreateDate();

            targetObject = FindRelatedTargetObjectIfExists(targetObject, targetFields);

            if (!ValueExists(targetObject.fileName))
                targetObject.fileName = FillFieldsWithValue(reader, "UrlName");

            if (ValueExists(tag)) targetObject.tags.Add(tag);
            if (ValueExists(categoryIdent)) targetObject.tags.Add(categoryIdent);
            if (ValueExists(groupIdent)) targetObject.tags.Add(groupIdent);

            return targetObject;
        }

        //bereinigt die Vorschaubilder und wandelt die zu Bilder
        private static string EditImageToImagePath(string image)
        {
            if (ValueExists(image))
            {
                image = image.Substring(image.LastIndexOf(@"/") + 1);
                image = "/images/items/" + image
                    .Replace("_kl.jpg", ".jpg")
                    .Replace("-kl.jpg", ".jpg")
                    .Replace("_kl.JPG", ".JPG")
                    .Replace("-kl.JPG", ".JPG")
                    .Replace("-akl.jpg", "-a.jpg");
            }
            return image;
        }

        //holt das Muster-Template, liest es und ersetzt die Platzhalter mit den properties vom TargetObject
        private static void EditTemplateAndCreateFile(List<TargetObject> targetObjects, string targetFolder, string srcTemplateName)
        {
            string srcfolderName = Directory.GetCurrentDirectory().ToString() + @"\Templates";
            var pathString = Path.Combine(srcfolderName, srcTemplateName);
            foreach (TargetObject targetObject in targetObjects)
            {
                var readBuffer = File.ReadAllText(pathString);
                try
                {
                    readBuffer = readBuffer
                        .Replace("{{date}}", targetObject.date)
                        .Replace("{{title}}", targetObject.title)
                        .Replace("{{image}}", targetObject.imagePath)
                        .Replace("{{content}}", targetObject.content)
                        .Replace("{{summary}}", targetObject.summary)
                        .Replace("{{subTitle}}", targetObject.subTitle)
                        .Replace("{{tags}}", ReplacePlaceholderObjectFields(targetObject.tags, "tags"))
                        .Replace("{{gallery}}", ReplacePlaceholderWithGalleryObjectFields(targetObject.gallery))
                        .Replace("{{categories}}", ReplacePlaceholderObjectFields(targetObject.categories, "categories"));

                    var fileName = ValueExists(targetObject.fileName) ? targetObject.fileName : targetFolder + "_" + targetObject.id;
                    CreateMarkdownFile(readBuffer, targetFolder, fileName);
                }

                catch (IOException e)
                {
                    throw new ArgumentException(e.Message);
                }
            }
        }

        //uebernimmt ein Galerie-Liste, formatiert es und gibt die Formatierung als string zurueck
        private static string ReplacePlaceholderWithGalleryObjectFields(List<TargetObjectGallery> fields)
        {
            var fieldsInString = "";
            if (fields != null)
            {
                foreach (TargetObjectGallery field in fields)
                {
                    if (ValueExists(field.image))
                    {
                        fieldsInString = fieldsInString == "" ? "gallery: \n" : fieldsInString;
                        var description = ConvertHtmlToMarkdown(field.description);
                        fieldsInString += "  - image: " + @"""/images/items/" + field.image + '"' + "\n";
                        fieldsInString += ValueExists(description) ? "    description: >-" + "\n      " + description.Replace("\n", "")
                            .Replace("\r", "") + "\n" : null;
                    }
                }
            }
            return fieldsInString;
        }

        //uebernimmt ein Tag/Kategorie-Liste, formatiert es und gibt die Formatierung als string zurueck
        private static string ReplacePlaceholderObjectFields(List<string> fields, string fieldType)
        {
            var fieldsInString = "";
            if (fields != null)
            {
                foreach (string field in fields)
                {
                    if (ValueExists(field))
                    {
                        fieldsInString = fieldsInString == "" ? fieldType + ": \n" : fieldsInString;
                        fieldsInString += "- " + '"' + field + '"' + "\n";
                    }
                }
            }
            return fieldsInString;
        }

        //uebernimmt den überarbeiteten Text und erstellt davon ein Markdown Datei
        private static void CreateMarkdownFile(string templateText, string targetFolder, string fileName)
        {
            var folderName = Directory.GetCurrentDirectory().ToString() + @"\Output\" + targetFolder;
            if (!File.Exists(folderName)) Directory.CreateDirectory(folderName);
            var pathString = Path.Combine(folderName, fileName + ".md");
            var fileExists = File.Exists(pathString);
            if (!fileExists && fileName.Length > 0) File.WriteAllText(pathString, templateText);

            WriteInfoInConsole(fileExists, fileName);
        }

        //gibt informaton über den Ablauf der Erstellung in der Konsole aus
        private static void WriteInfoInConsole(bool fileExists, string fileName)
        {
            if (!fileExists && fileName.Length > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("{0} is created", fileName);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("File {0} already exists", fileName);
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        private static string SWT_Item
        {
            get
            {
                return @"
                    SELECT
		                Ident,
                        SubTitle,
		                TeaserText,
                        SWT_Item.ID,
		                ItemTitle AS Title,
                        SWT_Item.[UrlName],
		                SWT_Item.[Description],
		                GroupTitle As GroupIdent,
                        MainImagePath AS ImagePath,
		                CategoryTitle AS CategoryIdent,
		                SubCategoryTitle AS SubCategoryIdent
                    FROM SWT_Item
                    LEFT JOIN SWT_ItemCategory ON SWT_Item.CategoryId = SWT_ItemCategory.ID
                    LEFT JOIN SWT_ItemGroup ON SWT_Item.GroupId = SWT_ItemGroup.ID
                    LEFT JOIN SWT_ItemSubCategory ON SWT_Item.SubCategoryId = SWT_ItemSubCategory.ID
                ";
            }
        }
        private static string SWT_ItemReletedQuery
        {
            get
            {
                return @"
                    SELECT DISTINCT
	                    ImagePath,
                        SWT_Item.ID,
                        ItemIdent AS Ident,
	                    Title AS CategoryIdent,
                        SWT_ItemImage.[Description]
                    FROM SWT_Item
                    LEFT JOIN SWT_ItemImage ON SWT_Item.ID = SWT_ItemImage.ItemId
                    LEFT JOIN SWT_ItemGroup ON SWT_Item.GroupId = SWT_ItemGroup.ID
                    LEFT JOIN Tabs ON SWT_ItemGroup.TabId = Tabs.TabID
                ";
            }
        }
        private static string SunBlog_Entries
        {
            get
            {
                return @"
                    SELECT
                        CreatedOnDate AS Date,
	                    SunBlog_Entries.Title,
	                    [Entry] AS Description,
                        Thumbnail AS ImagePath,
	                    SunBlog_Entries.EntryID AS ID,
                        MetaDescription AS TeaserText
                    FROM SunBlog_Entries
                    LEFT JOIN SunBlog_Tags ON ID = SunBlog_Tags.EntryID
                    LEFT JOIN SunBlog_CategoryEntry ON SunBlog_Entries.EntryID = SunBlog_CategoryEntry.EntryID
                    WHERE SunBlog_Entries.BlogID = 1
                ";
            }
        }
        private static string SunBlog_RelatedQuery
        {
            get
            {
                return @"
                    SELECT
	                    Tag AS SubCategoryIdent,
	                    SunBlog_CategoryEntry.EntryID AS ID,
	                    SunBlog_Categories.Title AS CategoryIdent
                    FROM SunBlog_CategoryEntry
                    LEFT JOIN SunBlog_Entries ON SunBlog_Entries.EntryID = SunBlog_CategoryEntry.EntryID
                    LEFT JOIN SunBlog_Tags ON SunBlog_Entries.EntryID = SunBlog_Tags.EntryID
                    LEFT JOIN SunBlog_Categories ON SunBlog_CategoryEntry.CategoryID = SunBlog_Categories.CategoryID 
                    WHERE SunBlog_Entries.BlogID = 1
                ";
            }
        }
    }
}

