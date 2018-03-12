﻿namespace SkiaSharp.Components.Markup.Build
{
    using System;
    using System.Xml.Linq;
    using System.Linq;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    public class SkmlTask : Task
    {
        [Required]
        public ITaskItem[] Source { get; set; }

        public string AssemblyName { get; set; }

        [Output]
        public string[] OutputFile { get; set; }

        public override bool Execute()
        {
            this.Log.LogMessage("Source: {0}", string.Join(",", Source.Select(x => x.ItemSpec)));
            this.Log.LogMessage("AssemblyName: {0}", AssemblyName);
            this.Log.LogMessage("OutputFile {0}", string.Join(",", OutputFile));

            try
            {
                for (int i = 0; i < Source.Length; i++)
                {
                    var source = this.Source[i];
                    var output = this.OutputFile[i];

                    var allStylesheets = source.GetMetadata("Stylesheets").Split(';').Select(x => x.Trim());
                    this.Log.LogMessage("Stylesheets: {0}", string.Join(",", allStylesheets.Select(x => x)));

                    // Parse skml
                    this.Log.LogMessage("Loading file: {0}", source.ItemSpec);
                    var parser = new SkmlParser();
                    var xml = XDocument.Load(source.ItemSpec);

                    // Parse referenced skss
                    var skssParser = new SkssParser();
                    var stylesheetsNames = xml.Root.Attribute("Stylesheets")?.Value?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? new string [] {};
                    var stylesheets = stylesheetsNames.Select(x => allStylesheets.FirstOrDefault(f => Path.GetFileName(f) == x))
                                                      .Select(x =>
                                                        {
                                                            Log.LogMessage("Parsing stylesheet '{0}'...", x);
                                                            using (var skssStream = File.OpenRead(x))
                                                            {
                                                                return skssParser.Parse(skssStream);
                                                            }
                                                        }).ToArray();

                    var layout = new Layout()
                    {
                        Content = xml,
                    };
                    layout.Stylesheets.AddRange(stylesheets);

                    this.Log.LogMessage("Parsing file: {0}", source.ItemSpec);
                    var flex = parser.Parse(layout);

                    // Generate C#
                    this.Log.LogMessage("Generating C#: {0}, {1}", output, flex.Class);
                    var generator = new CsharpGenerator();

                    if (File.Exists(output))
                        File.Delete(output);

                    var folder = Path.GetDirectoryName(output);

                    if (!Directory.Exists(folder))
                    {
                        this.Log.LogMessage("Creating directory: {0}", folder);
                        Directory.CreateDirectory(folder);
                    }

                    using(var stream = File.Create(output))
                    {
                        generator.Generate(flex, stream);
                    }

                } 

                return true;
            }
            catch (Exception e)
            {
                this.Log.LogMessage("Error: {0}", e.Message);
                this.Log.LogMessage("StackTrace: {0}", e.StackTrace);
                this.Log.LogError(null, null, null, Source, 0, 0, 0, 0, $"{e.Message}");
                return false;
            }
        }
    }
}