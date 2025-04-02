using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyDMVpro.Common;

public class SerializationHelpers
{
}

[JsonConverter(typeof(OptionsFunctionConverter))]
public class OptionsFunction
{
    public OptionsFunction(string text)
    {
        Text = text;
    }
    public string Text { get; set; }
}
public class OptionsFunctionConverter : JsonConverter<OptionsFunction>
{
    public override OptionsFunction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    protected virtual bool SkipInputValidation => true;

    public override void Write(Utf8JsonWriter writer, OptionsFunction value, JsonSerializerOptions options)
    {
        // skipInputValidation : true will improve performance, but only do this if you are certain the value represents well-formed JSON!
        writer.WriteRawValue(value.Text, skipInputValidation: SkipInputValidation);
    }
}

