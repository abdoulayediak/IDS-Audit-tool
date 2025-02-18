﻿using System.Diagnostics;
using System.Text;
using Xbim.Common.Metadata;

namespace IdsLib.codegen;

class IfcSchema_AttributesGenerator
{
    /// <summary>
    /// SchemaInfo.GeneratedAttributes.cs
    /// </summary>
    static public string Execute()
    {
        var source = stub;
        
        foreach (var schemaString in Program.schemas)
        {
            System.Reflection.Module module = SchemaHelper.GetModule(schemaString);
            var metaD = ExpressMetaData.GetMetadata(module);
            var sb = new StringBuilder();

            // trying to find a set of classes that matches the property types
            List<string> HandledTypes = new();
            foreach (var item in metaD.Types())
            {
                HandledTypes.Add(item.Name.ToUpperInvariant());
            }
            Dictionary<string, List<string>> typesByAttribute = new();
            foreach (var className in HandledTypes)
            {
                var daType = metaD.ExpressType(className.ToUpperInvariant());
                foreach (var prop in daType.Properties.Values)
                {
                    if (typesByAttribute.TryGetValue(prop.Name, out var lst))
                        lst.Add(className);
                    else
                    {
                        typesByAttribute.Add(prop.Name, new List<string>() { className });
                    }
                }
            }
            Debug.WriteLine($"{schemaString}");
            foreach (var pair in typesByAttribute)
            {
                var attribute = $"\"{pair.Key}\"";
                // trying to remove all subclasses
                var toRemove = new HashSet<string>();
                var onlyTopClasses = pair.Value.ToList();
                for (int i = 0; i < onlyTopClasses.Count; i++)
                {
                    var thisClassName = onlyTopClasses[i];
                    var thisClass = metaD.ExpressType(thisClassName.ToUpperInvariant());

                    foreach (var sub in thisClass.AllSubTypes)
                    {
                        if(!toRemove.Contains(sub.ExpressNameUpper))
                        {
                            toRemove.Add(sub.ExpressNameUpper);
                        }
                    }
                }

                var classesInQuotes = pair.Value.Select(x => $"\"{x}\"").ToArray();
                var topClassesInQuotes = onlyTopClasses
                    .Where(c=> !toRemove.Contains(c))
                        .Select(x => $"\"{x}\"").ToArray();
                var line = $"\t\tdestinationSchema.AddAttribute({attribute}, new[] {{ {string.Join(", ", topClassesInQuotes)} }}, new[] {{ {string.Join(", ", classesInQuotes)} }});";

                sb.AppendLine(line);
            }
            source = source.Replace($"<PlaceHolder{schemaString}>\r\n", sb.ToString());
        }
        source = source.Replace($"<PlaceHolderVersion>", VersionHelper.GetFileVersion(typeof(ExpressMetaData)));
        return source;
    }
    private const string stub = @"// generated code via ids-lib.codegen using Xbim.Essentials <PlaceHolderVersion> - any changes made directly here will be lost

using System;

namespace IdsLib.IfcSchema;

public partial class SchemaInfo
{
	static partial void GetAttributesIFC2x3(SchemaInfo destinationSchema)
	{
<PlaceHolderIfc2x3>
	}

	static partial void GetAttributesIFC4(SchemaInfo destinationSchema)
	{
<PlaceHolderIfc4>
	}

    static partial void GetAttributesIFC4x3(SchemaInfo destinationSchema)
	{
<PlaceHolderIfc4x3>
	}
}
";
}