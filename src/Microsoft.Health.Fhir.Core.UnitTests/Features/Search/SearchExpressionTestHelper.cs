﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Core.Extensions;
using Microsoft.Health.Fhir.Core.Features.Search.Expressions;
using Xunit;

namespace Microsoft.Health.Fhir.Core.UnitTests.Features.Search
{
    public static class SearchExpressionTestHelper
    {
        public static void ValidateParamAndValue(Expression expression, string paramName, params Action<Expression>[] valueValidators)
        {
            MultiaryExpression andExpression = Assert.IsType<MultiaryExpression>(expression);

            Assert.Equal(MultiaryOperator.And, andExpression.MultiaryOperation);

            var validators = new List<Action<Expression>>(valueValidators);

            // Parameter name validation always comes first.
            validators.Insert(0, e => ValidateEqualsExpression(e, FieldName.ParamName, paramName));

            Assert.Collection(
                andExpression.Expressions,
                validators.ToArray());
        }

        public static void ValidateChainedExpression(
            Expression expression,
            ResourceType resourceType,
            string key,
            ResourceType targetResourceType,
            Action<Expression> childExpressionValidator)
        {
            ChainedExpression chainedExpression = Assert.IsType<ChainedExpression>(expression);

            Assert.Equal(resourceType, chainedExpression.ResourceType);
            Assert.Equal(key, chainedExpression.ParamName);
            Assert.Equal(targetResourceType, chainedExpression.TargetResourceType);

            childExpressionValidator(chainedExpression.Expression);
        }

        public static void ValidateChainedExpression(
            Expression expression,
            Type resourceType,
            string key,
            Type targetResourceType,
            Action<Expression> childExpressionValidator)
        {
            ChainedExpression chainedExpression = Assert.IsType<ChainedExpression>(expression);

            Assert.Equal(resourceType, chainedExpression.ResourceType.ToResourceModelType());
            Assert.Equal(key, chainedExpression.ParamName);
            Assert.Equal(targetResourceType, chainedExpression.TargetResourceType.ToResourceModelType());

            childExpressionValidator(chainedExpression.Expression);
        }

        public static void ValidateMultiaryExpression(
            Expression expression,
            MultiaryOperator multiaryOperator,
            params Action<Expression>[] valueValidators)
        {
            MultiaryExpression multiaryExpression = Assert.IsType<MultiaryExpression>(expression);

            Assert.Equal(multiaryOperator, multiaryExpression.MultiaryOperation);

            Assert.Collection(
                multiaryExpression.Expressions,
                valueValidators);
        }

        public static void ValidateEqualsExpression(Expression expression, FieldName expectedFieldName, object expectedValue)
        {
            ValidateBinaryExpression(expression, expectedFieldName, BinaryOperator.Equal, expectedValue);
        }

        public static void ValidateBinaryExpression(
            Expression expression,
            FieldName expectedFieldName,
            BinaryOperator expectedBinaryOperator,
            object expectedValue)
        {
            BinaryExpression equalExpression = Assert.IsType<BinaryExpression>(expression);

            Assert.Equal(expectedFieldName, equalExpression.FieldName);
            Assert.Equal(expectedBinaryOperator, equalExpression.BinaryOperator);
            Assert.Equal(expectedValue, equalExpression.Value);
        }

        public static void ValidateStringExpression(
            Expression expression,
            FieldName expectedFieldName,
            StringOperator expectedStringOperator,
            string expectedValue,
            bool expectedIgnoreCase)
        {
            StringExpression stringExpression = Assert.IsType<StringExpression>(expression);

            Assert.Equal(expectedStringOperator, stringExpression.StringOperator);
            Assert.Equal(expectedFieldName, stringExpression.FieldName);
            Assert.Equal(expectedValue, stringExpression.Value);
            Assert.Equal(expectedIgnoreCase, stringExpression.IgnoreCase);
        }

        public static void ValidateDateTimeBinaryOperatorExpression(
            Expression expression,
            FieldName expectedFieldName,
            BinaryOperator expectedExpression,
            DateTimeOffset expectedValue)
        {
            BinaryExpression bExpression = Assert.IsType<BinaryExpression>(expression);

            Assert.Equal(expectedExpression, bExpression.BinaryOperator);
            Assert.Equal(expectedFieldName, bExpression.FieldName);
            Assert.Equal(expectedValue, bExpression.Value);
        }

        public static void ValidateMissingParamExpression(
            Expression expression,
            string expectedParamName,
            bool expectedIsMissing)
        {
            MissingParamExpression mpExpression = Assert.IsType<MissingParamExpression>(expression);

            Assert.Equal(expectedParamName, mpExpression.ParamName);
            Assert.Equal(expectedIsMissing, mpExpression.IsMissing);
        }

        public static void ValidateMissingFieldExpression(
            Expression expression,
            FieldName expectedFieldName)
        {
            MissingFieldExpression mfExpression = Assert.IsType<MissingFieldExpression>(expression);

            Assert.Equal(expectedFieldName, mfExpression.FieldName);
        }

        public static IEnumerable<object[]> GetEnumAsMemberData<TEnum>(Predicate<TEnum> predicate = null)
        {
            return Enum.GetNames(typeof(TEnum))
                .Select(name => (TEnum)Enum.Parse(typeof(TEnum), name))
                .Where(comparator => predicate?.Invoke(comparator) ?? true)
                .Select(comparator => new object[] { comparator });
        }
    }
}