using CronService.Models;
using CronService.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace CronService.Helpers
{
    public static class YamlHelper
    {
        private static YamlStream _yaml = new YamlStream();
        private static string _path;

        public static void Configure(IConfiguration configuration)
        {
            _path = configuration.GetSection("PathYAML").Value;
            Load();
        }

        private static void Load()
        {
            bool isFileLocked = true;
            string yamlContent = "";
            while (isFileLocked)
            {
                try
                {
                    using (StreamReader reader = new StreamReader(_path))
                    {
                        yamlContent = reader.ReadToEnd();
                    }

                    var input = new StringReader(yamlContent);
                    _yaml.Load(input);
                    input.Close();
                    isFileLocked = false;
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                    Task.Delay(10);
                }
            }
        }

        public static void SaveYAML()
        {
            bool isFileLocked = true;
            while (isFileLocked)
            {
                try
                {
                    using (var writer = new StreamWriter(_path))
                    {
                        _yaml.Save(writer, false);
                    }
                    isFileLocked = false;
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.Message);
                    Task.Delay(10);
                }
            }
        }

        public static DateTimeOffset GetOldTimeFromYAML(CronConfig cronConfig)
        {
            if (!_yaml.Documents.Any())
                return DateTimeOffset.MinValue;

            var rootNode = (YamlMappingNode)_yaml.Documents[0].RootNode;
            var key = cronConfig.IdMeasuring.ToString() + cronConfig.ToTable + cronConfig.Type;

            if (rootNode.Children.TryGetValue(key, out var valueNode))
                return DateTimeOffset.Parse(valueNode.ToString());
            else
                return DateTimeOffset.MinValue;
        }

        public static void WriteOldTimeToYAML(DateTimeOffset oldTime, string key)
        {
            if (!_yaml.Documents.Any())
            {
                var rootNode = new YamlMappingNode();
                rootNode.Children[new YamlScalarNode(key)] = new YamlScalarNode(oldTime.ToString());
                var document = new YamlDocument(rootNode);
                _yaml.Documents.Add(document);
            }
            else
            {
                var rootNode = (YamlMappingNode)_yaml.Documents[0].RootNode;
                YamlScalarNode newKey = new YamlScalarNode(key);
                YamlScalarNode newValue = new YamlScalarNode(oldTime.ToString());
                rootNode.Add(newKey, newValue);
            }
        }

        public static void UpdateOldTimeYAML(DateTimeOffset oldTime, string key)
        {
            var rootNode = (YamlMappingNode)_yaml.Documents[0].RootNode;
            if (rootNode.Children.ContainsKey(key))
            {
                var valueNode = (YamlScalarNode)rootNode.Children[key];
                valueNode.Value = oldTime.ToString();
            }
        }
    }
}