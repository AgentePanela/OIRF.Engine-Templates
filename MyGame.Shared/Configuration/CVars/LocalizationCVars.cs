using Engine.Shared.Configuration;

namespace MyGame.Shared.Configuration.CVars;

[CVarDefs]
public sealed class LocalizationCVars
{
    public static readonly CVarDef<string> CurrentLocale = 
        CVarDef.Create("localization.locale", "en-US");
}
