// 写一个代码比较xml doc文档中的API和DLL中的公开的API是否一致

using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace DLLXMLCompare
{
    class Program
    {
        static (string, string) getArgs(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: DLLXMLCompare <dll file> <xml file>");
                return (null, null);
            }

            string dllFile = args[0];
            string xmlFile = args[1];

            if (!File.Exists(dllFile))
            {
                Console.WriteLine("DLL file not found: " + dllFile);
                return (null, null);
            }

            if (!File.Exists(xmlFile))
            {
                Console.WriteLine("XML file not found: " + xmlFile);
                return (null, null);
            }
            return (dllFile, xmlFile);
        }
        
        static void Main(string[] args)
        {
            // var dllAndXml = getArgs(args);
            var dllFile = "D:\\1228TEMP\\test.dll";
            var xmlFile = "D:\\1228TEMP\\test.xml";
            compare(dllFile, xmlFile);
        }


        static Dictionary<string, List<string>> getDllMeta(string dllFile)
        {
            var dllMeta = new Dictionary<string, List<string>>
            {
                ["Class"] = [],
                ["Method"] = [],
                ["Property"] = [],
                ["Field"] = [],
                ["Event"] = [],
                ["Constructor"] = []
            };
            try
            {
                string runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
                
                var resolver = new PathAssemblyResolver(Directory.GetFiles(runtimeDirectory, "*.dll"));
                var metadataLoadContext = new MetadataLoadContext(resolver);
                
                Assembly assembly = metadataLoadContext.LoadFromAssemblyPath(dllFile);
                
                foreach (Type type in assembly.GetExportedTypes())
                {
                    getDllTypeMeta(type, dllMeta);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return dllMeta;
        }

        private static void getDllTypeMeta(Type type, Dictionary<string, List<string>> dllMeta)
        {
            Console.WriteLine($"Class: {type.FullName}");
            dllMeta["Class"].Add(type.FullName);
                    
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                dllMeta["Method"].Add(method.Name);
            }
                    
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                dllMeta["Property"].Add(property.Name);
            }
                    
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                dllMeta["Field"].Add(field.Name);
            }
                    
            foreach (EventInfo eventInfo in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                dllMeta["Event"].Add(eventInfo.Name);
            }
                    
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                dllMeta["Constructor"].Add(constructor.Name);
            }
                    
            foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                getDllTypeMeta(nestedType, dllMeta);
            }
        }

        static void compare(string dllFile, string xmlFile)
        {
            var dllMeta = getDllMeta(dllFile);
            var xmlMeta = getXmlMeta(xmlFile);
            Console.WriteLine("END");
        }

        private static Dictionary<string, List<string>> getXmlMeta(string xmlFile)
        {
            var xmlMeta = new Dictionary<string, List<string>>();
            try
            {
                XDocument doc = XDocument.Load(xmlFile);
                foreach (XElement member in doc.Descendants("member"))
                {
                    string name = member.Attribute("name").Value;
                    string type = name.Substring(0, 2);
                    string value = name.Substring(2);
                    if (!xmlMeta.ContainsKey(type))
                    {
                        xmlMeta[type] = new List<string>();
                    }
                    xmlMeta[type].Add(value);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return xmlMeta;
        }
    }
}