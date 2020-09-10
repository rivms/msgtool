using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
//using System.CommandLine.DragonFruit;
using System.IO;
using System.Text;


namespace azmsg
{
    class ConfigService
    {
        private string configPath;
        private string fullPath;

        public ConfigService(string configPath)
        {
            this.configPath = configPath;
            fullPath = Path.Combine(configPath, "config");
        }

        public AzMsgConfig LoadConfig()
        {
            if (!File.Exists(fullPath)) 
            {
                Directory.CreateDirectory(configPath);
                return new AzMsgConfig();
            }
            else
            {
                return ReadAsJson();
            }

        }

        public void UpdateConfig(AzMsgConfig config)
        {
            WriteAsJson(config);
        }

        public AzMsgConfig ReadConfig()
        {
            return ReadAsJson();
        }

        public void WriteConfig(AzMsgConfig config)
        {
            WriteAsJson(config);
        }

        //private AzMsgConfig ReadAsYaml() 
        //{
        //    var yaml = File.ReadAllText(fullPath);
        //    var deserializer = new DeserializerBuilder()
        //        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        //        //.IgnoreUnmatchedProperties()
        //        .Build();

        //    return deserializer.Deserialize<AzMsgConfig>(yaml);
        //}


        //private void WriteAsYaml(AzMsgConfig config)
        //{
        //    var serializer = new SerializerBuilder().EnsureRoundtrip().Build();
        //    var sb = new StringBuilder();
        //    File.WriteAllText(fullPath, serializer.Serialize(config)); 
        //}

        private AzMsgConfig ReadAsJson()
        {
            var json = File.ReadAllText(fullPath);
            var jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

            return JsonConvert.DeserializeObject<AzMsgConfig>(json, jsonSerializerSettings);
        }

        private void WriteAsJson(AzMsgConfig config)
        {
            var jsonSerializerSettings = new JsonSerializerSettings();
            //jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

            File.WriteAllText(fullPath, JsonConvert.SerializeObject(config));
        }

    }
}