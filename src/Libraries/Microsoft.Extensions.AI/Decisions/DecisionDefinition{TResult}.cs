// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Microsoft.Extensions.AI;

/// <summary>
/// Declares the decision questions and application-owned result properties for a typed decision.
/// </summary>
/// <typeparam name="TResult">The application-owned result type.</typeparam>
/// <remarks>
/// Each declared result property becomes one question whose ID is the property's JSON name. When a response is bound,
/// each answer is written to its property and the result is materialized with <see cref="ResultTypeInfo"/>.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionDefinition<TResult>
{
    private readonly ReadOnlyCollection<DecisionDefinitionQuestion<TResult>> _questions;

    private DecisionDefinition(
        JsonTypeInfo<TResult> resultTypeInfo,
        IReadOnlyList<DecisionDefinitionQuestion<TResult>> questions)
    {
        ResultTypeInfo = resultTypeInfo;
        _questions = new ReadOnlyCollection<DecisionDefinitionQuestion<TResult>>(questions.ToArray());
        Questions = new ReadOnlyCollection<DecisionQuestion>(_questions.Select(static question => question.Question).ToArray());
    }

    /// <summary>Gets the JSON contract used to materialize the result.</summary>
    public JsonTypeInfo<TResult> ResultTypeInfo { get; }

    /// <summary>Gets the serializer options associated with this definition.</summary>
    public JsonSerializerOptions SerializerOptions => ResultTypeInfo.Options;

    /// <summary>Gets the ordered questions declared by this definition.</summary>
    public IReadOnlyList<DecisionQuestion> Questions { get; }

    /// <summary>Creates a typed decision definition.</summary>
    /// <param name="configure">A callback that declares the questions.</param>
    /// <param name="serializerOptions">
    /// The JSON options that provide the contracts for <typeparamref name="TResult"/> and its enum properties.
    /// If <see langword="null"/>, <see cref="AIJsonUtilities.DefaultOptions"/> is used. The options are copied;
    /// when they have no <see cref="JsonSerializerOptions.TypeInfoResolver"/>, the resolver from
    /// <see cref="AIJsonUtilities.DefaultOptions"/> is used.
    /// </param>
    /// <returns>The decision definition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">No question is declared, or a declaration is invalid.</exception>
    /// <exception cref="NotSupportedException">The options do not provide a required contract.</exception>
    public static DecisionDefinition<TResult> Create(
        Action<Builder> configure,
        JsonSerializerOptions? serializerOptions = null)
    {
        _ = Throw.IfNull(configure);

        serializerOptions = new JsonSerializerOptions(serializerOptions ?? AIJsonUtilities.DefaultOptions);
        serializerOptions.TypeInfoResolver ??= AIJsonUtilities.DefaultOptions.TypeInfoResolver;
        serializerOptions.MakeReadOnly();

        if (serializerOptions.GetTypeInfo(typeof(TResult)) is not JsonTypeInfo<TResult> resultTypeInfo)
        {
            throw new NotSupportedException(
                $"The configured JSON options do not contain a contract for result type '{typeof(TResult)}'.");
        }

        Builder builder = new(resultTypeInfo);
        configure(builder);
        return builder.Build();
    }

    /// <summary>Creates an <see cref="AIFunction"/> that evaluates this definition for an application-owned state type.</summary>
    /// <typeparam name="TState">The application-owned state type exposed as the function's input.</typeparam>
    /// <param name="client">The decision client used for each invocation.</param>
    /// <param name="functionOptions">Optional function factory options.</param>
    /// <param name="decisionOptions">Optional decision options. They are copied now and cloned for each invocation.</param>
    /// <param name="stateTypeInfo">
    /// Optional JSON contract for <typeparamref name="TState"/>. When omitted, <see cref="SerializerOptions"/> must
    /// contain the state contract. When it comes from different options, the function serializes with a resolver
    /// that combines the state and result resolvers.
    /// </param>
    /// <returns>A function whose only input is <typeparamref name="TState"/> and whose result is <typeparamref name="TResult"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">No contract for <typeparamref name="TState"/> is available.</exception>
    public AIFunction AsAIFunction<TState>(
        IDecisionClient client,
        AIFunctionFactoryOptions? functionOptions = null,
        DecisionOptions? decisionOptions = null,
        JsonTypeInfo<TState>? stateTypeInfo = null)
    {
        _ = Throw.IfNull(client);

        stateTypeInfo ??= GetStateTypeInfo<TState>();
        DecisionFunctionInvoker<TState> invoker = new(client, this, stateTypeInfo, decisionOptions?.Clone());

        return AIFunctionFactory.Create(
            invoker.InvokeAsync,
            GetFunctionOptions(functionOptions, GetFunctionSerializerOptions(stateTypeInfo.Options)));
    }

    internal DecisionResponse<TResult> Bind(DecisionResponse response)
    {
        DecisionAnswer[] answers = new DecisionAnswer[_questions.Count];
        List<string>? unanswered = null;
        for (int i = 0; i < answers.Length; i++)
        {
            DecisionQuestion question = _questions[i].Question;
            answers[i] = GetBindableAnswer(response, question);
            if (answers[i] is UnansweredDecisionAnswer)
            {
                (unanswered ??= []).Add(question.Id);
            }
        }

        if (unanswered is not null)
        {
            throw new DecisionUnansweredException(response, unanswered);
        }

        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            writer.WriteStartObject();
            for (int i = 0; i < answers.Length; i++)
            {
                DecisionDefinitionQuestion<TResult> question = _questions[i];
                switch (answers[i])
                {
                    case BinaryDecisionAnswer binary:
                        writer.WriteNumber(question.PropertyName, binary.TrueProbability);
                        break;

                    case ChoiceDecisionAnswer choice:
                        writer.WriteString(question.PropertyName, choice.SelectedCandidateId);
                        break;

                    case ScoreDecisionAnswer score:
                        writer.WriteNumber(question.PropertyName, GetScoreValue(question, score));
                        break;

                    default:
                        throw new DecisionProtocolException($"Unsupported answer type for question '{question.Question.Id}'.");
                }
            }

            writer.WriteEndObject();
        }

        TResult? result;
        try
        {
            result = JsonSerializer.Deserialize(stream.ToArray(), ResultTypeInfo);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            throw new DecisionProtocolException(
                "The decision answers could not be materialized as the declared result type.",
                exception);
        }

        if (result is null)
        {
            throw new DecisionProtocolException("The decision answers produced a null result.");
        }

        for (int i = 0; i < answers.Length; i++)
        {
            DecisionDefinitionQuestion<TResult> question = _questions[i];
            object? actualValue = question.ValueAccessor(result);
            bool preserved = answers[i] switch
            {
                BinaryDecisionAnswer binary => actualValue is double actualBinary && actualBinary.Equals(binary.TrueProbability),
                ChoiceDecisionAnswer choice => question.ChoiceValueMatches?.Invoke(actualValue, choice.SelectedCandidateId) is true,
                ScoreDecisionAnswer score => actualValue is double actualScore && actualScore.Equals(GetScoreValue(question, score)),
                _ => false,
            };

            if (!preserved)
            {
                throw new DecisionProtocolException(
                    $"The decision answer for result member '{question.Property.Name}' was not preserved by the configured JSON contract.");
            }
        }

        return new DecisionResponse<TResult>(result, response, this);
    }

    internal IReadOnlyDictionary<TEnum, double> GetDistribution<TEnum>(
        DecisionResponse response,
        Expression<Func<TResult, TEnum>> selector)
        where TEnum : struct, Enum
    {
        PropertyInfo property = GetDirectProperty(selector);
        DecisionDefinitionQuestion<TResult>? question = _questions.FirstOrDefault(question => question.Property == property);
        if (question?.EnumDefinition is not DecisionEnumDefinition<TEnum> definition)
        {
            Throw.ArgumentException(nameof(selector), $"The result property '{property.Name}' is not declared as a choice in this definition.");
            return null!;
        }

        ChoiceDecisionAnswer answer = (ChoiceDecisionAnswer)response.GetAnswer(question.Question.Id);
        Dictionary<TEnum, double> values = [];
        foreach (DecisionProbability probability in answer.Probabilities)
        {
            values.Add(definition.Parse(probability.Id), probability.Value);
        }

        return new ReadOnlyDictionary<TEnum, double>(values);
    }

    /// <summary>Gets the answer for <paramref name="declaredQuestion"/> and checks that it carries what binding consumes.</summary>
    private static DecisionAnswer GetBindableAnswer(DecisionResponse decisionResponse, DecisionQuestion declaredQuestion)
    {
        DecisionAnswer? answer = null;
        foreach (DecisionAnswer? candidate in decisionResponse.Answers)
        {
            if (candidate is not null && string.Equals(candidate.QuestionId, declaredQuestion.Id, StringComparison.Ordinal))
            {
                answer = candidate;
                break;
            }
        }

        if (answer is null)
        {
            throw new DecisionProtocolException($"The response does not contain an answer for question '{declaredQuestion.Id}'.");
        }

        bool bindable = (declaredQuestion, answer) switch
        {
            (_, UnansweredDecisionAnswer) => answer.Kind == declaredQuestion.Kind,
            (BinaryDecisionQuestion, BinaryDecisionAnswer) => true,
            (ChoiceDecisionQuestion choice, ChoiceDecisionAnswer choiceAnswer) =>
                choiceAnswer.Probabilities.Select(static p => p.Id).SequenceEqual(choice.Candidates.Select(static c => c.Id), StringComparer.Ordinal),
            (ScoreDecisionQuestion score, ScoreDecisionAnswer scoreAnswer) =>
                scoreAnswer.Probabilities.Select(static p => p.Id).SequenceEqual(score.Levels.Select(static l => l.Id), StringComparer.Ordinal),
            _ => false,
        };

        if (!bindable)
        {
            throw new DecisionProtocolException(
                $"The answer for question '{declaredQuestion.Id}' does not match the question's kind, candidates, or levels.");
        }

        return answer;
    }

    private static double GetScoreValue(DecisionDefinitionQuestion<TResult> question, ScoreDecisionAnswer score) =>
        question.UsesExpectedScore ? score.ExpectedScore :
        score.Score ?? throw new DecisionProtocolException($"Question '{question.Question.Id}' does not contain a provider-reported score.");

    private static AIFunctionFactoryOptions GetFunctionOptions(
        AIFunctionFactoryOptions? functionOptions,
        JsonSerializerOptions serializerOptions)
    {
        if (functionOptions is null)
        {
            return new AIFunctionFactoryOptions { SerializerOptions = serializerOptions };
        }

        if (functionOptions.SerializerOptions is not null)
        {
            return functionOptions;
        }

        return new AIFunctionFactoryOptions
        {
            SerializerOptions = serializerOptions,
            JsonSchemaCreateOptions = functionOptions.JsonSchemaCreateOptions,
            Name = functionOptions.Name,
            Description = functionOptions.Description,
            AdditionalProperties = functionOptions.AdditionalProperties,
            ConfigureParameterBinding = functionOptions.ConfigureParameterBinding,
            MarshalResult = functionOptions.MarshalResult,
            ExcludeResultSchema = functionOptions.ExcludeResultSchema,
        };
    }

    private static PropertyInfo GetDirectProperty<TValue>(Expression<Func<TResult, TValue>> selector)
    {
        _ = Throw.IfNull(selector);

        Expression body = selector.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } conversion)
        {
            body = conversion.Operand;
        }

        if (body is not MemberExpression { Member: PropertyInfo property, Expression: ParameterExpression parameter } ||
            selector.Parameters.Count != 1 ||
            selector.Parameters[0] != parameter)
        {
            Throw.ArgumentException(nameof(selector), "The selector must access one direct instance property of the result type.");
            return null!;
        }

        return property;
    }

    private JsonSerializerOptions GetFunctionSerializerOptions(JsonSerializerOptions stateOptions)
    {
        if (ReferenceEquals(stateOptions, SerializerOptions) ||
            stateOptions.TypeInfoResolver is not { } stateResolver ||
            ReferenceEquals(stateResolver, SerializerOptions.TypeInfoResolver))
        {
            return stateOptions;
        }

        JsonSerializerOptions combined = new(stateOptions)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(stateResolver, SerializerOptions.TypeInfoResolver),
        };
        combined.MakeReadOnly();
        return combined;
    }

    private JsonTypeInfo<TState> GetStateTypeInfo<TState>()
    {
        JsonTypeInfo? stateTypeInfo;
        try
        {
            stateTypeInfo = SerializerOptions.GetTypeInfo(typeof(TState));
        }
        catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException)
        {
            throw new NotSupportedException(
                $"The configured JSON options do not contain a contract for state type '{typeof(TState)}'.",
                exception);
        }

        return stateTypeInfo as JsonTypeInfo<TState> ?? throw new NotSupportedException(
            $"The configured JSON options do not contain a contract for state type '{typeof(TState)}'.");
    }

    /// <summary>Declares the questions of a <see cref="DecisionDefinition{TResult}"/>.</summary>
    [Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
    public sealed class Builder
    {
        private readonly JsonTypeInfo<TResult> _resultTypeInfo;
        private readonly List<DecisionDefinitionQuestion<TResult>> _questions = [];

        internal Builder(JsonTypeInfo<TResult> resultTypeInfo)
        {
            _resultTypeInfo = resultTypeInfo;
        }

        /// <summary>Declares a choice question backed by an enum result property.</summary>
        /// <typeparam name="TEnum">The enum type of the result property.</typeparam>
        /// <param name="selector">A direct result property selector.</param>
        /// <param name="instructions">
        /// Optional question instructions. When omitted, the property's <see cref="DescriptionAttribute"/> or name is used.
        /// </param>
        /// <remarks>
        /// Each enum value becomes one candidate whose ID is the value's JSON string and whose description is the value's
        /// <see cref="DescriptionAttribute"/> or name. The enum must serialize as a JSON string.
        /// </remarks>
        public void Choice<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(
            Expression<Func<TResult, TEnum>> selector,
            string? instructions = null)
            where TEnum : struct, Enum
        {
            PropertyInfo property = GetDirectProperty(selector);
            EnsurePropertyCanBeDeclared(property, typeof(TEnum));

            JsonPropertyInfo jsonProperty = GetJsonProperty(_resultTypeInfo, property);
            EnsurePropertyCanBeMaterialized(property, jsonProperty);
            DecisionEnumDefinition<TEnum> enumDefinition = CreateEnumDefinition<TEnum>(property, _resultTypeInfo, jsonProperty);
            AddQuestion(
                property,
                jsonProperty.Name,
                enumDefinition.CreateQuestion(jsonProperty.Name, GetInstructions(jsonProperty, property, instructions)),
                enumDefinition,
                usesExpectedScore: false,
                CreateValueAccessor(selector),
                (actualValue, candidateId) =>
                    actualValue is TEnum actual &&
                    EqualityComparer<TEnum>.Default.Equals(actual, enumDefinition.Parse(candidateId)));
        }

        /// <summary>Declares a binary question whose true probability is bound to a <see cref="double"/> result property.</summary>
        /// <param name="selector">A direct result property selector.</param>
        /// <param name="instructions">
        /// Optional question instructions. When omitted, the property's <see cref="DescriptionAttribute"/> or name is used.
        /// </param>
        public void BinaryProbability(
            Expression<Func<TResult, double>> selector,
            string? instructions = null)
        {
            PropertyInfo property = GetDirectProperty(selector);
            EnsurePropertyCanBeDeclared(property, typeof(double));

            JsonPropertyInfo jsonProperty = GetJsonProperty(_resultTypeInfo, property);
            EnsurePropertyCanBeMaterialized(property, jsonProperty);
            AddQuestion(
                property,
                jsonProperty.Name,
                new BinaryDecisionQuestion(jsonProperty.Name, GetInstructions(jsonProperty, property, instructions)),
                enumDefinition: null,
                usesExpectedScore: false,
                CreateValueAccessor(selector));
        }

        /// <summary>Declares a score question whose provider-reported score is bound to a <see cref="double"/> result property.</summary>
        /// <param name="selector">A direct result property selector.</param>
        /// <param name="levels">The ordered score levels.</param>
        /// <param name="instructions">
        /// Optional question instructions. When omitted, the property's <see cref="DescriptionAttribute"/> or name is used.
        /// </param>
        /// <remarks>Binding fails with <see cref="DecisionProtocolException"/> when the answer has no reported score.</remarks>
        public void Score(
            Expression<Func<TResult, double>> selector,
            IReadOnlyList<DecisionScoreLevel> levels,
            string? instructions = null) =>
            AddScore(selector, levels, instructions, usesExpectedScore: false);

        /// <summary>Declares a score question whose expected score is bound to a <see cref="double"/> result property.</summary>
        /// <param name="selector">A direct result property selector.</param>
        /// <param name="levels">The ordered score levels.</param>
        /// <param name="instructions">
        /// Optional question instructions. When omitted, the property's <see cref="DescriptionAttribute"/> or name is used.
        /// </param>
        /// <remarks>The bound value is <see cref="ScoreDecisionAnswer.ExpectedScore"/>, computed from the answer's distribution.</remarks>
        public void ExpectedScore(
            Expression<Func<TResult, double>> selector,
            IReadOnlyList<DecisionScoreLevel> levels,
            string? instructions = null) =>
            AddScore(selector, levels, instructions, usesExpectedScore: true);

        internal DecisionDefinition<TResult> Build()
        {
            if (_questions.Count == 0)
            {
                Throw.ArgumentException("configure", "A decision definition must declare at least one question.");
            }

            return new DecisionDefinition<TResult>(_resultTypeInfo, _questions);
        }

        private static DecisionEnumDefinition<TEnum> CreateEnumDefinition<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(
            PropertyInfo property,
            JsonTypeInfo<TResult> resultTypeInfo,
            JsonPropertyInfo jsonProperty)
            where TEnum : struct, Enum
        {
            if (typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false))
            {
                Throw.ArgumentException("selector", "Flags enums are not supported as decision choices.");
            }

            if (jsonProperty.CustomConverter is not null)
            {
                throw new NotSupportedException(
                    $"Property-specific JSON converters are not supported for decision enum member '{property.Name}'. " +
                    "Configure the enum converter on the enum type or the JSON options.");
            }

            JsonTypeInfo enumTypeInfo;
            try
            {
                enumTypeInfo = resultTypeInfo.Options.GetTypeInfo(typeof(TEnum));
            }
            catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException)
            {
                throw new NotSupportedException(
                    $"The configured JSON options do not contain a contract for enum type '{typeof(TEnum)}'.",
                    exception);
            }

#if NET
            TEnum[] enumValues = Enum.GetValues<TEnum>();
#else
            TEnum[] enumValues = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToArray();
#endif
            List<DecisionEnumValue<TEnum>> values = new(enumValues.Length);
            foreach (TEnum value in enumValues)
            {
                JsonElement serialized = JsonSerializer.SerializeToElement(value, enumTypeInfo);
                if (serialized.ValueKind != JsonValueKind.String || serialized.GetString() is not string id)
                {
                    throw new NotSupportedException(
                        $"Enum type '{typeof(TEnum)}' must use string JSON serialization for decision choices.");
                }

                string enumName = Enum.GetName(typeof(TEnum), value)!;
                string description = typeof(TEnum).GetField(enumName)?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? enumName;
                values.Add(new DecisionEnumValue<TEnum>(value, id, description));
            }

            return new DecisionEnumDefinition<TEnum>(values);
        }

        private static JsonPropertyInfo GetJsonProperty(JsonTypeInfo<TResult> resultTypeInfo, PropertyInfo property)
        {
            JsonPropertyInfo? jsonProperty = resultTypeInfo.Properties.FirstOrDefault(
                candidate => candidate.AttributeProvider is PropertyInfo propertyInfo && propertyInfo == property);

            if (jsonProperty is null)
            {
                string expectedName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ??
                    resultTypeInfo.Options.PropertyNamingPolicy?.ConvertName(property.Name) ??
                    property.Name;
                jsonProperty = resultTypeInfo.Properties.FirstOrDefault(
                    candidate => string.Equals(candidate.Name, expectedName, StringComparison.Ordinal));
            }

            if (jsonProperty is null)
            {
                Throw.ArgumentException("selector", $"The selected property '{property.Name}' is not present in the configured JSON contract.");
            }

            return jsonProperty;
        }

        private static string GetInstructions(JsonPropertyInfo jsonProperty, PropertyInfo property, string? explicitInstructions)
        {
            if (explicitInstructions is not null)
            {
                return Throw.IfNullOrWhitespace(explicitInstructions);
            }

            string? description = (jsonProperty.AttributeProvider as PropertyInfo)?.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;
            if (description is not null)
            {
                return description;
            }

#if NET9_0_OR_GREATER
            description = jsonProperty.AssociatedParameter?.AttributeProvider?.GetCustomAttributes(typeof(DescriptionAttribute), inherit: true)
                .OfType<DescriptionAttribute>()
                .FirstOrDefault()
                ?.Description;
#else
            description = GetConstructorDescription(property);
#endif
            if (description is not null)
            {
                return description;
            }

            return property.PropertyType.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description ?? property.Name;
        }

        private static void EnsurePropertyCanBeDeclared(PropertyInfo property, Type expectedType)
        {
            if (property.GetMethod is null ||
                property.GetMethod.IsStatic ||
                property.PropertyType != expectedType)
            {
                Throw.ArgumentException("selector", $"The selected property must be a readable instance property of type '{expectedType}'.");
            }
        }

