using System;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SteamToolsBot.Helpers;

internal static class ServiceCollectionValidatableOptionsExtensions
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

	private sealed class FluentValidateOptions<TOptions> : IValidateOptions<TOptions>
		where TOptions : class
	{
		private readonly IValidator<TOptions> _validator;
		private readonly string? _name;

		public FluentValidateOptions(IValidator<TOptions> validator, string? name)
		{
			_validator = validator;
			_name = name;
		}

		public ValidateOptionsResult Validate(string? name, TOptions options)
		{
			if (_name != null && _name != name)
			{
				return ValidateOptionsResult.Skip;
			}

			ArgumentNullException.ThrowIfNull(options);

			var results = _validator.Validate(options);
			if (results.IsValid)
			{
				return ValidateOptionsResult.Success;
			}

			var typeName = options.GetType().Name;

			return ValidateOptionsResult.Fail(
				results.Errors
					.Select(result =>
						$"Fluent validation failed for '{typeName}.{result.PropertyName}' with the error: '{result.ErrorMessage}'."));
		}
	}
}

