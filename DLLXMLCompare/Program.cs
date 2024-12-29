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
            var valueTuple = compare(dllFile, xmlFile);
            var inDllNotInXml = valueTuple.Item1;
            var inXmlNotInDll = valueTuple.Item2;
            
            ProcessXML(xmlFile, "D:\\1228TEMP\\test1.xml", inXmlNotInDll);
        }
        
        static void ProcessXML(string xmlFile,string saveXml, Dictionary<string,List<string>> inXmlNotInDll)
        {
            XDocument doc = XDocument.Load(xmlFile);
            HashSet<string> hashSet = new HashSet<string>();
            foreach (var keyValuePair in inXmlNotInDll)
            {
                // 将所有的value放入hashSet
                foreach (var value in keyValuePair.Value)
                {
                    hashSet.Add(value);
                }
            }
            // 获取所有的member
            var members = doc.Descendants("member").ToList();
            foreach (var member in members)
            {
                string name = member.Attribute("name").Value;
                string type = name.Substring(0, 2);
                string value = name.Substring(2);
                if (hashSet.Contains(value))
                {
                    Console.WriteLine($"Remove {name}");
                    member.Remove();
                }
            }
            doc.Save(saveXml);
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
            var className = type.FullName.Replace("+", ".");
            dllMeta["Class"].Add(className);
                    
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var methodName = className +"."+ method.Name;
                dllMeta["Method"].Add(methodName);
            }
                    
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var propertyName = className +"."+ property.Name;
                dllMeta["Property"].Add(propertyName);
            }
                    
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var fieldName = className +"."+ field.Name;
                dllMeta["Field"].Add(fieldName);
            }
                    
            foreach (EventInfo eventInfo in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var eventName = className +"."+ eventInfo.Name;
                dllMeta["Event"].Add(eventName);
            }
                    
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var constructorName = className +"."+ constructor.Name;
                dllMeta["Constructor"].Add(constructorName);
            }
                    
            foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                getDllTypeMeta(nestedType, dllMeta);
            }
        }

        static (Dictionary<string, List<string>>, Dictionary<string, List<string>>) compare(string dllFile, string xmlFile)
        {
            var dllMeta = getDllMeta(dllFile);
            var xmlMeta = getXmlMeta(xmlFile);
            
            // 比较class
            Dictionary<string,List<string>> inDllNotInXml = new Dictionary<string, List<string>>()
            {
                ["Class"] = [],
                ["Method"] = [],
                ["Property"] = [],
                ["Field"] = [],
                ["Event"] = [],
                ["Constructor"] = []
            };
            Dictionary<string,List<string>> inXmlNotInDll = new Dictionary<string, List<string>>()
            {
                ["Class"] = [],
                ["Method"] = [],
                ["Property"] = [],
                ["Field"] = [],
                ["Event"] = [],
                ["Constructor"] = []
            };
            var dllClass = dllMeta["Class"];
            var xmlClass = xmlMeta["T:"];
            foreach (var className in dllClass)
            {
                if (!xmlClass.Contains(className))
                {
                    inDllNotInXml["Class"].Add(className);
                }
            }
            foreach (var className in xmlClass)
            {
                if (!dllClass.Contains(className))
                {
                    inXmlNotInDll["Class"].Add(className);
                }
            }
            // 比较method
            var dllMethod = dllMeta["Method"];
            var xmlMethod = xmlMeta["M:"];
            foreach (var dllMethodName in dllMethod)
            {
                var found = false;
                foreach (var xmlMethodName in xmlMethod)
                {
                    if (xmlMethodName.StartsWith(dllMethodName))
                    {
                        found = true;
                        break;
                    }
                }
                // XML中没有
                if (!found)
                {
                    inDllNotInXml["Method"].Add(dllMethodName);
                }
            }
            foreach (var xmlMethodName in xmlMethod)
            {
                if(xmlMethodName.Contains("#ctor"))
                {
                    // 判断是否有这个类
                    var className = xmlMethodName.Split(".#ctor")[0];
                    if (!dllClass.Contains(className))
                    {
                        inXmlNotInDll["Method"].Add(xmlMethodName);
                    }
                }
                else
                {
                    var found = false;
                    foreach (var dllMethodName in dllMethod)
                    {
                        if (xmlMethodName.StartsWith(dllMethodName))
                        {
                            found = true;
                            break;
                        }
                    }
                    // DLL中没有
                    if (!found)
                    {
                        inXmlNotInDll["Method"].Add(xmlMethodName);
                    }
                }
                
            }
            
            // 比较property
            var dllProperty = dllMeta["Property"];
            var xmlProperty = xmlMeta["P:"];
            foreach (var dllPropertyName in dllProperty)
            {
               if(!xmlProperty.Contains(dllPropertyName))
               {
                   inDllNotInXml["Property"].Add(dllPropertyName);
               }
            }
            foreach (var xmlPropertyName in xmlProperty)
            {
                if(!dllProperty.Contains(xmlPropertyName))
                {
                    inXmlNotInDll["Property"].Add(xmlPropertyName);
                }
            }
            // 比较field
            var dllField = dllMeta["Field"];
            var xmlField = xmlMeta["F:"];
            foreach (var dllFieldName in dllField)
            {
                if(!xmlField.Contains(dllFieldName))
                {
                    inDllNotInXml["Field"].Add(dllFieldName);
                }
            }
            foreach (var xmlFieldName in xmlField)
            {
                if(!dllField.Contains(xmlFieldName))
                {
                    inXmlNotInDll["Field"].Add(xmlFieldName);
                }
            }
            
            // 比较event
            var dllEvent = dllMeta["Event"];
            var xmlEvent = xmlMeta["E:"];
            foreach (var dllEventName in dllEvent)
            {
                if(!xmlEvent.Contains(dllEventName))
                {
                    inDllNotInXml["Event"].Add(dllEventName);
                }
            }
            foreach (var xmlEventName in xmlEvent)
            {
                if(!dllEvent.Contains(xmlEventName))
                {
                    inXmlNotInDll["Event"].Add(xmlEventName);
                }
            }
            return (inDllNotInXml, inXmlNotInDll);
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