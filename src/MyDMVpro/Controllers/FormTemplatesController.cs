using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common.Extensions;
using MyDMVpro.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    public class USState
    {
        public string Abbrev { get; set; }
        public string FullName { get; set; }
        public string Display { get { return Abbrev + " - " + FullName; } }

        private static IEnumerable<USState> _states = null;
        public static IEnumerable<USState> GetAllStates()
        {
            return _states;
        }
        public static IEnumerable<SelectListItem> USStates
        {
            get { return new SelectList(USState.GetAllStates(), "Abbrev", "Display"); }
        }

        static USState()
        {
            var s = new string[,] {
                    { "AL", "Alabama" },
                    { "AK", "Alaska" },
                    { "AZ", "Arizona" },
                    { "AR", "Arkansas" },
                    { "CA", "California" },
                    { "CO", "Colorado" },
                    { "CT", "Connecticut" },
                    { "DE", "Delaware" },
                    { "DC", "District Of Columbia" },
                    { "FL", "Florida" },
                    { "GA", "Georgia" },
                    { "GU", "Guam" },
                    { "HI", "Hawaii" },
                    { "ID", "Idaho" },
                    { "IL", "Illinois" },
                    { "IN", "Indiana" },
                    { "IA", "Iowa" },
                    { "KS", "Kansas" },
                    { "KY", "Kentucky" },
                    { "LA", "Louisiana" },
                    { "ME", "Maine" },
                    { "MD", "Maryland" },
                    { "MA", "Massachusetts" },
                    { "MI", "Michigan" },
                    { "MN", "Minnesota" },
                    { "MS", "Mississippi" },
                    { "MO", "Missouri" },
                    { "MT", "Montana" },
                    { "NE", "Nebraska" },
                    { "NV", "Nevada" },
                    { "NH", "New Hampshire" },
                    { "NJ", "New Jersey" },
                    { "NM", "New Mexico" },
                    { "NY", "New York" },
                    { "NC", "North Carolina" },
                    { "ND", "North Dakota" },
                    { "OH", "Ohio" },
                    { "OK", "Oklahoma" },
                    { "OR", "Oregon" },
                    { "PA", "Pennsylvania" },
                    { "PR", "Puerto Rico" },
                    { "RI", "Rhode Island" },
                    { "SC", "South Carolina" },
                    { "SD", "South Dakota" },
                    { "TN", "Tennessee" },
                    { "TX", "Texas" },
                    { "UT", "Utah" },
                    { "VT", "Vermont" },
                    { "VA", "Virginia" },
                    { "WA", "Washington" },
                    { "WV", "West Virginia" },
                    { "WI", "Wisconsin" },
                    { "WY", "Wyoming" }
                };
            List<USState> states = new List<USState>();

            for (int i = 0; i <= s.GetUpperBound(0); i++)
            {
                USState state = new USState();
                state.Abbrev = s[i, 0];
                state.FullName = s[i, 1];
                states.Add(state);
            }
            _states = states;
        }
        public static string GetStateName(string stateAbbrev)
        {
            USState state = _states.Where(s => s.Abbrev == stateAbbrev).FirstOrDefault();
            if (state != null)
                return state.FullName;
            return "";
        }
    }
    public partial class FormTemplateUpload
    {
        public int Id { get; set; }
        [Required]
        [Display(Name = "State")]
        public string State { get; set; }
        [Required]
        [Display(Name = "Form Code")]
        public string FormCode { get; set; }
        public string Description { get; set; }
        public IFormFile FormFile { get; set; }

        public IEnumerable<SelectListItem> USStates
        {
            get { return new SelectList(USState.GetAllStates(), "Abbrev", "Display"); }
        }
    }

    public class FormTemplatesController : BaseController
    {
        public FormTemplatesController(MaggardDMVContext context, IConfiguration configuration, ILogger<FormTemplatesController> logger) : base(context, configuration, logger)
        {
        }

        // GET: FormTemplates
        public async Task<IActionResult> Index()
        {
            Guid vendorId = CurrentUserVendorId();

            var forms = _context.FormTemplates.Include(f => f.Vendor).Where(x => x.VendorId == vendorId);
            return View(await forms.ToListAsync());
        }

        // GET: FormTemplates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Guid vendorId = CurrentUserVendorId();

            var formTemplates = await _context.FormTemplates
                .Include(f => f.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id && m.VendorId == vendorId);
            if (formTemplates == null)
            {
                return NotFound();
            }

            return View(formTemplates);
        }

        public async Task<IActionResult> Download(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Guid vendorId = CurrentUserVendorId();

            var formTemplates = await _context.FormTemplates
                .Include(f => f.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id && m.VendorId == null || m.VendorId.Value == vendorId);
            if (formTemplates == null)
            {
                return NotFound();
            }

            return ReturnPdfFile(formTemplates.FormCode + ".pdf", formTemplates.PdfImage, true);
        }
        private IActionResult ReturnPdfFile(string filename, byte[] file, bool displayInline)
        {
            System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
            {
                FileName = filename,
                Inline = displayInline
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            //string lastmod = lastModified.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
            //Response.Headers.Add("Last-Modified", lastmod);

            return File(file, "application/pdf");
        }
        // GET: FormTemplates/Create
        public IActionResult Create()
        {
            FormTemplateUpload model = new FormTemplateUpload();
            return View(model);
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public async Task<IActionResult> Create([Bind("State,FormCode,Description,FormFile")] FormTemplateUpload newTemplate)
        {
            if (ModelState.IsValid)
            {
                var user = await GetCurrentUserAsync();

                FormTemplates ft = new FormTemplates();
                ft.FormCode = newTemplate.FormCode;
                ft.Description = newTemplate.Description;
                ft.PdfImage = MyDMVpro.Common.FileHelpers.ProcessBinaryFormFile(newTemplate.FormFile, ModelState);
                ft.State = newTemplate.State;
                ft.VendorId = user.VendorId;

                await _context.AddAsync(ft);
                await _context.SaveChangesAsync(user);
                return RedirectToAction(nameof(Index));
            }
            return View(newTemplate);
        }

        // GET: FormTemplates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formTemplates = await _context.FormTemplates.FindAsync(id);
            if (formTemplates == null)
            {
                return NotFound();
            }
            FormTemplateUpload template = new FormTemplateUpload();
            template.Id = formTemplates.Id;
            template.Description = formTemplates.Description;
            template.State = formTemplates.State;
            template.FormFile = null;
            template.FormCode = formTemplates.FormCode;

            return View(template);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,State,FormCode,Description,FormFile")] FormTemplateUpload formTemplate)
        {
            if (id != formTemplate.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    FormTemplates template = _context.FormTemplates.Find(id);
                    byte[] filedata = MyDMVpro.Common.FileHelpers.ProcessBinaryFormFile(formTemplate.FormFile, ModelState);
                    template.Description = formTemplate.Description;
                    template.FormCode = formTemplate.FormCode;
                    template.State = formTemplate.State;
                    template.PdfImage = filedata;
                    _context.Update<FormTemplates>(template, "State", "FormCode", "Description", "FormFile");
                    var user = await GetCurrentUserAsync();
                    await _context.SaveChangesAsync(user);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FormTemplatesExists(formTemplate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(formTemplate);
        }
        // GET: FormTemplates/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var formTemplates = await _context.FormTemplates
                .Include(f => f.Vendor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (formTemplates == null)
            {
                return NotFound();
            }

            return View(formTemplates);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var formTemplates = await _context.FormTemplates.FindAsync(id);
            _context.FormTemplates.Remove(formTemplates);
            var user = await GetCurrentUserAsync();
            await _context.SaveChangesAsync(user);
            return RedirectToAction(nameof(Index));
        }

        private bool FormTemplatesExists(int id)
        {
            return _context.FormTemplates.Any(e => e.Id == id);
        }
        private Guid CurrentUserVendorId()
        {
            string userName = GetUserNameOrSID();
            var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.UserPrincipalName == userName || u.NameIdentifierClaim == userName);
            if (user != null)
            {
                var vendorAgent = _context.VendorAgent.AsNoTracking().Single(x => x.AgentId == user.UserId);
                return vendorAgent.VendorId;
            }
            return Guid.Empty;
        }
    }
}
