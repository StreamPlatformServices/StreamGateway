namespace APIGatewayCoreUtilities.CommonConfiguration.ConfigurationModels
{
    //TODO: default values in collections will be not overwritten. It will be cancatenated
    public class CorsSettings
    {
        public string[] AllowedHosts { get; set; }
        public string[] AllowedHeaders { get; set; }
        public string[] AllowedMethods { get; set; }
    }
}
