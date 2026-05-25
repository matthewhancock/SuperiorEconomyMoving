using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading.Tasks;
using api.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace api.Redirects;

/// <summary>
/// Inspects the inbound <c>Host</c> header. If it matches any source in <c>Features.Redirects</c>,
/// issues a 301 permanent redirect to the configured target preserving the request path and query.
/// Pass-through if no match — the rest of the pipeline (static files, quote endpoints) handles
/// requests to the canonical domain (e.g. <c>superiormoving.la</c>).
/// </summary>
internal sealed class DomainRedirectMiddleware {
    private readonly RequestDelegate _next;
    private readonly FrozenDictionary<string, string> _hostToTarget;

    public DomainRedirectMiddleware(RequestDelegate next, IOptions<FeaturesOptions> options) {
        _next = next;

        // Flatten the array of { Sources[], Target } into a Host → Target lookup.
        // Hosts are normalized to lowercase; both bare and "www." variants supported as configured.
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in options.Value.Redirects) {
            if (string.IsNullOrWhiteSpace(rule.Target)) continue;
            var target = rule.Target.TrimEnd('/');
            foreach (var source in rule.Sources) {
                if (string.IsNullOrWhiteSpace(source)) continue;
                dict[source.Trim()] = target;
            }
        }
        _hostToTarget = dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context) {
        var host = context.Request.Host.Host; // strips port
        if (!string.IsNullOrEmpty(host) && _hostToTarget.TryGetValue(host, out var target)) {
            var location = string.Concat(target, context.Request.Path, context.Request.QueryString);
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = location;
            context.Response.Headers.CacheControl = "public, max-age=604800"; // a week
            return;
        }
        await _next(context);
    }
}