#if !NET9_0_OR_GREATER
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Constructor parameter metadata is read only on older frameworks.")]
#endif
        private static void EnsurePropertyCanBeMaterialized(PropertyInfo property, JsonPropertyInfo jsonProperty)
        {
            if (jsonProperty.Set is not null)
            {
                return;
            }

#if NET9_0_OR_GREATER
            if (jsonProperty.AssociatedParameter is not null)
            {
                return;
            }
#else
            ConstructorInfo[] constructors = property.DeclaringType?.GetConstructors() ?? [];
            if (Array.Exists(
                constructors,
                candidate => Array.Exists(
                    candidate.GetParameters(),
                    parameter => string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase))))
            {
                return;
            }
#endif

            Throw.ArgumentException("selector", $"The selected property '{property.Name}' is not writable or bound to a JSON constructor parameter.");
        }

#if !NET9_0_OR_GREATER
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Constructor parameter metadata is read only on older frameworks.")]
        private static string? GetConstructorDescription(PropertyInfo property)
        {
            ConstructorInfo[] constructors = property.DeclaringType?.GetConstructors() ?? [];
            ConstructorInfo? constructor = Array.Find(
                constructors,
                candidate => Array.Exists(candidate.GetParameters(), parameter =>
                    string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase)));
            return Array.Find(
                constructor?.GetParameters() ?? [],
                parameter => string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase))
                ?.GetCustomAttribute<DescriptionAttribute>(inherit: true)
                ?.Description;
        }
