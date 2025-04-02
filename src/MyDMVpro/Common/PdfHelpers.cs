#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
//using SelectPdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace MyDMVpro.Common
{
    public class PdfHelpers
    {
        static PdfHelpers()
        {
            //DefaultPdfLibraryFolder = @"C:\maggard\dmvfiles\forms";
            //Startup.
        }

        public static string DefaultPdfLibraryFolder {
            get {
                string folder = ConfigurationHelper.Configuration["AppSettings:DefaultPdfLibraryFolder"];
                if (!Directory.Exists(folder))
                {
                    try
                    {
                        Directory.CreateDirectory(folder);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("{0}", ex);
                    }
                }
                return folder; 
            }
        }

        public static byte[] MergePdfs(List<MemoryStream> streams)
        {
            PdfFormManager form = new PdfFormManager(); 

            List<PdfDocument> docs = new List<PdfDocument>();
            foreach (MemoryStream ms in streams)
            {
                docs.Add(new PdfDocument(ms));
            }

            PdfDocument doc = new PdfDocument();
            {
                for (int i = 0; i < docs.Count; i++)
                {
                    PdfDocument d = docs[i];
                    doc.Append(d);
                }
                form.Load(doc);
            }

            //form.Flatten = true;
            //form.ReadOnly = true;
            //PdfDocument readonlyDoc = form.GetDocument();
            //form.Close();
            //readonlyDoc.CompressionLevel = PdfCompressionLevel.Best;
            //form.ReadOnly = true;

            byte[] fileBytes = form.Save();

            foreach (PdfDocument closeDocs in docs)
            {
                closeDocs.Close();
            }

            return fileBytes;
        }
        public static byte[] MergePdfs(List<byte[]> files)
        {
            List<MemoryStream> msList = new List<MemoryStream>();
            foreach (byte[] file in files)
            {
                if (file != null && file.Length > 0)
                {
                    msList.Add(new MemoryStream(file));
                }
            }
            return MergePdfs(msList);
        }
        public static MemoryStream GeneratePdf(List<byte[]> pdftemplates, JObject jobj, Dictionary<string, string> mapping, StringBuilder trace)
        {
            List<PdfDocument> docs = new List<PdfDocument>();

            if (pdftemplates == null || pdftemplates.Count == 0) return null;

            for (int index = 0; index < pdftemplates.Count; index++)
            {
                PdfFormManager form = new PdfFormManager();

                try
                {
                    // load the pdf form manager
                    form.Load(pdftemplates[index]);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("Error loading form template: {0}", ex);
                    throw new ApplicationException("Error loading form template");
                }
                StringBuilder sb = new StringBuilder();
                PdfFormFieldsCollection fields = form.Fields;

                for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
                {
                    PdfFormField field = fields[fieldIndex];
                    if (field.ReadOnly)
                    {
                        if (trace != null)
                            trace.AppendLine($"Skipping form field {field.Name}");
                        continue;
                    }

                    object fieldValue = null;
                    //GetValue(data, field.Name, mapping);
                    fieldValue = jobj[field.Name];
                    if (fieldValue != null)
                    {
                        SetFieldValue(field, fieldValue);
                        if (trace != null)
                            trace.AppendLine($"Setting form field {field.Name}: {fieldValue}");
                    }
                    else
                    {
                        if (trace != null)
                            trace.AppendLine($"No value for form field {field.Name}");
                    }
                }
                if (index > 0)
                    docs[0].Append(form.GetDocument());
                else
                    docs.Add(form.GetDocument());
            }

            // save pdf document
            PdfDocument doc = docs[0];
            MemoryStream ms = new MemoryStream();
            doc.Save(ms);

            // close pdf document
            doc.Close();

            return ms;
        }
        public static MemoryStream GeneratePdf(List<string> pdfnames, JObject jobj, Dictionary<string, string> mapping)
        {
            List<PdfDocument> docs = new List<PdfDocument>();

            if (pdfnames == null || pdfnames.Count == 0) return null;

            for (int index = 0; index < pdfnames.Count; index++)
            {
                string pdfname = pdfnames[index];
                PdfFormManager form = new PdfFormManager();

                string file = Path.Combine(DefaultPdfLibraryFolder, pdfname);

                try
                {
                    // load the pdf form manager
                    form.Load(file);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("Errorloading file {0}", ex);
                    throw new ApplicationException($"Error loading file '{file}'");
                }
                StringBuilder sb = new StringBuilder();
                PdfFormFieldsCollection fields = form.Fields;

                for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
                {
                    PdfFormField field = fields[fieldIndex];
                    if (field.ReadOnly)
                        continue;

                    object fieldValue = null;
                    //GetValue(data, field.Name, mapping);
                    fieldValue = jobj[field.Name];
                    if (fieldValue != null)
                        SetFieldValue(field, fieldValue);
                }
                if (index > 0)
                    docs[0].Append(form.GetDocument());
                else
                    docs.Add(form.GetDocument());
            }

            // save pdf document
            PdfDocument doc = docs[0];
            MemoryStream ms = new MemoryStream();
            doc.Save(ms);

            // close pdf document
            doc.Close();

            return ms;
        }
        public static MemoryStream GeneratePdf(List<string> pdfnames, DataRow row, Dictionary<string, string> mapping)
        {
            List<PdfDocument> docs = new List<PdfDocument>();

            if (pdfnames == null || pdfnames.Count == 0) return null;

            for (int index = 0; index< pdfnames.Count; index++)
            {
                string pdfname = pdfnames[index];

                PdfFormManager form = new PdfFormManager();

                string file = Path.Combine(DefaultPdfLibraryFolder, pdfname);

                // load the pdf form manager
                form.Load(file);

                PdfFormFieldsCollection fields = form.Fields;

                for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
                {
                    PdfFormField field = fields[fieldIndex];
                    if (field.ReadOnly)
                        continue;

                    object fieldValue = GetValue(row, field.Name, mapping);
                    SetFieldValue(field, fieldValue);
                }
                if (index > 0)
                    docs[0].Append(form.GetDocument());
                else 
                    docs.Add(form.GetDocument());
            }

            // save pdf document
            PdfDocument doc = docs[0];
            MemoryStream ms = new MemoryStream();
            doc.Save(ms);

            // close pdf document
            doc.Close();

            return ms;
        }

        public static MemoryStream GeneratePdf(List<string> pdfnames, Dictionary<string,object> data, Dictionary<string,string> mapping)
        {
            List<PdfDocument> docs = new List<PdfDocument>();

            if (pdfnames == null || pdfnames.Count == 0) return null;

            for (int index = 0; index < pdfnames.Count; index++)
            {
                string pdfname = pdfnames[index];
                PdfFormManager form = new PdfFormManager();

                string file = Path.Combine(DefaultPdfLibraryFolder, pdfname);

                // load the pdf form manager
                form.Load(file);

                StringBuilder sb = new StringBuilder();
                PdfFormFieldsCollection fields = form.Fields;

                for (int fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
                {
                    PdfFormField field = fields[fieldIndex];
                    if (field.ReadOnly)
                        continue;

                    object fieldValue = GetValue(data, field.Name, mapping);
                    if (fieldValue != null)
                        SetFieldValue(field, fieldValue);
                }
                if (index > 0)
                    docs[0].Append(form.GetDocument());
                else
                    docs.Add(form.GetDocument());
            }

            // save pdf document
            PdfDocument doc = docs[0];
            MemoryStream ms = new MemoryStream();
            doc.Save(ms);

            // close pdf document
            doc.Close();

            return ms;
        }

        static object GetValue(DataRow row, string key, Dictionary<string,string> mapping = null)
        {
            if (row == null)
            {
                return null;
            }

            if (mapping != null && mapping.ContainsKey(key))
            {
                string mappedKey = mapping[key];
                if (row.Table.Columns.Contains(mappedKey))
                    return row[mappedKey];
                return null;
            }
            if (row.Table.Columns.Contains(key))
                return row[key];
            return null;
        }
        static object GetValue(Dictionary<string,object> data, string key, Dictionary<string, string> mapping = null)
        {
            if (data == null)
            {
                return null;
            }

            if (mapping != null && mapping.ContainsKey(key))
            {
                string mappedKey = mapping[key];
                if (data.ContainsKey(mappedKey))
                    return data[mappedKey];
                return null;
            }
            if (data.ContainsKey(key))
                return data[key];
            return null;
        }

        static void SetFieldValue(PdfFormField field, object value)
        {
            if (value == null || value is DBNull)
            {
                field.ReadOnly = true;
                field.Flatten = true;
                return;
            }

            if (field is PdfFormFieldTextBox)
            {
                PdfFormFieldTextBox f = (field as PdfFormFieldTextBox);
                string strValue = value.ToString();
                if (value is JToken)
                {
                    JToken jtValue = value as JToken;
                    if (jtValue.Type == JTokenType.Date)
                    {
                        DateTime dt = jtValue.ToObject<DateTime>();
                        strValue = dt.ToString("MM/dd/yyyy");
                    }
                }
                f.Text = strValue;
                f.ReadOnly = true;
                f.Flatten = true;
            }
            else if (field is PdfFormFieldCheckBox)
            {
                PdfFormFieldCheckBox f = field as PdfFormFieldCheckBox;
                f.Checked = Convert.ToBoolean(value);
                f.ReadOnly = true;
                f.Flatten = true;
            }
            else if (field is PdfFormFieldRadioButtonList)
            {
                PdfFormFieldRadioButtonList rbf = field as PdfFormFieldRadioButtonList;
                string[] items = rbf.Items;
                rbf.ReadOnly = true;
                rbf.Flatten = true;
            }
            else
            {
                // ignore, should not get here
                //sb.AppendLine($"{field.GetType()}: '{field.Name}'");
            }

        }

        private static void DumpFields(PdfFormManager form)
        {
            PdfFormFieldsCollection fields = form.Fields;

            StringBuilder sb = new StringBuilder();

            foreach (bool isReadOnly in new bool[] { false, true })
            {
                string readonlyText = isReadOnly ? "READONLY" : "EDITABLE";
                sb.AppendLine($"************** {readonlyText} **************");

                for (int i = 0; i < fields.Count; i++)
                {
                    PdfFormField field = fields[i];
                    if (field.ReadOnly == isReadOnly)
                    {
                        string coords = $"Page: {field.DisplayPage}, Coords {field.DisplayRectangle}";
                        sb.AppendLine($"'{field.Name}'");
                        if (false)
                        {
                            if (field is PdfFormFieldTextBox)
                                sb.AppendLine($"{coords}, textbox: '{field.Name}'");
                            else if (field is PdfFormFieldCheckBox)
                                sb.AppendLine($"{coords}, checkbox: '{field.Name}'");
                            else if (field is PdfFormFieldRadioButtonList)
                                sb.AppendLine($"{coords}, rb list: '{field.Name}'");
                            else
                                sb.AppendLine($"{coords}, {field.GetType()}: '{field.Name}'");
                        }
                    }
                }
            }
            string text = sb.ToString();
        }
        public static List<byte[]> SplitPdf(Stream stream)
        {
            List<byte[]> list = new List<byte[]>();

            PdfFormManager form = new PdfFormManager();

            PdfDocument doc = new PdfDocument(stream);

            foreach (PdfPage page in doc.Pages)
            {
                PdfDocument docpage = new PdfDocument();
                docpage.Pages.Add(page);
                form.Load(docpage);
                byte[] fileBytes = form.Save();
                list.Add(fileBytes);
            }
            return list;
        }
    }
}
#endif