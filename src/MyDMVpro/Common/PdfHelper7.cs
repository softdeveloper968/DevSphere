using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iText;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.IO.Font;
using iText.Kernel.Font;
using iText.Layout;
using iText.Layout.Element;
using iText.Html2pdf;

using System.IO;
using iText.StyledXmlParser.Css.Media;
using MyDMVpro.Services;

namespace MyDMVpro.Common;

public class PdfHelper7
{
    public static byte[] MergeFiles(List<byte[]> sourceFiles)
    {
        using (MemoryStream msNewDoc = new MemoryStream())
        {
            PdfWriter writer = new PdfWriter(msNewDoc);
            PdfDocument newDoc = new PdfDocument(writer);
            PdfMerger pdfMerger = new PdfMerger(newDoc);

            foreach (byte[] file in sourceFiles)
            {
                using (MemoryStream ms = new MemoryStream(file))
                {
                    PdfReader reader = new PdfReader(ms);
                    PdfDocument srcDoc = new PdfDocument(reader);
                    //PdfWriter fileWriter = new PdfWriter(msOutput);
                    //PdfAcroForm form = PdfAcroForm.GetAcroForm(srcDoc, true);
                    //foreach (var fieldKVP in form.GetFormFields())
                    //{
                    //    string name = fieldKVP.Key;
                    //    PdfFormField f = fieldKVP.Value;
                    //    bool comb = f.GetFieldFlag(iText.Forms.Fields.PdfTextFormField.FF_COMB);
                    //    if (comb)
                    //    {
                    //        var maxlen = f.GetPdfObject().GetAsNumber(PdfName.MaxLen);
                    //        if (maxlen == null)
                    //        {
                    //            System.Diagnostics.Trace.WriteLine($"{name}: no maxlength set");
                    //        }
                    //    }
                    //}
                    pdfMerger.Merge(srcDoc, 1, srcDoc.GetNumberOfPages());
                    srcDoc.Close();
                }
            }
            newDoc.Close();
            return msNewDoc.ToArray();
        }
    }

    public static List<byte[]> SplitPdf(Stream stream)
    {
        List<byte[]> list = new List<byte[]>();

        PdfReader reader = new PdfReader(stream);
        PdfDocument srcDoc = new PdfDocument(reader);

        MyPdfSplitter splitter = new MyPdfSplitter(srcDoc);
        var splitDocs = splitter.SplitByPageCount(1);

        foreach (var doc in splitDocs)
        {
            doc.SetCloseWriter(false);
            doc.Close();
        }
        foreach (MemoryStream ms in splitter.Streams)
        {
            ms.Position = 0;
            System.Diagnostics.Trace.WriteLine(ms.Length);
            list.Add(ms.ToArray());
        }
        return list;
    }

    public class MyPdfSplitter : PdfSplitter
    {
        public List<MemoryStream> Streams = new List<MemoryStream>();
        public MyPdfSplitter(PdfDocument pdfDocument) : base(pdfDocument)
        {
        }

        protected override PdfWriter GetNextPdfWriter(PageRange documentPageRange)
        {
            MemoryStream ms = new MemoryStream();
            Streams.Add(ms);
            return new PdfWriter(ms);
        }
    }

    public static Stream ConvertHtmlToPdf(string html)
    {
        // TBD: use iText7 for now
        //if (ConfigurationHelper.Configuration["UseIText7"] == "true")
        {
            using MemoryStream ms = new MemoryStream();
            ConverterProperties converterProperties = new ConverterProperties();
            converterProperties.SetMediaDeviceDescription(new MediaDeviceDescription(MediaType.PRINT));

            if (true)
            {
                HtmlConverter.ConvertToPdf(html, ms, converterProperties);
            }
            else 
            {
                PdfDocument pdfDoc = new PdfDocument(new PdfWriter(ms));
                pdfDoc.SetDefaultPageSize(new iText.Kernel.Geom.PageSize(72 * 8.5f, 72 * 11f));
                HtmlConverter.ConvertToPdf(html, pdfDoc, converterProperties);
            }

            //HtmlConverter.ConvertToPdf(html, ms, converterProperties);
            // ConvertToPdf closes the stream, so we need to recreate it
            // not optimal, as this will double the memory usage
            // until the original stream is garbage collected
            return new MemoryStream(ms.ToArray());
        }
        return PdfUtils.ConvertHtmlToPdf(html);
    }
}