#endif

        private static Func<TResult, object?> CreateValueAccessor<TValue>(Expression<Func<TResult, TValue>> selector)
        {
#if NET
            Func<TResult, TValue> accessor = selector.Compile(preferInterpretation: true);
#else
            Func<TResult, TValue> accessor = selector.Compile();
#endif
            return result => accessor(result);
        }

        private void AddScore(
            Expression<Func<TResult, double>> selector,
            IReadOnlyList<DecisionScoreLevel> levels,
            string? instructions,
            bool usesExpectedScore)
        {
            PropertyInfo property = GetDirectProperty(selector);
            EnsurePropertyCanBeDeclared(property, typeof(double));
            _ = Throw.IfNull(levels);

            JsonPropertyInfo jsonProperty = GetJsonProperty(_resultTypeInfo, property);
            EnsurePropertyCanBeMaterialized(property, jsonProperty);
            AddQuestion(
                property,
                jsonProperty.Name,
                new ScoreDecisionQuestion(jsonProperty.Name, GetInstructions(jsonProperty, property, instructions), levels),
                enumDefinition: null,
                usesExpectedScore,
                CreateValueAccessor(selector));
        }

        private void AddQuestion(
            PropertyInfo property,
            string propertyName,
            DecisionQuestion question,
            object? enumDefinition,
            bool usesExpectedScore,
            Func<TResult, object?> valueAccessor,
            Func<object?, string, bool>? choiceValueMatches = null)
        {
            if (_questions.Exists(existing => existing.Property == property || string.Equals(existing.PropertyName, propertyName, StringComparison.Ordinal)))
            {
                Throw.ArgumentException("selector", "A result property or JSON property name can only be declared once.");
            }

            _questions.Add(new DecisionDefinitionQuestion<TResult>(
                property,
                propertyName,
                question,
                enumDefinition,
                usesExpectedScore,
                valueAccessor,
                choiceValueMatches));
        }
    }

    private sealed class DecisionFunctionInvoker<TState>(
        IDecisionClient client,
        DecisionDefinition<TResult> definition,
        JsonTypeInfo<TState> stateTypeInfo,
        DecisionOptions? decisionOptions)
    {
        [DisplayName("evaluate_decision")]
        [Description("Evaluate the declared decision for one application state.")]
        public async Task<TResult> InvokeAsync(
            [Description("The application state for the declared decision.")] TState state,
            CancellationToken cancellationToken)
        {
            DecisionResponse<TResult> response = await client.GetDecisionAsync(
                state,
                stateTypeInfo,
                definition,
                options: decisionOptions?.Clone(),
                cancellationToken: cancellationToken);
            return response.Result;
        }
    }
}
