using System;
using System.Linq;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace SteamToolsBot.Helpers;

internal sealed class FluentValidateOptions<TOptions> : IValidateOptions<TOptions>
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
