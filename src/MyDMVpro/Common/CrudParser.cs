using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyDMVpro.Common;

public enum CrudAction
{
    create,
    edit,
    remove
}

public class CrudParser<T> where T : class, new()
{
    public T Value { get; set; }
    public CrudAction Action { get; set; }
    public object KeyValue { get; private set; }
    public PropertyInfo JsonProp { get; private set; }
    public PropertyInfo JsonDictionaryProp { get; private set; }
    public PropertyInfo KeyProp { get; private set; }
    public IFormCollection Form { get; private set; }
    public List<object> KeyValues { get; private set; }

    public CrudParser(IFormCollection form, CrudAction action)
    {
        this.Form = form;
        if (form.ContainsKey("action"))
        {
            this.Action = Enum.Parse<CrudAction>(form["action"]);
        }
        if (action != this.Action)
        {
            throw new ApplicationException($"Expected action '${action}, received '{this.Action}'");
        }
        // and set the value of the property to the value in the form
        this.KeyProp = GetKeyProperty();
        this.JsonProp = GetJsonProperty();
        if (JsonProp != null)
        {
            var attr = JsonProp.CustomAttributes.FirstOrDefault(p => p.AttributeType == typeof(JsonPropertyBucketAttribute));
            JsonDictionaryProp = GetPropertyByName("Fields");
        }
        //if (keyValues.Count > 1)
        //{
        //    throw new Exception("Multiple keys found");
        //}
        KeyValues = new();

        var keyValues = GetUniqueIds();
        foreach (var keyValue in keyValues)
        {
            if (Action != CrudAction.create)
            {
                KeyValues.Add(ConvertToType(keyValue, KeyProp.PropertyType));
            }
            else
            {
                KeyValues.Add(keyValue);
            }
        }
        if (KeyValues.Count == 1)
        {
            this.KeyValue = KeyValues[0];
        }
    }
    public delegate Task BeforeSaveHandler(List<T> results);

    public async Task<List<T>> ProcessCreateAsync(DbContext context, DbSet<T> dbset, BeforeSaveHandler handler = null)
    {
        if (this.Action != CrudAction.create)
        {
            throw new InvalidOperationException($"Create on action '{Action}' not supported");
        }
        List<T> result = new();
        bool updated = false;
        foreach (var keyValue in KeyValues)
        {
            updated = true;
            var entity = new T();
            result.Add(entity);
            dbset.Add(entity);
            UpdateInstance(keyValue, entity);
        }
        if (updated)
        {
            if (handler != null)
            {
                await handler(result);
            }
            await context.SaveChangesAsync();
        }
        return result;
    }
    public async Task<List<T>> ProcessEditAsync(DbContext context, DbSet<T> dbset, BeforeSaveHandler handler = null)
    {
        if (this.Action != CrudAction.edit)
        {
            throw new InvalidOperationException($"Edit on action '{Action}' not supported");
        }
        List<T> result = new();
        bool updated = false;
        foreach (var keyValue in KeyValues)
        {
            var entity = await dbset.FindAsync(keyValue);
            if (entity != null)
            {
                updated = true;
                result.Add(entity);
                UpdateInstance(keyValue, entity);
            }
            else
            {
                throw new ApplicationException("Entity not found");
            }
        }
        if (updated)
        {
            if (handler != null)
            {
                await handler(result);
            }
            await context.SaveChangesAsync();
        }
        return result;
    }
    public async Task<List<T>> ProcessRemoveAsync(DbContext context, DbSet<T> dbset, BeforeSaveHandler handler = null)
    {
        if (this.Action != CrudAction.remove)
        {
            throw new InvalidOperationException($"Remove on action '{Action}' not supported");
        }
        var updated = false;
        List<T> result = new();
        foreach (var keyValue in KeyValues)
        {
            var contact = await dbset.FindAsync(keyValue);
            if (contact != null)
            {
                updated = true;
                result.Add(contact);
                dbset.Remove(contact);
            }
            else
            {
                //throw new ApplicationException("Entity not found");
            }
        }
        if (updated)
        {
            if (handler != null)
            {
                await handler(result);
            }
            await context.SaveChangesAsync();
        }
        return result;
    }

