using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDMVpro.Common;
using MyDMVpro.Models;
using MyDMVpro.Models.ChatsViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using UserInfo = MyDMVpro.Common.UserInfo;
using MyDMVpro.Common.ViewHelpers;

namespace MyDMVpro.Controllers
{
    public class ChatsController : BaseController
    {
        public ChatsController(MaggardDMVContext context, IConfiguration configuration, ILogger<ChatsController> logger) : base(context, configuration, logger)
        {
        }

        public IActionResult Index(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            return PartialView(GetModel(id.Value));
        }
        [HttpGet]
        public IActionResult Unread()
        {
            return PartialView("_Unread");
        }

        internal async Task<ChatViewModel> GetModel(Guid requestId)
        {
            ChatViewModel model = new ChatViewModel();

            try
            {
                UserInfo user = await GetCurrentUserAsync();

                if (!UserHasRequestPermission(user, requestId))
                {
                    model.ErrorMessage = "Access denied";
                    return model;
                }
                var chatContext = _context.Chats.Include(c => c.Request)
                    .Include(c => c.User)
                    .Where(x => x.RequestId == requestId)
                    .OrderBy(x => x.Created);

                List<Chats> list = await chatContext.ToListAsync();
                model.Chats = list;

                if (user.IsVendorAgent)
                {
                    model.HasUnreadChats = list.Any(c => c.IsVendor == false && c.ReadByVendor == false);
                }
                else
                {
                    model.HasUnreadChats = list.Any(c => c.IsVendor == true && c.ReadByUser == false);
                }

                var request = await _context.Requests.SingleOrDefaultAsync(x => x.RequestId == requestId);
                if (request != null)
                {
                    model.RequestId = requestId;
                    model.VehicleYear = request.VehicleYear;
                    model.VehicleMake = request.VehicleMake;
                }
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
            }
            return model;
        }
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid? id, DateTime? lastcreated)
        {
            var user = await GetCurrentUserAsync();

            var query = _context.Chats
                                    .Include(c => c.User)
                                    .Include(c => c.Request)
                            .Where(c => c.RequestId == id && (lastcreated == null || c.Created <= lastcreated));

            if (user.IsVendorAgent)
            {
                query = query.Where(c => c.ReadByVendor == false // not read by vendor
                                        && c.IsVendor == false // is a user message
                                        && c.Request.VendorId == user.VendorId);
            }
            else
            {
                query = query.Where(c => c.ReadByUser == false // not read by user
                                        && c.IsVendor == true // message from vendor
                                        && (c.UserId == user.UserId || (c.Request.GroupId == user.GroupId))); // user has permission
            }
            var chats = query.ToList();

            try
            {
                bool hasChanges = false;

                foreach (Chats c in chats)
                {
                    if (!c.IsVendor && !c.ReadByVendor)
                    {
                        // User message
                        c.ReadByVendor = true;
                        hasChanges = true;
                        c.ReadBy = user.UserId;
                        c.ReadOn = DateTime.Now;
                    }
                    else if (!c.ReadByUser)
                    {
                        c.ReadByUser = true;
                        hasChanges = true;
                        c.ReadBy = user.UserId;
                        c.ReadOn = DateTime.Now;
                    }
                }
                if (hasChanges)
                {
                    await _context.SaveChangesAsync(user);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("{0}", ex);
            }
            return Ok();
        }
        public async Task<IActionResult> View(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            if (!(await CurrentUserHasPermissionAsync(id.Value)))
            {
                return new UnauthorizedResult();
            }
            return PartialView("Index", await GetModel(id.Value));
        }
        public async Task<IActionResult> ChatStatus(Guid? id)
        {
            ChatStatus cs = new Models.ChatStatus();
            if (id != null)
            {
                if (await CurrentUserHasPermissionAsync(id))
                {
                    cs = await _context.ChatStatus.Where(c => c.RequestId == id.Value).SingleOrDefaultAsync();
                    if (cs == null)
                    {
                        // Just return default values when no chats exist for the request
                        cs = new ChatStatus()
                        {
                            RequestId = id.Value,
                            HasActiveChat = false,
                            LastChatUpdate = null,
                            HasNewChat = false,
                            WaitingForUserReply = false,
                            WaitingForVendorReply = false
                        };
                    }
                }
                else
                {
                    // just return default ChatStatus values
                    // 
                }
            }
            return new JsonResult(cs);
        }
        [HttpPost]
        public async Task<IActionResult> ChatStatusUpdates([FromForm] DateTime? date)
        {
            if (date != null) date = date.Value.AddSeconds(1);
            List<ChatStatus> list = null;

            UserInfo ui = await GetCurrentUserAsync();

            list = await _context.ChatStatus.Where(c =>
                ((c.GroupId == ui.GroupId) || (c.VendorId == ui.VendorId))
                && (date == null || c.LastChatUpdate >= date))
                .ToListAsync();
            if (list.Count > 0)
            {
#if DEBUG
                var breakHere = false;
#endif
            }
            return new JsonResult(list);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            if (!(await CurrentUserHasPermissionAsync(id.Value)))
            {
                return NotFound();
            }

            var chats = await _context.Chats
                .Include(c => c.Request)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.RequestId == id);
            if (chats == null)
            {
                return NotFound();
            }

            return View(chats);
        }

