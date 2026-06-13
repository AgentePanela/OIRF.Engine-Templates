using System;
using System.Collections.Generic;
using System.Globalization;
using Engine.Shared.Locale;
using MyGame.Configuration.CVars;
using Linguini.Shared.Types.Bundle;
using Engine.Shared.IoC;
using Engine.Shared.Configuration;

namespace MyGame.Locale;

[RegisterIoC]
public sealed class ContentLocalizationManager
{
    [Dependency] private readonly ILocalizationManager _locMan = default!;
    [Dependency] private readonly IConfigurationManager _cgf = default!;
    public string Locale = "en-US";
    public string FallbackLocale = "en-US";

    public ContentLocalizationManager()
    {
        IoCManager.ResolveDependencies(this);
        Locale = _cgf.Get(LocalizationCVars.CurrentLocale);
        Init();
    }

    public void Init()
    {
        // global functions
        _locMan.AddFunction("UPPER", Upper);
        _locMan.AddFunction("LOWER", Lower);
        _locMan.AddFunction("CAPITALIZE", Capitalize);
        _locMan.AddFunction("FORMAT_TIME", FormatTime);

        var culture = new CultureInfo(Locale);
        var fallback = new CultureInfo(FallbackLocale);

        _locMan.SetCulture(culture);

        _locMan.SetFallbackCulture(fallback);
        _locMan.LoadCulture();
    }

    #region Fluent functions

    private IFluentType Upper(IList<IFluentType> args, IDictionary<string, IFluentType> named)
    {
        var str = args[0].AsString();
        return (FluentString)str.ToUpperInvariant();
    }

    private IFluentType Lower(IList<IFluentType> args, IDictionary<string, IFluentType> named)
    {
        var str = args[0].AsString();
        return (FluentString)str.ToLowerInvariant();
    }

    private IFluentType Capitalize(IList<IFluentType> args, IDictionary<string, IFluentType> named)
    {
        var str = args[0].AsString();

        if (string.IsNullOrEmpty(str))
            return (FluentString)"";

        return (FluentString)(char.ToUpperInvariant(str[0]) + str[1..]);
    }

    private IFluentType FormatTime(IList<IFluentType> args, IDictionary<string, IFluentType> named)
    {
        var seconds = ((FluentNumber)args[0]).Value;
        var ts = TimeSpan.FromSeconds(seconds);

        return (FluentString)ts.ToString(@"mm\:ss");
    }
    #endregion
}
