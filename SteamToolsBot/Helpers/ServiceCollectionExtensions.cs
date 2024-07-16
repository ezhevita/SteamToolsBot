using System;
using FluentValidation;
using IL.FluentValidation.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace SteamToolsBot.Helpers;

public static class ServiceCollectionExtensions
{
	public static void AddValidatableOptions<T, TValidator>(this IServiceCollection serviceCollection)
		where T : class
		where TValidator : class, IValidator<T>
	{
		const string OptionsSuffix = "Options";
		var optionsTypeName = typeof(T).Name;
		if (optionsTypeName.EndsWith(OptionsSuffix, StringComparison.OrdinalIgnoreCase))
		{
			optionsTypeName = optionsTypeName[..^OptionsSuffix.Length];
		}

		serviceCollection.AddTransient<IValidator<T>, TValidator>();

		serviceCollection.AddOptions<T>()
			.BindConfiguration(optionsTypeName)
			.ValidateWithFluentValidator()
			.ValidateOnStart();
	}
}