        public IActionResult Create()
        {
            return View();
        }
        public IActionResult GetMyChats()
        {
            UserInfo user = GetCurrentUser();

            if (user.IsVendorAgent == true)
            {
                var cs = _context.UnreadChatMessages.Where(c => c.VendorId == user.VendorId && c.WaitingForUser == false);
                return new JsonResult(cs);
            }
            else
            {
                var cs = _context.UnreadChatMessages.Where(c => c.UserId == user.UserId && c.WaitingForUser == true);
                return new JsonResult(cs);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] Guid RequestId, [FromForm] string Message)
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                return BadRequest();
            }
            //TODO: verify user owns this request
            var user = await GetCurrentUserAsync();
            if (!UserHasRequestPermission(user, RequestId))
            {
                return NotFound();
            }
            
            if (user != null)
            {
                Chats chat = new Chats();
                chat.RequestId = RequestId;
                chat.UserId = user.UserId.Value;
                chat.Message = Message;
                chat.IsVendor = (user.IsVendorAgent == true);
                await _context.AddAsync(chat);
                await _context.SaveChangesAsync(user);
                return PartialView("Index", await GetModel(RequestId));
            }
            return NotFound();
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            if (!(await CurrentUserHasPermissionAsync(id.Value)))
            {
                return NotFound();
            }

            var chats = await _context.Chats
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ChatId == id);
            if (chats == null)
            {
                return NotFound();
            }

            return View(chats);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var chats = await _context.Chats.FindAsync(id);
            if (!(await CurrentUserHasPermissionAsync(chats.RequestId)))
            {
                return RedirectToAction(nameof(Index));
            }
            _context.Chats.Remove(chats);
            UserInfo user = GetCurrentUser(false);
            await _context.SaveChangesAsync(user);
            return RedirectToAction(nameof(Index));
        }

        private bool ChatsExists(Guid id)
        {
            return _context.Chats.Any(e => e.ChatId == id);
        }

        public IActionResult GetAllChats(bool allGroupChats)
        {
            UserInfo user = GetCurrentUser();

            var chatsWithRequests = (from chat in _context.UnreadChatMessages.AsNoTracking()
                                     join req in _context.Requests.AsNoTracking()
                                         on chat.RequestId equals req.RequestId into reqGroup
                                     from req in reqGroup.DefaultIfEmpty()
                                     join stage in _context.ProcessStages.AsNoTracking()
                                         on req.ProcessStageId equals stage.ProcessStageId into stageGroup
                                     from stage in stageGroup.DefaultIfEmpty()
                                     join users in _context.Users.AsNoTracking()
                                         on chat.RequestUserId equals users.UserId into userChat
                                     from users in userChat.DefaultIfEmpty()
                                     select new ChatMessageModel
                                     {
                                         RequestId = chat.RequestId,
                                         RequestUserId = chat.RequestUserId,
                                         GroupId = chat.GroupId,
                                         UserId = chat.UserId,
                                         VendorId = chat.VendorId,
                                         UserName = chat.UserName,
                                         GroupName = chat.GroupName,
                                         VendorName = chat.VendorName,
                                         DateSent = chat.DateSent,
                                         VIN = chat.VIN,
                                         Message = chat.Message,
                                         WaitingForUser = chat.WaitingForUser,
                                         RequestUserName = users.DisplayName,
                                         RequestDetails = req != null ? new RequestDetailsModel
                                         {
                                             Id = req.RequestId,
                                             State = req.State,
                                             AppType = req.AppType,
                                             ProcessStageName = stage != null ? stage.ProcessStageName : null,
                                             RequestNumber = req.Id,
                                         } : null
                                     }).ToList();

            var result = new List<ChatMessageModel>();

            if (allGroupChats)
            {
                result = chatsWithRequests.Where(x => x.WaitingForUser == true && x.GroupName == user.GroupName).ToList();

            }
            else
            {
                result = chatsWithRequests.Where(x => x.RequestUserId == user.UserId && x.WaitingForUser == true && x.GroupName == user.GroupName).ToList();

            }

            return Ok(result);
        }

        public IActionResult GetAllClients()
        {
            UserInfo user = GetCurrentUser();

            var client = _context.Users
                        .Join(_context.UserGroups, user => user.UserId, userGroup => userGroup.UserId,
                            (user, userGroup) => new { user, userGroup })
                        .Join(_context.Groups, ug => ug.userGroup.GroupId, group => group.GroupId,
                            (ug, group) => new
                            {
                                UserId = ug.user.UserId,
                                UserName = ug.user.UserPrincipalName,
                                DisplayName = ug.user.DisplayName,
                                GroupId = group.GroupId,
                                GroupName = group.GroupName,
                                IsVendorAgent = ug.user.IsVendorAgent,
                                Active = ug.user.Active
                            })
                        .Where(x => x.IsVendorAgent == false && x.Active == true)
                        .AsNoTracking()
                        .ToList();

            return Json(client);

        }

        [HttpPost]
        public async Task<IActionResult> GetAllUnreadChats()
        {
            var filterHelper = new FilterHelper<CommunicationChatMessages>((MaggardDMVContext)_context, _configuration, this);
            var allGroupChats = Request.Form["allGroupChats"];

            return await filterHelper.GetRecords(delegate (Guid? vendorId, Guid? groupId, Guid? userId)
            {
                IQueryable<CommunicationChatMessages> rows;

                if (Convert.ToBoolean(allGroupChats))
                {
                    rows = _context.UnreadCommunicationChatMessages
                        .Where(x => x.GroupId == groupId && x.WaitingForUser == true);
                    return rows;
                }
                else
                {
                    rows = _context.UnreadCommunicationChatMessages
                        .Where(x => x.RequestUserId == userId && x.GroupId == groupId && x.WaitingForUser == true);
                    return rows;
                }
               
            }, filterOnCurrentUser: true);
        }

    }
}
