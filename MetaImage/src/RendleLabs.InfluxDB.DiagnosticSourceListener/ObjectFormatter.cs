using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RendleLabs.InfluxDB.DiagnosticSourceListener
{
    using CustomDict = Dictionary<(string, Type), Func<PropertyInfo, IFormatter>>;
    
    internal sealed class ObjectFormatter
    {
        private static readonly byte[] ActivityDurationFieldName = Encoding.UTF8.GetBytes("activity_duration");
        private const byte Space = (byte) ' ';

        private readonly IFormatter[] _fieldFormatters;
        private readonly int _fieldCount;
        private readonly IFormatter[] _tagFormatters;
        private readonly int _tagCount;

        public ObjectFormatter(Type type, CustomDict customFieldFormatters, CustomDict customTagFormatters)
        {
            var (fieldFormatters, tagFormatters) = CreateFormatters(type, customFieldFormatters, customTagFormatters);

            _fieldFormatters = fieldFormatters.ToArray();
            _fieldCount = _fieldFormatters.Length;
            _tagFormatters = tagFormatters.ToArray();
            _tagCount = _tagFormatters.Length;
        }

        public bool Write(object args, Activity activity, Span<byte> span, out int bytesWritten)
        {
            if (span.Length == 0) goto fail;

            bytesWritten = 0;

            for (int i = 0; i < _tagCount; i++)
            {
                if (span.Length == 0) goto fail;

                if (!_tagFormatters[i].TryWrite(args, span, true, out int tagWritten)) goto fail;

                span = span.Slice(tagWritten);
                bytesWritten += tagWritten;
            }


            span[0] = Space;
            span = span.Slice(1);
            bytesWritten++;

            bool comma = false;
            for (int i = 0; i < _fieldCount; i++)
            {
                if (span.Length == 0) goto fail;

                if (!_fieldFormatters[i].TryWrite(args, span, comma, out int fieldWritten)) goto fail;

                span = span.Slice(fieldWritten);
                bytesWritten += fieldWritten;
                comma = comma || fieldWritten > 0;
            }

            if (activity == null || activity.Duration.Ticks <= 0L)
            {
                // We're done
                return true;
            }

            if (WriteActivityDuration(activity, span, ref bytesWritten, comma))
            {
                return true;
            }

            fail:
            bytesWritten = 0;
            return false;
        }

        private static bool WriteActivityDuration(Activity activity, Span<byte> span, ref int bytesWritten, bool comma)
        {
            if (span.Length < 25)
            {
                return false;
            }

            if (!FieldHelpers.Write(ActivityDurationFieldName.AsSpan(), activity.Duration.TotalMilliseconds, comma, span, out int fieldWritten))
            {
                return false;
            }

            bytesWritten += fieldWritten;
            return true;
        }

        private static (List<IFormatter> fieldFormatters, List<IFormatter> tagFormatters) CreateFormatters(Type type,
            CustomDict customFieldFormatters, CustomDict customTagFormatters)
        {
            var fieldFormatters = new List<IFormatter>();
            var tagFormatters = new List<IFormatter>();
            foreach (var property in type.GetProperties().Where(p => p.CanRead))
            {
                if (CheckCustomFormatters(customFieldFormatters, customTagFormatters, property, fieldFormatters, tagFormatters))
                {
                    continue;
                }

                if (FieldFormatter.IsFieldType(property.PropertyType))
                {
                    var formatter = FieldFormatter.TryCreate(property);
                    if (formatter != null)
                    {
                        fieldFormatters.Add(formatter);
                    }
                }
                else if (TagFormatter.IsTagType(property.PropertyType))
                {
                    var tagFormatter = TagFormatter.TryCreate(property);
                    if (tagFormatter != null)
                    {
                        tagFormatters.Add(tagFormatter);
                    }
                }
            }

            return (fieldFormatters, tagFormatters);
        }

        private static bool CheckCustomFormatters(CustomDict customFieldFormatters, CustomDict customTagFormatters, PropertyInfo property,
            List<IFormatter> fieldFormatters, List<IFormatter> tagFormatters)
        {
            bool custom = false;

            if (customFieldFormatters != null && customFieldFormatters.TryGetValue((property.Name, property.PropertyType), out var cf))
            {
                fieldFormatters.Add(cf(property));
                custom = true;
            }

            if (customTagFormatters != null && customTagFormatters.TryGetValue((property.Name, property.PropertyType), out cf))
            {
                tagFormatters.Add(cf(property));
                custom = true;
            }

            return custom;
        }
    }
}