using Engine.Shared.Configuration;

namespace MyGame.Configuration.CVars;

[CVarDefs]
public sealed class LocalizationCVars
{
    public static readonly CVarDef<string> CurrentLocale = 
        CVarDef.Create("localization.locale", "en-US");
}
