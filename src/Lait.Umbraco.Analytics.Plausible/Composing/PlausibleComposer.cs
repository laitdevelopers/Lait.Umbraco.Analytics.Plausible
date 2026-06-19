using Lait.Umbraco.Analytics.Plausible.Configuration;
using Lait.Umbraco.Analytics.Plausible.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Lait.Umbraco.Analytics.Plausible.Composing;

/// <summary>
/// Registers options, the typed Plausible HttpClient, and the tracking service into DI.
/// Umbraco discovers and runs this automatically on boot.
/// </summary>
public sealed class PlausibleComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services
            .AddOptions<PlausibleOptions>()
            .Bind(builder.Config.GetSection(PlausibleOptions.SectionName));

        builder.Services.AddHttpClient<PlausibleClient>();
        builder.Services.AddScoped<ITrackingService, TrackingService>();
    }
}
