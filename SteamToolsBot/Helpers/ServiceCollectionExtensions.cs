using System;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SteamToolsBot.Helpers;

internal static class ServiceCollectionExtensions
{
	public static void AddValidatableOptions<TOptions, TValidator>(this IServiceCollection serviceCollection)
		where TOptions : class
		where TValidator : class, IValidator<TOptions>
	{
		const string OptionsSuffix = "Options";
		var optionsTypeName = typeof(TOptions).Name;
		if (optionsTypeName.EndsWith(OptionsSuffix, StringComparison.OrdinalIgnoreCase))
		{
			optionsTypeName = optionsTypeName[..^OptionsSuffix.Length];
		}

		serviceCollection.AddTransient<IValidator<TOptions>, TValidator>();

		serviceCollection.AddOptions<TOptions>()
			.BindConfiguration(optionsTypeName)
			.ValidateFluentValidation()
			.ValidateOnStart();
	}


	private static OptionsBuilder<TOptions> ValidateFluentValidation<TOptions>(
		this OptionsBuilder<TOptions> builder)
		where TOptions : class
	{
		builder.Services.AddSingleton<IValidateOptions<TOptions>>(serviceProvider => new FluentValidateOptions<TOptions>(
			serviceProvider.GetRequiredService<IValidator<TOptions>>(),
			builder.Name));

		return builder;
	}
}