    public void UpdateInstance(object keyValue, T instance)
    {
        object keyVal = null;
        List<string> jsonFields = new List<string>();

        // loop through the form keys and get the split
        // the key into the id and property name
        string id = null;

        if (instance == null)
        {
            throw new ArgumentNullException("instance");
        }
        string oid = null;
        Regex regex = new Regex(@"data\[(.*?)\]\[(.*?)\](?:\[(.*?)\])?");
        foreach (var key in Form.Keys)
        {
            // key is in the format data[id][propertyName]
            if (key != "action")
            {
                var match = regex.Match(key);
                if (match.Success)
                {
                    id = match.Groups[1].Value;
                    if (id != keyValue.ToString())
                    {
                        continue;
                    }
                    if (Action != CrudAction.create)
                    {
                        if (KeyProp.PropertyType.IsValueType)
                        {
                            keyVal = ConvertToType(id, KeyProp.PropertyType);
                        }

                        var currentKeyVal = KeyProp.GetValue(instance);
                        if (currentKeyVal == null || IsDefaultValue(currentKeyVal))
                        {
                            KeyProp.SetValue(instance, ConvertToType(id, KeyProp.PropertyType));
                        }
                        else
                        {
                            if (((IComparable)currentKeyVal).CompareTo(keyVal) != 0)
                            {
                                throw new Exception("Key mismatch");
                            }
                        }
                    }

                    if (match.Groups.Count == 3 || match.Groups[3].Value == "")
                    {
                        var propertyName = match.Groups[2].Value;
                        var property = GetPropertyByName(propertyName);
                        if (property != null)
                        {
                            var values = Form[key];
                            if (values.Count == 1)
                            {
                                string val = values[0];
                                property.SetValue(instance, ConvertToType(val, property.PropertyType));
                            }
                        }
                        // Use json property if exists
                        else if (JsonProp != null)
                        {
                            var values = Form[key];
                            if (values.Count == 1)
                            {
                                string val = values[0];
                                Dictionary<string, object> dict = null;
                                var existingValue = JsonDictionaryProp.GetValue(instance);
                                if (existingValue == null)
                                {
                                    existingValue = new Dictionary<string, object>();
                                    JsonDictionaryProp.SetValue(instance, existingValue);
                                }
                                if (existingValue is Dictionary<string, object>)
                                {
                                    dict = (Dictionary<string, object>)existingValue;
                                    dict[propertyName] = val;
                                }
                            }
                        }
                        else
                        {
                            // Nested child elements not yet supported
                            System.Diagnostics.Debug.WriteLine("key not matched: " + key);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("key not matched: " + key);
                    }
                }
            }
        }
    }
    private PropertyInfo GetPropertyByName(string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Public);
        if (property == null)
        {
            property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance);
        }
        return property;
    }
    private static bool IsDefaultValue(object value)
    {
        if (value == null)
        {
            // Null is considered the default value for reference types
            return true;
        }

        // Get the type of the object
        Type type = value.GetType();

        // Handle nullable types separately
        if (Nullable.GetUnderlyingType(type) != null)
        {
            // Get the underlying type (non-nullable)
            type = Nullable.GetUnderlyingType(type);
        }

        // Get the default value of the type
        object defaultValue = Activator.CreateInstance(type);

        // Compare the value with the default value
        return value.Equals(defaultValue);
    }
    private static object ConvertToType(object value, Type type)
    {
        if (type == typeof(Guid) || type == typeof(Guid?))
        {
            if (value.GetType() == typeof(string))
            {
                if (string.IsNullOrEmpty(value as string)) return null;
                return new Guid((string)value);
            }
        }
        if (value.GetType() == typeof(Microsoft.Extensions.Primitives.StringValues))
        {
            throw new Exception("unexpected StringValues");
        }
        if (value.GetType() == type)
        {
            return value;
        }
        if (type == typeof(byte[]))
        {
            return Convert.FromBase64String((string)value);
        }
        return Convert.ChangeType(value, type);
    }
    public PropertyInfo GetKeyProperty()
    {
        return typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() != null);
    }
    public PropertyInfo GetJsonProperty()
    {
        return typeof(T).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<JsonPropertyBucketAttribute>() != null);
    }
    /*
     * Given a form collection, the keys are in the format data[id][propertyName]
     * Get the unique id values from the form keys
     * */
    public List<string> GetUniqueIds()
    {
        List<string> ids = new();

        Regex regex = new Regex(@"data\[(.*?)\]\[(.*?)\](?:\[(.*?)\])?");
        foreach (var key in Form.Keys)
        {
            if (key != "action")
            {
                var match = regex.Match(key);
                if (match.Success)
                {
                    var id = match.Groups[1].Value;
                    if (!ids.Contains(id))
                    {
                        ids.Add(id);
                    }
                }
            }
        }
        return ids;
    }
}

