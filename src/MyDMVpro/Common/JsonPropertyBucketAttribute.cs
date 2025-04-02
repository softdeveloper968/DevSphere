using System;

namespace MyDMVpro.Common;

// Used to mark a string field as containing property values in json format
[AttributeUsage(AttributeTargets.Property)]
public sealed class JsonPropertyBucketAttribute : Attribute
{
    public string DictionaryFieldName { get; set; }
    public JsonPropertyBucketAttribute()
    {
    }
}

