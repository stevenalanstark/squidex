﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Squidex.Infrastructure;

namespace Squidex.Domain.Apps.Core.Schemas
{
    public static class SchemaExtensions
    {
        public static long MaxId(this Schema schema)
        {
            var id = 0L;

            foreach (var field in schema.Fields)
            {
                if (field is IArrayField arrayField)
                {
                    foreach (var nestedField in arrayField.Fields)
                    {
                        id = Math.Max(id, nestedField.Id);
                    }
                }

                id = Math.Max(id, field.Id);
            }

            return id;
        }

        public static string TypeName(this IField field)
        {
            return field.Name.ToPascalCase();
        }

        public static string DisplayName(this IField field)
        {
            return field.RawProperties.Label.WithFallback(field.TypeName());
        }

        public static string TypeName(this Schema schema)
        {
            return schema.Name.ToPascalCase();
        }

        public static string DisplayName(this Schema schema)
        {
            return schema.Properties.Label.WithFallback(schema.TypeName());
        }

        public static string DisplayNameUnchanged(this Schema schema)
        {
            return schema.Properties.Label.WithFallback(schema.Name);
        }

        public static Guid SingleId(this ReferencesFieldProperties properties)
        {
            return properties.SchemaIds?.Count == 1 ? properties.SchemaIds[0] : Guid.Empty;
        }

        public static IEnumerable<RootField> ReferenceFields(this Schema schema)
        {
            var references = schema.FieldsInReferences.Select(x => schema.FieldsByName.GetOrDefault(x)).Where(x => x != null).ToList();

            if (references.Any())
            {
                return references;
            }

            references = schema.FieldsInLists.Select(x => schema.FieldsByName.GetOrDefault(x)).Where(x => x != null).ToList();

            if (references.Any())
            {
                return references;
            }

            return schema.Fields.Take(1);
        }

        public static IEnumerable<IField<ReferencesFieldProperties>> ResolvingReferences(this Schema schema)
        {
            return schema.Fields.OfType<IField<ReferencesFieldProperties>>()
                .Where(x =>
                    x.Properties.ResolveReference &&
                    x.Properties.MaxItems == 1 &&
                    x.IsListField(schema));
        }

        public static IEnumerable<IField<AssetsFieldProperties>> ResolvingAssets(this Schema schema)
        {
            return schema.Fields.OfType<IField<AssetsFieldProperties>>()
                .Where(x =>
                    x.Properties.ResolveImage &&
                    x.IsListField(schema));
        }

        private static bool IsListField(this IField field, Schema schema)
        {
            return schema.FieldsInLists.Contains(field.Name) || schema.Fields.Count == 1;
        }
    }
}
