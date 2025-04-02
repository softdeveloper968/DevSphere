using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyDMVpro.Controllers
{
    public class VinController : BaseController
    {
        public VinController(MaggardDMVContext context, IConfiguration configuration, ILogger<VinController> logger) : base(context, configuration, logger)
        {
        }

        [AllowAnonymous]
        public async Task<IActionResult> Lookup(string id)
        {
            try
            {
                string vin = id?.Trim().ToUpper();
                LookupResult result = new()
                {
                    VIN = vin,
                    ValidChecksum = VinHelper.IsValidVIN(vin)
                };
                if (!string.IsNullOrEmpty(vin) && vin.Length >= 11)
                {
                    string partialVin = string.Concat(vin[..8], vin.AsSpan(9, 2));
                    result.vpd = await _context.VinPartialDetail.AsNoTracking().Where(v => v.VinPattern == partialVin).ToListAsync();
                    foreach (var vpd in result.vpd)
                    {
                        vpd.InitFieldMappings();
                    }
                    if (result.vpd.Count == 0)
                    {
                        var pvins = VinHelper.GetPotentialVins(vin);
                        result.PossibleVins = await GetVinPartialDetails(pvins);
                    }
                }
                return Json(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
            }
            return Json(new { });
        }

        public async Task<IActionResult> VinCheck(string id)
        {
            try
            {
                string vin = id;
                var o = new Dictionary<string, object>
                {
                    ["VIN"] = vin,
                    ["IsValid"] = false
                };

                if (!string.IsNullOrEmpty(vin) && vin.Length == 17)
                {
                    o["IsValid"] = VinHelper.IsValidVIN(vin);
                }
                return JsonSuccess(o);
            }
            catch (Exception ex)
            {
                LogError(ex, "VinCheck");
                return JsonError("Unhandled exception");
            }
        }

        [Authorize(Policy = "VendorAgentOnly")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.VinPartialDetail.ToListAsync());
        }

        [Authorize(Policy = "VendorAgentOnly")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vinPartialDetail = await _context.VinPartialDetail
                .FirstOrDefaultAsync(m => m.VinId == id);
            if (vinPartialDetail == null)
            {
                return NotFound();
            }

            return View(vinPartialDetail);
        }

        [Authorize(Policy = "VendorAgentOnly")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        public async Task<IActionResult> Create([Bind("VinId,VinPattern,Year,Make,Model,Cylinders,Doors,FuelType,CurbWeight")] VinPartialDetail vinPartialDetail)
        {

            if (ModelState.IsValid)
            {
                await _context.AddAsync(vinPartialDetail);
                await _context.SaveChangesAsync(await GetCurrentUserAsync());
                return RedirectToAction(nameof(Index));
            }
            return View(vinPartialDetail);
        }

        [Authorize(Policy = "VendorAgentOnly")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vinPartialDetail = await _context.VinPartialDetail.FindAsync(id);
            if (vinPartialDetail == null)
            {
                return NotFound();
            }
            return View(vinPartialDetail);
        }

        [HttpPost]
        [Authorize(Policy = "VendorAgentOnly")]
        public async Task<IActionResult> Edit(int id, [Bind("VinId,VinPattern,Year,Make,Model,Cylinders,Doors,FuelType,CurbWeight")] VinPartialDetail vinPartialDetail)
        {
            if (id != vinPartialDetail.VinId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    //_context.Update(vinPartialDetail);

                    await _context.SaveChangesAsync(await GetCurrentUserAsync());
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VinPartialDetailExists(vinPartialDetail.VinId))
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
            return View(vinPartialDetail);
        }

        // GET: Vin/Delete/5
        [Authorize(Policy = "VendorAgentOnly")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vinPartialDetail = await _context.VinPartialDetail
                .FirstOrDefaultAsync(m => m.VinId == id);
            if (vinPartialDetail == null)
            {
                return NotFound();
            }

            return View(vinPartialDetail);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "VendorAgentOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vinPartialDetail = await _context.VinPartialDetail.FindAsync(id);
            _context.VinPartialDetail.Remove(vinPartialDetail);
            await _context.SaveChangesAsync(await GetCurrentUserAsync());
            return RedirectToAction(nameof(Index));
        }

        private bool VinPartialDetailExists(int id)
        {
            return _context.VinPartialDetail.Any(e => e.VinId == id);
        }
    }
    public class VinInfo
    {
        public string Vin { get; set; }
        public string Desc { get; set; }
    }

    public class LookupResult
    {
        public LookupResult()
        {
            vpd = new List<VinPartialDetail>();
            PossibleVins = new List<VinInfo>();
        }
        public string VIN { get; set; }
        public bool ValidChecksum { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public List<VinPartialDetail> vpd { get; set; }
        public List<VinInfo> PossibleVins { get; set; }
    }

}
