dotnet new -i StreamDeck.PluginTemplate.Csharp --force

dotnet new streamdeck-plugin -o testPlugin -pn IntegrationTestPlugin -uu test.plugin.integrationtest
cd testPlugin